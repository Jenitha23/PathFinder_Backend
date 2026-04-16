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
        private readonly string _model;
        private readonly ILogger<GeminiAIService> _logger;
        private readonly bool _isEnabled;

        public GeminiAIService(IConfiguration configuration, ILogger<GeminiAIService> logger)
        {
            _logger = logger;
            _apiKey = configuration["Gemini:ApiKey"] ?? "";
            _model = configuration["Gemini:Model"] ?? "gemini-2.5-flash";
            
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("Gemini API key is missing. AI features will be disabled.");
                _isEnabled = false;
                _httpClient = new HttpClient();
            }
            else
            {
                _isEnabled = true;
                _httpClient = new HttpClient();
                _httpClient.Timeout = TimeSpan.FromSeconds(120);
                _logger.LogInformation($"Gemini AI Service initialized with model: {_model}");
            }
        }

        public async Task<string> GenerateContentAsync(string prompt, string? systemInstruction = null)
        {
            if (!_isEnabled)
            {
                _logger.LogWarning("Gemini AI is disabled. API key not configured.");
                throw new InvalidOperationException("AI features are disabled. Please configure Gemini API key.");
            }

            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
                
                _logger.LogInformation($"Calling Gemini API with model: {_model}");

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
                        maxOutputTokens = 4096  // Increased to get complete response
                    }
                };

                var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Gemini API error: {response.StatusCode} - {responseJson}");
                    
                    try
                    {
                        using var errorDoc = JsonDocument.Parse(responseJson);
                        var error = errorDoc.RootElement.GetProperty("error");
                        var message = error.GetProperty("message").GetString();
                        throw new Exception($"Gemini API error: {message}");
                    }
                    catch
                    {
                        throw new Exception($"Gemini API error: {response.StatusCode}");
                    }
                }

                using var resultDoc = JsonDocument.Parse(responseJson);
                var candidates = resultDoc.RootElement.GetProperty("candidates");
                
                if (candidates.GetArrayLength() == 0)
                {
                    throw new Exception("No candidates returned from Gemini API");
                }
                
                var text = candidates[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();
                    
                return text ?? "";
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
            
            // Extract JSON from the response (handles markdown, incomplete JSON, etc.)
            var jsonString = ExtractJsonFromResponse(textResponse);
            
            if (string.IsNullOrEmpty(jsonString))
            {
                _logger.LogError($"Could not extract JSON from response. Raw response: {textResponse.Substring(0, Math.Min(500, textResponse.Length))}");
                throw new Exception("Response did not contain valid JSON");
            }
            
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                return JsonSerializer.Deserialize<T>(jsonString, options) ?? throw new Exception("Failed to deserialize JSON");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"Failed to parse JSON. Attempted to parse: {jsonString.Substring(0, Math.Min(500, jsonString.Length))}");
                throw;
            }
        }

        private string ExtractJsonFromResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return "";
            
            var cleaned = response.Trim();
            
            // Remove markdown code blocks
            if (cleaned.Contains("```json"))
            {
                var start = cleaned.IndexOf("```json") + 7;
                var end = cleaned.LastIndexOf("```");
                if (end > start)
                {
                    cleaned = cleaned.Substring(start, end - start);
                }
            }
            else if (cleaned.Contains("```"))
            {
                var start = cleaned.IndexOf("```") + 3;
                var end = cleaned.LastIndexOf("```");
                if (end > start)
                {
                    cleaned = cleaned.Substring(start, end - start);
                }
            }
            
            cleaned = cleaned.Trim();
            
            // Find the first { and last }
            var firstBrace = cleaned.IndexOf('{');
            var lastBrace = cleaned.LastIndexOf('}');
            
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                var json = cleaned.Substring(firstBrace, lastBrace - firstBrace + 1);
                
                // Validate it's valid JSON (or close enough)
                if (json.StartsWith("{") && json.EndsWith("}"))
                {
                    return json;
                }
            }
            
            // If we couldn't extract, return the original cleaned response
            return cleaned;
        }
    }
}