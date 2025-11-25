using FitnessApp.Data;
using FitnessApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Controllers
{
    // Bu etiketler buranın bir ARAYÜZ değil, VERİ SERVİSİ olduğunu belirtir.
    [Route("api/trainers")]
    [ApiController]
    public class TrainersApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TrainersApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. TÜM ANTRENÖRLERİ GETİR (GET: api/trainers)
        // 2. FİLTRELEME (GET: api/trainers?uzmanlik=Yoga)
        [HttpGet]
        public async Task<IActionResult> GetTrainers([FromQuery] string? uzmanlik)
        {
            // LINQ Sorgusu Başlatılıyor
            var query = _context.Trainers.AsQueryable();

            // Eğer URL'de ?uzmanlik=... varsa filtrele (LINQ Where)
            if (!string.IsNullOrEmpty(uzmanlik))
            {
                query = query.Where(t => t.Specialization.Contains(uzmanlik));
            }

            // Sadece ihtiyacımız olan verileri seçelim (LINQ Select)
            // Tüm veritabanı nesnesini döndürmek yerine DTO (Data Transfer Object) mantığı
            var trainers = await query.Select(t => new
            {
                Id = t.TrainerId,
                AdSoyad = t.FullName,
                Alan = t.Specialization,
                Saatler = $"{t.WorkStartTime} - {t.WorkEndTime}"
            }).ToListAsync();

            return Ok(trainers); // 200 OK ve JSON verisi döner
        }

        // 3. ID'ye GÖRE GETİR (GET: api/trainers/5)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTrainerById(int id)
        {
            var trainer = await _context.Trainers
                .Where(t => t.TrainerId == id)
                .Select(t => new
                {
                    Id = t.TrainerId,
                    AdSoyad = t.FullName,
                    Alan = t.Specialization
                })
                .FirstOrDefaultAsync();

            if (trainer == null)
            {
                return NotFound("Böyle bir eğitmen bulunamadı.");
            }

            return Ok(trainer);
        }
    }
}