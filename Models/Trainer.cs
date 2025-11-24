using System.ComponentModel.DataAnnotations;

namespace FitnessApp.Models
{
    public class Trainer
    {
        public int TrainerId { get; set; }

        [Required(ErrorMessage = "Ad Soyad zorunludur.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Uzmanlık alanı zorunludur.")]
        public string Specialization { get; set; } // Örn: Fitness, Yoga (Hizmet ile eşleşecek kilit nokta)

        public string ImageUrl { get; set; }

        // MESAİ SAATLERİ (Randevu kontrolü için kritik)
        public TimeSpan WorkStartTime { get; set; } 
        public TimeSpan WorkEndTime { get; set; }

        public virtual ICollection<Appointment> Appointments { get; set; }
    }
}