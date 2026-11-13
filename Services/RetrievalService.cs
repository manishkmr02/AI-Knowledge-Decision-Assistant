using KnowledgeAssistant.Data;
using KnowledgeAssistant.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeAssistant.Services;

public record RetrievedChunk(
    int ChunkId,
    int DocumentId,
    string FileName,
    int PageNumber,
    string Text,
    double Score);

public class RetrievalService
{
    private readonly AppDbContext _db;
    private readonly IEmbeddingService _embed;

    public RetrievalService(AppDbContext db, IEmbeddingService embed)
    {
        _db = db;
        _embed = embed;
    }

    /// Embeds and stores vectors for all chunks of a document that don't have one yet.
    public async Task<int> EmbedDocumentAsync(int documentId, CancellationToken ct = default)
    {
        var chunks = await _db.DocumentChunks
            .Where(c => c.DocumentId == documentId && c.EmbeddingJson == null)
            .OrderBy(c => c.Id)
            .ToListAsync(ct);

        if (chunks.Count == 0) return 0;

        // Batch in groups of 64 (OpenAI allows up to 2048 inputs, but 64 keeps requests small)
        const int batchSize = 64;
        for (int i = 0; i < chunks.Count; i += batchSize)
        {
            var slice = chunks.Skip(i).Take(batchSize).ToList();
            var texts = slice.Select(c => c.Text).ToList();
            var vectors = await _embed.EmbedBatchAsync(texts, ct);

            for (int j = 0; j < slice.Count; j++)
                slice[j].EmbeddingJson = SerializeVector(vectors[j]);

            await _db.SaveChangesAsync(ct);
        }
        return chunks.Count;
    }

    /// Embeds the question and returns top-K chunks **scoped to the given organization**.
    public async Task<List<RetrievedChunk>> SearchAsync(
        int organizationId, string question, int topK = 5, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(question))
            return new();

        var qVec = await _embed.EmbedAsync(question, ct);

        // Load only this org's chunks with embeddings
        var rows = await _db.DocumentChunks
            .Where(c => c.OrganizationId == organizationId && c.EmbeddingJson != null)
            .Join(_db.Documents,
                c => c.DocumentId, d => d.Id,
                (c, d) => new { c.Id, c.DocumentId, d.FileName, c.PageNumber, c.Text, c.EmbeddingJson })
            .ToListAsync(ct);

        if (rows.Count == 0) return new();

        var scored = new List<RetrievedChunk>(rows.Count);
        foreach (var r in rows)
        {
            var vec = DeserializeVector(r.EmbeddingJson!);
            double score = CosineSimilarity(qVec, vec);
            scored.Add(new RetrievedChunk(r.Id, r.DocumentId, r.FileName, r.PageNumber, r.Text, score));
        }

        return scored
            .OrderByDescending(s => s.Score)
            .Take(topK)
            .ToList();
    }

    // ---- helpers ----

    private static string SerializeVector(float[] v) =>
        System.Text.Json.JsonSerializer.Serialize(v);

    private static float[] DeserializeVector(string json) =>
        System.Text.Json.JsonSerializer.Deserialize<float[]>(json) ?? Array.Empty<float>();

    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0) return 0;
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }
        if (na == 0 || nb == 0) return 0;
        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }
}