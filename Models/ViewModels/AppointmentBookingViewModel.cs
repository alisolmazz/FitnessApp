using FitnessApp.Models;
using System.ComponentModel.DataAnnotations;

namespace FitnessApp.ViewModels
{
    public class AppointmentBookingViewModel
    {
        [Required(ErrorMessage = "Hizmet seçimi zorunludur.")]
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Eğitmen seçimi zorunludur.")]
        public int TrainerId { get; set; }

        [Required(ErrorMessage = "Tarih zorunludur.")]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Saat zorunludur.")]
        [DataType(DataType.Time)]
        public TimeSpan AppointmentTime { get; set; }

        // Fiyat bilgisi
        public decimal TotalPrice { get; set; }

        // Dropdown listeleri
        public List<Service> Services { get; set; } = new List<Service>();
        public List<Trainer> Trainers { get; set; } = new List<Trainer>();
    }
}