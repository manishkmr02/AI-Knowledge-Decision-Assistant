using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace KnowledgeAssistant.Services;

public class GeminiOptions
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gemini-1.5-flash";
}

public class GeminiService
{
    private readonly HttpClient _http;
    private readonly GeminiOptions _opts;

    public GeminiService(HttpClient http, IOptions<GeminiOptions> opts)
    {
        _http = http;
        _opts = opts.Value;
    }

    public async Task<string> GenerateContentAsync(string prompt, bool jsonMode = false)
    {
        if (string.IsNullOrWhiteSpace(_opts.ApiKey))
            throw new InvalidOperationException("Gemini API Key is missing.");

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_opts.Model}:generateContent?key={_opts.ApiKey}";

        //var payload = new
        //{
        //    contents = new[] { new { parts = new[] { new { text = prompt } } } },
        //    generationConfig = jsonMode ? new { responseMimeType = "application/json" } : null
        //};
        var payload = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new
            {
                responseMimeType = jsonMode ? "application/json" : "text/plain",
                temperature = 0.2, // Makes it faster and more factual
                maxOutputTokens = 800 // Prevents it from writing a massive essay
            }
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var res = await _http.SendAsync(req);
        var body = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
            throw new Exception($"Gemini API error ({(int)res.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "";
    }
}