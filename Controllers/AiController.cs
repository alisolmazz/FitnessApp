using FitnessApp.Services;
using FitnessApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Controllers
{
    [Authorize] // Sadece üye olanlar girebilir
    public class AiController : Controller
    {
        private readonly GeminiService _geminiService;

        public AiController(GeminiService geminiService)
        {
            _geminiService = geminiService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            // Sayfa ilk açıldığında boş bir model gönder
            return View(new AiRequestViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Index(AiRequestViewModel model)
        {
            // Kullanıcı butona bastığında verileri alıp servise gönderiyoruz
            if (model.Age > 0 && model.Weight > 0)
            {
                // Servisten cevabı bekle
                string aiResponse = await _geminiService.GetGymAdviceAsync(
                    model.Age, model.Height, model.Weight, model.Gender, model.Goal);

                // Cevabı modele ekle ve sayfaya geri dön
                model.Response = aiResponse;
            }

            return View(model);
        }
    }
}