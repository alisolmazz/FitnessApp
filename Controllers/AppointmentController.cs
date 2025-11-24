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

        // 1. RANDEVULARIM SAYFASI (Bu metot eksikse 404 alırsın)
        // URL: /Appointment veya /Appointment/Index
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var appointments = await _context.Appointments
                .Where(a => a.AppUserId == userId)
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            return View(appointments);
        }

        // 2. RANDEVU ALMA FORMU
        [HttpGet]
        public async Task<IActionResult> Booking(int? serviceId)
        {
            var viewModel = new AppointmentBookingViewModel
            {
                Services = await _context.Services.ToListAsync(),
                Trainers = await _context.Trainers.ToListAsync()
            };

            if (serviceId.HasValue)
            {
                viewModel.ServiceId = serviceId.Value;
            }

            return View(viewModel);
        }

        // 3. RANDEVU KAYDETME
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Booking(AppointmentBookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Services = await _context.Services.ToListAsync();
                model.Trainers = await _context.Trainers.ToListAsync();
                return View(model);
            }

            var selectedService = await _context.Services.FirstOrDefaultAsync(s => s.ServiceId == model.ServiceId);
            if (selectedService == null)
            {
                return View(model);
            }

            model.TotalPrice = selectedService.Price; 
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var appointment = new Appointment
            {
                AppUserId = userId,
                TrainerId = model.TrainerId,
                ServiceId = model.ServiceId,
                AppointmentDate = model.AppointmentDate.Date.Add(model.AppointmentTime), 
                IsConfirmed = false,
                IsRejected = false, 
                Price = model.TotalPrice 
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Randevu talebiniz alındı.";
            return RedirectToAction("Index"); // Kayıttan sonra listeye dön
        }

        // 4. İPTAL ETME
        [HttpPost]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (appointment != null && appointment.AppUserId == userId)
            {
                appointment.IsRejected = true; 
                appointment.IsConfirmed = false;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}