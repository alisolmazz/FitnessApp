using FitnessApp.Data;
using FitnessApp.Models;
using FitnessApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitnessApp.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. RANDEVULARIM SAYFASI
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var appointments = await _context.Appointments
                .Where(a => a.AppUserId == userId)
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .OrderByDescending(a => a.AppointmentDate) // En yeni en üstte
                .ToListAsync();

            return View(appointments);
        }

        // 2. RANDEVU ALMA FORMU (GET)
        // Hizmetler veya Eğitmenler sayfasından gelen ID'leri otomatik seçer
        [HttpGet]
        public async Task<IActionResult> Booking(int? serviceId, int? trainerId)
        {
            var viewModel = new AppointmentBookingViewModel
            {
                Services = await _context.Services.ToListAsync(),
                Trainers = await _context.Trainers.ToListAsync()
            };

            // Eğer Hizmet ID ile gelindiyse
            if (serviceId.HasValue)
            {
                viewModel.ServiceId = serviceId.Value;
            }

            // Eğer Eğitmen ID ile gelindiyse
            if (trainerId.HasValue)
            {
                viewModel.TrainerId = trainerId.Value;

                // Eğitmenin uzmanlığını bulup, ilgili hizmeti de otomatik seçmeye çalışalım
                var trainer = viewModel.Trainers.FirstOrDefault(t => t.TrainerId == trainerId.Value);
                if (trainer != null)
                {
                    // Basit bir eşleştirme: Hizmet adı hocanın uzmanlığını içeriyor mu?
                    var relatedService = viewModel.Services
                        .FirstOrDefault(s => s.ServiceName.Contains(trainer.Specialization) ||
                                             trainer.Specialization.Contains(s.ServiceName));

                    if (relatedService != null)
                    {
                        viewModel.ServiceId = relatedService.ServiceId;
                    }
                }
            }

            return View(viewModel);
        }

        // 3. RANDEVU KAYDETME (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Booking(AppointmentBookingViewModel model)
        {
            // 1. Validasyon
            if (!ModelState.IsValid)
            {
                model.Services = await _context.Services.ToListAsync();
                model.Trainers = await _context.Trainers.ToListAsync();
                return View(model);
            }

            // 2. Hizmet Kontrolü
            var selectedService = await _context.Services.FirstOrDefaultAsync(s => s.ServiceId == model.ServiceId);
            if (selectedService == null)
            {
                ModelState.AddModelError("", "Hizmet bulunamadı.");
                return View(model);
            }

            // 3. GEÇMİŞ ZAMAN KONTROLÜ (Güvenlik)
            // Kullanıcı JS'i atlatıp geçmişe randevu alamasın
            var appointmentDateTime = model.AppointmentDate.Date.Add(model.AppointmentTime);
            if (appointmentDateTime < DateTime.Now)
            {
                ModelState.AddModelError("", "Geçmiş bir tarih veya saate randevu alamazsınız.");
                model.Services = await _context.Services.ToListAsync();
                model.Trainers = await _context.Trainers.ToListAsync();
                return View(model);
            }

            // 4. ÇAKIŞMA KONTROLÜ (Database)
            // Reddedilenler hariç, o saatte başka randevu var mı?
            bool isBusy = await _context.Appointments.AnyAsync(a =>
                a.TrainerId == model.TrainerId &&
                a.AppointmentDate.Date == model.AppointmentDate.Date &&
                a.AppointmentDate.Hour == model.AppointmentTime.Hours &&     // TimeSpan çoğul
                a.AppointmentDate.Minute == model.AppointmentTime.Minutes && // TimeSpan çoğul
                !a.IsRejected);

            if (isBusy)
            {
                ModelState.AddModelError("", "Seçtiğiniz saatte eğitmenimiz maalesef dolu.");
                model.Services = await _context.Services.ToListAsync();
                model.Trainers = await _context.Trainers.ToListAsync();
                return View(model);
            }

            // 5. KAYIT
            model.TotalPrice = selectedService.Price;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var appointment = new Appointment
            {
                AppUserId = userId,
                TrainerId = model.TrainerId,
                ServiceId = model.ServiceId,
                AppointmentDate = appointmentDateTime,
                IsConfirmed = false,
                IsRejected = false,
                Price = model.TotalPrice
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Randevu talebiniz alındı. Onay bekleniyor.";
            return RedirectToAction("Index");
        }

        // 4. İPTAL ETME (Kullanıcı)
        [HttpPost]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (appointment != null && appointment.AppUserId == userId)
            {
                // Sadece gelecekteki randevular iptal edilebilir
                if (appointment.AppointmentDate < DateTime.Now)
                {
                    TempData["ErrorMessage"] = "Geçmiş tarihli randevular iptal edilemez.";
                }
                else
                {
                    appointment.IsRejected = true;   // İptal durumu
                    appointment.IsConfirmed = false; // Onayı kaldır
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Randevunuz iptal edildi.";
                }
            }
            return RedirectToAction("Index");
        }

        // 5. AJAX SLOT KONTROLÜ (Frontend İçin)
        [HttpGet]
        public async Task<IActionResult> GetAvailableSlots(int trainerId, string date)
        {
            // Tarih formatı kontrolü
            if (!DateTime.TryParse(date, out DateTime selectedDate))
            {
                return BadRequest("Geçersiz tarih.");
            }

            var trainer = await _context.Trainers.FindAsync(trainerId);
            if (trainer == null) return NotFound();

            // O günkü dolu saatleri çek (Reddedilenler hariç)
            var busyTimes = await _context.Appointments
                .Where(a => a.TrainerId == trainerId
                         && a.AppointmentDate.Date == selectedDate.Date
                         && !a.IsRejected)
                .Select(a => a.AppointmentDate.TimeOfDay)
                .ToListAsync();

            var availableSlots = new List<object>();

            // Mesai başlangıcı
            TimeSpan currentTime = trainer.WorkStartTime;

            // Şu anki zaman (Geçmiş kontrolü için)
            TimeSpan now = DateTime.Now.TimeOfDay;
            bool isToday = selectedDate.Date == DateTime.Today;

            // Mesai bitimine kadar dön
            while (currentTime < trainer.WorkEndTime)
            {
                // 1. Doluluk Kontrolü
                bool isTaken = busyTimes.Any(bt => bt.Hours == currentTime.Hours && bt.Minutes == currentTime.Minutes);

                // 2. Geçmiş Saat Kontrolü (Eğer bugünse ve saat geçtiyse)
                bool isPast = isToday && currentTime < now;

                availableSlots.Add(new
                {
                    time = currentTime.ToString(@"hh\:mm"),
                    // Müsait olması için: Ne Dolu olmalı, Ne de Geçmiş olmalı
                    isAvailable = !isTaken && !isPast
                });

                // 1 saatlik aralıklarla slot ekle
                currentTime = currentTime.Add(TimeSpan.FromHours(1));
            }

            return Json(availableSlots);
        }
    }
}