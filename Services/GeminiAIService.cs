using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PATHFINDER_BACKEND.Services
{
    public class GeminiAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model = "gemini-2.0-flash-exp";
        private readonly ILogger<GeminiAIService> _logger;

        public GeminiAIService(IConfiguration configuration, ILogger<GeminiAIService> logger)
        {
            _logger = logger;
            _apiKey = configuration["Gemini:ApiKey"] ?? throw new Exception("Gemini:ApiKey is missing");
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/");
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        public async Task<string> GenerateContentAsync(string prompt, string? systemInstruction = null)
        {
            try
            {
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[] { new { text = prompt } }
                        }
                    },
                    system_instruction = systemInstruction != null ? new
                    {
                        parts = new[] { new { text = systemInstruction } }
                    } : null,
                    generationConfig = new
                    {
                        temperature = 0.2,
                        topP = 0.95,
                        topK = 40,
                        maxOutputTokens = 8192
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"{_model}:generateContent?key={_apiKey}";
                var response = await _httpClient.PostAsync(url, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Gemini API error: {response.StatusCode} - {responseJson}");
                    throw new Exception($"Gemini API error: {response.StatusCode}");
                }

                using var doc = JsonDocument.Parse(responseJson);
                return doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                throw;
            }
        }

        public async Task<T> GenerateStructuredContentAsync<T>(string prompt, string? systemInstruction = null)
        {
            var textResponse = await GenerateContentAsync(prompt, systemInstruction);
            var jsonStart = textResponse.IndexOf('{');
            var jsonEnd = textResponse.LastIndexOf('}') + 1;

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonString = textResponse.Substring(jsonStart, jsonEnd - jsonStart);
                return JsonSerializer.Deserialize<T>(jsonString) ?? throw new Exception("Failed to parse JSON");
            }
            throw new Exception("Response did not contain valid JSON");
        }
    }
}