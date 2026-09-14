using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace HdPlatform.Services;

public class LlmService
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly ILogger<LlmService> _logger;

    public LlmService(IConfiguration config, ILogger<LlmService> logger)
    {
        _http = new HttpClient();
        _logger = logger;
        _model = config["Llm:Model"] ?? "kimi-k3";
        _apiKey = Environment.GetEnvironmentVariable("MOONSHOT_API_KEY") ?? config["Llm:ApiKey"] ?? "";
        _baseUrl = config["Llm:BaseUrl"] ?? "https://api.moonshot.ai/v1";
        
        if (string.IsNullOrEmpty(_apiKey))
            _logger.LogWarning("LLM API key not configured. Set Llm:ApiKey or MOONSHOT_API_KEY env var.");
    }

    public async Task<string> GenerateReadingAsync(string chartJson, string readingType = "general")
    {
        if (string.IsNullOrEmpty(_apiKey))
            return "LLM service not configured. Please contact administrator.";

        var prompt = BuildPrompt(chartJson, readingType);
        
        var request = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = "You are a professional Human Design analyst. Provide insightful, practical readings based on chart data. Be specific, warm, and actionable." },
                new { role = "user", content = prompt }
            },
            temperature = 1,
            max_tokens = 2000
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        
        try
        {
            var response = await _http.PostAsync($"{_baseUrl}/chat/completions", content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "No response generated.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM API call failed");
            return $"Reading generation failed: {ex.Message}";
        }
    }

    private string BuildPrompt(string chartJson, string readingType)
    {
        return readingType switch
        {
            "career" => $"Based on this Human Design chart, provide a career and life purpose reading. Focus on the Profile, Incarnation Cross, and defined Centers. Chart data:\n\n{chartJson}",
            "relationship" => $"Based on this Human Design chart, provide a relationship and partnership reading. Focus on the Profile, emotional authority, and defined/undefined Centers. Chart data:\n\n{chartJson}",
            "health" => $"Based on this Human Design chart, provide a health and wellbeing reading. Focus on the Centers, Channels, and any open/defined energy dynamics. Chart data:\n\n{chartJson}",
            _ => $"Based on this Human Design chart, provide a comprehensive general reading. Cover Type, Strategy, Authority, Profile, and key themes. Chart data:\n\n{chartJson}"
        };
    }
}
