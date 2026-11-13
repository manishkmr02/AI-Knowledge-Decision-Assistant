using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace KnowledgeAssistant.Services;

public class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _http;
    private readonly EmbeddingOptions _opts;

    public OpenAiEmbeddingService(HttpClient http, IOptions<EmbeddingOptions> opts)
    {
        _http = http;
        _opts = opts.Value;
        _http.BaseAddress = new Uri("https://api.openai.com/v1/");
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _opts.ApiKey);
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var many = await EmbedBatchAsync(new[] { text }, ct);
        return many[0];
    }

    public async Task<List<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_opts.ApiKey))
            throw new InvalidOperationException(
                "OpenAI:ApiKey is not configured. Set it in appsettings.Development.json or user-secrets.");

        var payload = new
        {
            model = _opts.EmbeddingModel,
            input = texts
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "embeddings");
        req.Content = new StringContent(JsonSerializer.Serialize(payload),
            Encoding.UTF8, "application/json");

        using var res = await _http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI embeddings failed ({(int)res.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        var data = doc.RootElement.GetProperty("data");

        var results = new List<float[]>(texts.Count);
        foreach (var item in data.EnumerateArray())
        {
            var arr = item.GetProperty("embedding");
            var vec = new float[arr.GetArrayLength()];
            int i = 0;
            foreach (var v in arr.EnumerateArray()) vec[i++] = (float)v.GetDouble();
            results.Add(vec);
        }
        return results;
    }
}