namespace KnowledgeAssistant.Models;

public class DocumentChunk
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public int OrganizationId { get; set; }               // denormalized for fast tenant filtering
    public int PageNumber { get; set; }                   // 1-based
    public int ChunkIndex { get; set; }                   // ordering within page
    public string Text { get; set; } = "";
    public string? EmbeddingJson { get; set; }            // filled in Step 4
}