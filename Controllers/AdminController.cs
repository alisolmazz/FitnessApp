using FitnessApp.Data;
using FitnessApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        // --- HİZMET YÖNETİMİ ---
        public async Task<IActionResult> Services()
        {
            var services = await _context.Services.ToListAsync();
            return View(services);
        }

        [HttpGet]
        public IActionResult CreateService()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateService(Service service, IFormFile? file)
        {
            if (file != null)
            {
                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                service.ImageUrl = uniqueFileName;
            }
            else
            {
                service.ImageUrl = "default-service.jpg";
            }

            _context.Services.Add(service);
            await _context.SaveChangesAsync();
            return RedirectToAction("Services");
        }

        public async Task<IActionResult> DeleteService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service != null)
            {
                _context.Services.Remove(service);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Services");
        }

        // --- ANTRENÖR YÖNETİMİ ---
        public async Task<IActionResult> Trainers()
        {
            var trainers = await _context.Trainers.ToListAsync();
            return View(trainers);
        }

        [HttpGet]
        public IActionResult CreateTrainer()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateTrainer(Trainer trainer, IFormFile? file)
        {
            if (file != null)
            {
                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                trainer.ImageUrl = uniqueFileName;
            }
            else
            {
                trainer.ImageUrl = "default-user.png";
            }

            if (trainer.WorkStartTime == TimeSpan.Zero) { trainer.WorkStartTime = new TimeSpan(9, 0, 0); }
            if (trainer.WorkEndTime == TimeSpan.Zero) { trainer.WorkEndTime = new TimeSpan(17, 0, 0); }

            _context.Trainers.Add(trainer);
            await _context.SaveChangesAsync();
            return RedirectToAction("Trainers");
        }

        public async Task<IActionResult> DeleteTrainer(int id)
        {
            var trainer = await _context.Trainers.FindAsync(id);
            if (trainer != null)
            {
                _context.Trainers.Remove(trainer);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Trainers");
        }

        // --- RANDEVU YÖNETİMİ (GÜNCELLENDİ) ---

        public async Task<IActionResult> Appointments()
        {
            var appointments = await _context.Appointments
                .Include(a => a.AppUser)
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            return View(appointments);
        }

        // Onayla
        public async Task<IActionResult> ApproveAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.IsConfirmed = true;
                appointment.IsRejected = false; // Reddedilmişse düzelt
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Appointments");
        }

        // Reddet (ARTIK SİLMİYOR, DURUMU DEĞİŞTİRİYOR)
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.IsRejected = true; // Reddedildi olarak işaretle
                appointment.IsConfirmed = false; // Onayı kaldır
                
                // _context.Appointments.Remove(appointment); // BU SATIR ARTIK YOK (Silme iptal)
                
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Appointments");
        }
    }
}