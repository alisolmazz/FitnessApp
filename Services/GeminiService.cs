using System.Text;
using System.Text.Json;

namespace FitnessApp.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;

        // API Key (Bu kısım sizde tanımlı olmalı)
        private const string ApiKey = "AIzaSyBl5X954J__SVfB0WUHh7lVQGs9s49RWAY";

        private const string ApiUrl =
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=";

        public GeminiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetGymAdviceAsync(int age, int height, int weight, string gender, string goal)
        {
            // PROMPT MÜHENDİSLİĞİ: Kısa, net ve ÇİFT KUTU HTML yapısı istiyoruz.
            string prompt =
                $"Sen profesyonel ve enerjik bir spor koçusun. Danışan: {age} yaş, {height} cm, {weight} kg, {gender}. Hedef: {goal}. " +
                $"Bana bu kişi için ÇOK KISA, ÖZET ve MOTİVE EDİCİ bir plan hazırla. " +
                $"Kurallar: " +
                $"1. Cevabı sadece geçerli HTML kodu olarak ver (Markdown veya ```html kullanma). " +
                $"2. Cevabın tam olarak 2 ana bölümden oluşmalı: Beslenme ve Antrenman. " +
                $"3. Her bölüm için 4-5 kısa madde kullan. " +
                $"4. Maddeler için <ul> ve <li> etiketi kullan. " +
                $"5. Önemli kelimeleri <strong> etiketiyle vurgula. " +
                $"6. Cevabı aşağıdaki Bootstrap yapısına uygun olacak şekilde, tek bir string olarak üret. Toplam cevap 150 kelimeyi asla geçmesin. " +

                // İstenen HTML Yapısı
                $"<div class='row g-4'>" +
                    // KUTU 1: BESLENME
                    $"<div class='col-md-6'>" +
                        $"<div class='ai-box nutrition-box'>" +
                            $"<div class='box-header'><i class='fa fa-utensils'></i> Beslenme Tüyoları</div>" +
                            $"<div class='box-content'>" +
                                $"<ul>" +
                                    $"<li>Hedefine özel giriş (Örn: {goal} için protein/kalori dengesi).</li>" +
                                    $"<li>Madde 2: Sıvı tüketimi.</li>" +
                                    $"<li>Madde 3: Karbonhidrat seçimi (Hangi türler?).</li>" +
                                    $"<li>Madde 4: Antrenman öncesi/sonrası ne yemelisin?.</li>" +
                                $"</ul>" +
                            $"</div>" +
                        $"</div>" +
                    $"</div>" +

                    // KUTU 2: ANTRENMAN
                    $"<div class='col-md-6'>" +
                        $"<div class='ai-box workout-box'>" +
                            $"<div class='box-header'><i class='fa fa-dumbbell'></i> Antrenman Özeti</div>" +
                            $"<div class='box-content'>" +
                                $"<ul>" +
                                    $"<li>Haftada kaç gün antrenman yapmalısın.</li>" +
                                    $"<li>Hangi hareketlere odaklanmalısın.</li>" +
                                    $"<li>Antrenman süresi ve dinlenme tüyosu.</li>" +
                                    $"<li>Madde 4: Motivasyon veya ısınma/soğuma tüyosu.</li>" +
                                $"</ul>" +
                            $"</div>" +
                        $"</div>" +
                    $"</div>" +
                $"</div>";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = prompt } }
                    }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(ApiUrl + ApiKey, jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("candidates", out JsonElement candidates))
                    {
                        string result = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

                        // Temizlik: AI yanlışlıkla ```html ... ``` eklerse temizle (Razor'un doğru işlemesi için kritik)
                        return result.Replace("```html", "").Replace("```", "").Trim();
                    }
                }
            }
            catch (Exception ex) { return $"<div class='alert alert-danger'>API Bağlantı Hatası: {ex.Message}</div>"; }

            return "<div class='alert alert-warning'>Yapay zekadan cevap alınamadı.</div>";
        }
    }
}