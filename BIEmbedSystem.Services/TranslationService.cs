using BIEmbedSystem.Services.DTO;
using BIEmbedSystem.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;

namespace BIEmbedSystem.Services
{
    public class TranslationService : ITranslationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public TranslationService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<TranslateSidebarResponse> TranslateAsync(
            List<string> texts,
            string targetLanguage
        )
        {
            var endpoint = _config["Translator:Endpoint"];
            var key = _config["Translator:Key"];
            var region = _config["Translator:Region"];

            var route = $"translate?api-version=3.0&to={targetLanguage}";
            var url = endpoint + route;

            var requestBody = texts.Select(t => new { Text = t }).ToArray();
            var json = JsonConvert.SerializeObject(requestBody);

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            request.Headers.Add("Ocp-Apim-Subscription-Key", key);
            request.Headers.Add("Ocp-Apim-Subscription-Region", region);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var resultJson = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<TranslatorResult>>(resultJson);

            var translations = new Dictionary<string, string>();

            for (int i = 0; i < texts.Count; i++)
            {
                translations[texts[i]] = result[i].Translations[0].Text;
            }

            return new TranslateSidebarResponse
            {
                Translations = translations
            };
        }

        // Internal models
        private class TranslatorResult
        {
            public List<TranslatorText> Translations { get; set; }
        }

        private class TranslatorText
        {
            public string Text { get; set; }
            public string To { get; set; }
        }
    }
}
