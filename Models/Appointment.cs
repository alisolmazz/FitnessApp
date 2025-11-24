using System.ComponentModel.DataAnnotations;

namespace FitnessApp.Models
{
    public class Appointment
    {
        public int AppointmentId { get; set; }

        public DateTime AppointmentDate { get; set; } 

        public bool IsConfirmed { get; set; } = false;
        
        // --- Reddedilme Alanı ---
        public bool IsRejected { get; set; } = false; 

        // --- Fiyat Alanı (Tek fiyata döndü) ---
        public decimal Price { get; set; } 

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // İlişkiler
        public string AppUserId { get; set; } 
        public virtual AppUser AppUser { get; set; }

        public int TrainerId { get; set; }
        public virtual Trainer Trainer { get; set; }

        public int ServiceId { get; set; }
        public virtual Service Service { get; set; }
    }
}