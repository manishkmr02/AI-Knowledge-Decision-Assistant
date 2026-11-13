using System.Security.Claims;
using KnowledgeAssistant.Data;
using KnowledgeAssistant.Models;
using KnowledgeAssistant.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeAssistant.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PdfService _pdf;
    private readonly RetrievalService _retrieval;

    public DocumentsController(AppDbContext db, PdfService pdf, RetrievalService retrieval)
    {
        _db = db; _pdf = pdf; _retrieval = retrieval;
    }

    private int CurrentOrgId =>
        int.Parse(User.FindFirstValue("org_id") ?? "0");

    // GET /api/documents
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var orgId = CurrentOrgId;
        var docs = await _db.Documents
            .Where(d => d.OrganizationId == orgId)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new DocumentDto(d.Id, d.FileName, d.PageCount, d.SizeBytes, d.UploadedAt))
            .ToListAsync();
        return Ok(new { documents = docs, limit = PdfService.MaxDocumentsPerOrg });
    }

    // POST /api/documents/upload  (multipart form, field name = "file")
    [HttpPost("upload")]
    [RequestSizeLimit(PdfService.MaxBytes + 1024)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        var orgId = CurrentOrgId;

        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file was uploaded." });

        var existingCount = await _db.Documents.CountAsync(d => d.OrganizationId == orgId);
        if (existingCount >= PdfService.MaxDocumentsPerOrg)
            return BadRequest(new
            {
                error =
                $"You have reached the limit of {PdfService.MaxDocumentsPerOrg} documents for this organization. Delete one to upload another."
            });

        if (file.Length > PdfService.MaxBytes)
            return BadRequest(new { error = "File exceeds the 10 MB limit." });

        byte[] bytes;
        using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms);
            bytes = ms.ToArray();
        }

        int pageCount;
        List<(int Page, int Index, string Text)> chunks;
        try
        {
            (pageCount, chunks) = _pdf.Extract(bytes, file.FileName);
        }
        catch (PdfService.PdfValidationException ex)
        {
            return StatusCode(ex.StatusCode, new { error = ex.Message });
        }

        var doc = new Document
        {
            OrganizationId = orgId,
            FileName = Path.GetFileName(file.FileName),
            PageCount = pageCount,
            SizeBytes = file.Length
        };

        foreach (var (page, idx, text) in chunks)
        {
            doc.Chunks.Add(new DocumentChunk
            {
                OrganizationId = orgId,
                PageNumber = page,
                ChunkIndex = idx,
                Text = text
            });
        }

        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();
        int embedded = 0;
        string? embedWarning = null;
        try
        {
            embedded = await _retrieval.EmbedDocumentAsync(doc.Id);
        }
        catch (Exception ex)
        {
            // Upload succeeds; embeddings can be retried later via /reindex
            embedWarning = "Document saved but embedding failed: " + ex.Message;
        }

        var dto = new DocumentDto(doc.Id, doc.FileName, doc.PageCount, doc.SizeBytes, doc.UploadedAt);
        var msg = $"Uploaded {doc.FileName} — {pageCount} page(s), {doc.Chunks.Count} chunks, {embedded} embedded.";
        if (embedWarning is not null) msg += " ⚠ " + embedWarning;
        return Ok(new UploadResponse(dto, msg));
        //var dto = new DocumentDto(doc.Id, doc.FileName, doc.PageCount, doc.SizeBytes, doc.UploadedAt);
        //return Ok(new UploadResponse(dto,
        //    $"Uploaded {doc.FileName} — {pageCount} page(s), {doc.Chunks.Count} chunks indexed."));
    }

    // DELETE /api/documents/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var orgId = CurrentOrgId;
        var doc = await _db.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.OrganizationId == orgId);

        if (doc is null) return NotFound(new { error = "Document not found." });

        _db.Documents.Remove(doc);   // cascades to DocumentChunks
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }
    [HttpPost("{id:int}/reindex")]
    public async Task<IActionResult> Reindex(int id)
    {
        var orgId = CurrentOrgId;
        var doc = await _db.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.OrganizationId == orgId);
        if (doc is null) return NotFound(new { error = "Document not found." });

        try
        {
            var n = await _retrieval.EmbedDocumentAsync(id);
            return Ok(new { ok = true, embeddedChunks = n });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Embedding failed: " + ex.Message });
        }
    }
}