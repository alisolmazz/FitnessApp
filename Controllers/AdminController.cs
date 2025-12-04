using FitnessApp.Data;
using FitnessApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Threading.Tasks;
using System;

namespace FitnessApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        // ==========================================
        //              HİZMET YÖNETİMİ
        // ==========================================

        public async Task<IActionResult> Services()
        {
            // AsNoTracking: Sadece listeleme yapacağımız için EF Core'un değişiklik izlemesini kapatır, performans artar.
            var services = await _context.Services.AsNoTracking().ToListAsync();
            return View(services);
        }

        [HttpGet]
        public IActionResult CreateService() => View();

        [HttpPost]
        [ValidateAntiForgeryToken] // Güvenlik önlemi
        public async Task<IActionResult> CreateService(Service service, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                // Helper metodumuzu kullandık
                service.ImageUrl = await UploadImageAsync(file, "default-service.jpg");

                _context.Services.Add(service);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Hizmet başarıyla oluşturuldu.";
                return RedirectToAction(nameof(Services));
            }
            return View(service);
        }

        [HttpGet]
        public async Task<IActionResult> EditService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();
            return View(service);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(Service model, IFormFile? file)
        {
            // ID manipülasyonunu engellemek için veritabanından asıl veriyi çekiyoruz
            var existingService = await _context.Services.FindAsync(model.ServiceId);
            if (existingService == null) return NotFound();

            if (ModelState.IsValid)
            {
                existingService.ServiceName = model.ServiceName;
                existingService.Description = model.Description;
                existingService.DurationMinutes = model.DurationMinutes;
                existingService.Price = model.Price;

                // Sadece yeni bir dosya yüklenmişse resim değişir
                if (file != null)
                {
                    // İstersen burada eski resmi silme kodu da yazılabilir (System.IO.File.Delete...)
                    existingService.ImageUrl = await UploadImageAsync(file);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Hizmet güncellendi.";
                return RedirectToAction(nameof(Services));
            }
            return View(model);
        }

        public async Task<IActionResult> DeleteService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service != null)
            {
                _context.Services.Remove(service);
                await _context.SaveChangesAsync();
                TempData["WarningMessage"] = "Hizmet silindi.";
            }
            return RedirectToAction(nameof(Services));
        }

        // ==========================================
        //              EĞİTMEN YÖNETİMİ
        // ==========================================

        public async Task<IActionResult> Trainers()
        {
            var trainers = await _context.Trainers.AsNoTracking().ToListAsync();
            return View(trainers);
        }

        [HttpGet]
        public IActionResult CreateTrainer() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTrainer(Trainer trainer, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                trainer.ImageUrl = await UploadImageAsync(file, "default-user.png");

                // Saat atamalarını daha kısa yazdık
                if (trainer.WorkStartTime == TimeSpan.Zero) trainer.WorkStartTime = new TimeSpan(9, 0, 0);
                if (trainer.WorkEndTime == TimeSpan.Zero) trainer.WorkEndTime = new TimeSpan(17, 0, 0);

                _context.Trainers.Add(trainer);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Eğitmen başarıyla eklendi.";
                return RedirectToAction(nameof(Trainers));
            }
            return View(trainer);
        }

        [HttpGet]
        public async Task<IActionResult> EditTrainer(int id)
        {
            var trainer = await _context.Trainers.FindAsync(id);
            return trainer == null ? NotFound() : View(trainer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTrainer(Trainer model, IFormFile? file)
        {
            var trainer = await _context.Trainers.FindAsync(model.TrainerId);
            if (trainer == null) return NotFound();

            if (ModelState.IsValid)
            {
                trainer.FullName = model.FullName;
                trainer.Specialization = model.Specialization;
                trainer.WorkStartTime = model.WorkStartTime;
                trainer.WorkEndTime = model.WorkEndTime;

                if (file != null)
                {
                    trainer.ImageUrl = await UploadImageAsync(file);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Eğitmen bilgileri güncellendi.";
                return RedirectToAction(nameof(Trainers));
            }
            return View(model);
        }

        public async Task<IActionResult> DeleteTrainer(int id)
        {
            var trainer = await _context.Trainers.FindAsync(id);
            if (trainer != null)
            {
                _context.Trainers.Remove(trainer);
                await _context.SaveChangesAsync();
                TempData["WarningMessage"] = "Eğitmen silindi.";
            }
            return RedirectToAction(nameof(Trainers));
        }

        // ==========================================
        //              RANDEVU YÖNETİMİ
        // ==========================================

        public async Task<IActionResult> Appointments()
        {
            var appointments = await _context.Appointments
                .AsNoTracking() // Performans
                .Include(a => a.AppUser)
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            return View(appointments);
        }

        // Durum değiştirme işlemlerini tek bir metoda indirgedim (Opsiyonel ama daha temiz)
        public async Task<IActionResult> ChangeAppointmentStatus(int id, string status)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            if (status == "approve")
            {
                appointment.IsConfirmed = true;
                appointment.IsRejected = false;
                TempData["SuccessMessage"] = "Randevu onaylandı.";
            }
            else if (status == "cancel")
            {
                appointment.IsConfirmed = false;
                appointment.IsRejected = true;
                TempData["WarningMessage"] = "Randevu reddedildi.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Appointments));
        }

        // ==========================================
        //              YARDIMCI METOTLAR
        // ==========================================

        // Resim yükleme kodunu tek bir yerde topladık (DRY Prensibi)
        private async Task<string> UploadImageAsync(IFormFile? file, string defaultImageName = null)
        {
            if (file == null) return defaultImageName;

            // Güvenli dosya adı oluşturma (Guid + Orijinal isimdeki boşlukları temizleme)
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName.Replace(" ", "_");
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");

            // Klasör yoksa oluştur (Hata almamak için)
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return uniqueFileName;
        }
    }
}