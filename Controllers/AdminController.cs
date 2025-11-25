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

        // ==========================================
        //              HİZMET YÖNETİMİ
        // ==========================================

        // LİSTELEME
        public async Task<IActionResult> Services()
        {
            var services = await _context.Services.ToListAsync();
            return View(services);
        }

        // EKLEME (GET)
        [HttpGet]
        public IActionResult CreateService()
        {
            return View();
        }

        // EKLEME (POST)
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

        // DÜZENLEME (GET) - YENİ EKLENDİ
        [HttpGet]
        public async Task<IActionResult> EditService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();
            return View(service);
        }

        // DÜZENLEME (POST) - YENİ EKLENDİ
        [HttpPost]
        public async Task<IActionResult> EditService(Service model, IFormFile? file)
        {
            var service = await _context.Services.FindAsync(model.ServiceId);
            if (service == null) return NotFound();

            // Bilgileri güncelle
            service.ServiceName = model.ServiceName;
            service.Description = model.Description;
            service.DurationMinutes = model.DurationMinutes;
            service.Price = model.Price;

            // Yeni resim varsa güncelle
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

            await _context.SaveChangesAsync();
            return RedirectToAction("Services");
        }

        // SİLME
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

        // ==========================================
        //             EĞİTMEN YÖNETİMİ
        // ==========================================

        // LİSTELEME
        public async Task<IActionResult> Trainers()
        {
            var trainers = await _context.Trainers.ToListAsync();
            return View(trainers);
        }

        // EKLEME (GET)
        [HttpGet]
        public IActionResult CreateTrainer()
        {
            return View();
        }

        // EKLEME (POST)
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

        // DÜZENLEME (GET) - YENİ EKLENDİ
        [HttpGet]
        public async Task<IActionResult> EditTrainer(int id)
        {
            var trainer = await _context.Trainers.FindAsync(id);
            if (trainer == null) return NotFound();
            return View(trainer);
        }

        // DÜZENLEME (POST) - YENİ EKLENDİ
        [HttpPost]
        public async Task<IActionResult> EditTrainer(Trainer model, IFormFile? file)
        {
            var trainer = await _context.Trainers.FindAsync(model.TrainerId);
            if (trainer == null) return NotFound();

            // Bilgileri güncelle
            trainer.FullName = model.FullName;
            trainer.Specialization = model.Specialization;
            trainer.WorkStartTime = model.WorkStartTime;
            trainer.WorkEndTime = model.WorkEndTime;

            // Yeni resim varsa güncelle
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

            await _context.SaveChangesAsync();
            return RedirectToAction("Trainers");
        }

        // SİLME
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

        // ==========================================
        //             RANDEVU YÖNETİMİ
        // ==========================================

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

        // ONAYLA
        public async Task<IActionResult> ApproveAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.IsConfirmed = true;
                appointment.IsRejected = false;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Appointments");
        }

        // REDDET
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.IsRejected = true;
                appointment.IsConfirmed = false;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Appointments");
        }
    }
}