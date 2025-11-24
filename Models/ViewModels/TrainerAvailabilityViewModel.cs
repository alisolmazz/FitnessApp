using FitnessApp.Models;

namespace FitnessApp.ViewModels
{
    public class TrainerAvailabilityViewModel
    {
        public Trainer Trainer { get; set; }
        public List<TimeSlot> TimeSlots { get; set; } // O hocanın saatleri
    }

    public class TimeSlot
    {
        public TimeSpan Time { get; set; } // Saat (Örn: 14:00)
        public bool IsAvailable { get; set; } // Müsait mi? (Yeşil buton)
        public string StatusMessage { get; set; } // "Dolu", "Geçti" vs.
    }
}

