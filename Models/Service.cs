using System.ComponentModel.DataAnnotations;

namespace FitnessApp.Models
{
    public class Service
    {
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Hizmet adı zorunludur.")]
        public string ServiceName { get; set; } // Örn: Pilates, Fitness

        public string Description { get; set; } // Örn: Birebir özel ders...
        
        public string ImageUrl { get; set; } // Hizmet kartı resmi

        public int DurationMinutes { get; set; } // Süre (dk)

        public decimal Price { get; set; } // Ücret
    }
}