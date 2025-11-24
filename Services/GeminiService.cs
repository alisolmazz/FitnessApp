using System.Text;
using System.Text.Json;

namespace FitnessApp.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;

        // API Key
        private const string ApiKey = "AIzaSyBl5X954J__SVfB0WUHh7lVQGs9s49RWAY";

        // ÇALIŞAN doğru endpoint + model
        private const string ApiUrl =
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=";

        public GeminiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetGymAdviceAsync(int age, int height, int weight, string gender, string goal)
        {
            string prompt =
                $"Ben {age} yaşında, {height} cm boyunda, {weight} kg ağırlığında bir {gender} bireyim. " +
                $"Spor salonuna gidiyorum ve hedefim: {goal}. " +
                $"Bana maddeler şeklinde günlük beslenme önerileri ve kısa bir antrenman planı ver. " +
                $"Samimi bir antrenör gibi konuş, cevap Türkçe olsun.";

            // ÇALIŞAN doğru JSON formatı
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                var response = await _httpClient.PostAsync(ApiUrl + ApiKey, jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    using var doc = JsonDocument.Parse(json);

                    if (doc.RootElement.TryGetProperty("candidates", out JsonElement candidates))
                    {
                        return candidates[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text")
                            .GetString();
                    }

                    return "AI boş bir cevap döndürdü.";
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return $"API Hatası: {response.StatusCode}\nDetay: {error}";
                }
            }
            catch (Exception ex)
            {
                return "Programsal hata: " + ex.Message;
            }
        }
    }
}
