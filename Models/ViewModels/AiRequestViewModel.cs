namespace FitnessApp.ViewModels
{
    public class AiRequestViewModel
    {
        public int Age { get; set; }
        public int Height { get; set; } // cm
        public int Weight { get; set; } // kg
        public string Gender { get; set; } // Erkek/Kadın
        public string Goal { get; set; } // Kilo vermek, Hacim, Kondisyon

        // AI'dan gelen cevabı burada tutacağız
        public string? Response { get; set; }
    }
}