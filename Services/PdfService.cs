using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace KnowledgeAssistant.Services;

public class PdfService
{
    public const int MaxPages = 20;
    public const long MaxBytes = 10L * 1024 * 1024; // 10 MB
    public const int MaxDocumentsPerOrg = 10;

    public class PdfValidationException : Exception
    {
        public int StatusCode { get; }
        public PdfValidationException(string message, int statusCode = 400) : base(message)
            => StatusCode = statusCode;
    }

    /// Validates bytes and returns (pageCount, list of chunks as (pageNumber, chunkIndex, text)).
    public (int PageCount, List<(int Page, int Index, string Text)> Chunks) Extract(
        byte[] bytes, string fileName)
    {
        if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            throw new PdfValidationException("Only PDF files are allowed.");

        if (bytes.Length == 0)
            throw new PdfValidationException("Uploaded file is empty.");

        if (bytes.Length > MaxBytes)
            throw new PdfValidationException(
                $"File exceeds the {MaxBytes / (1024 * 1024)} MB limit.");

        // PDF magic bytes: %PDF-
        if (bytes.Length < 5 ||
            bytes[0] != 0x25 || bytes[1] != 0x50 ||
            bytes[2] != 0x44 || bytes[3] != 0x46)
            throw new PdfValidationException("File is not a valid PDF.");

        PdfDocument pdf;
        try { pdf = PdfDocument.Open(bytes); }
        catch { throw new PdfValidationException("Could not read this PDF — it may be corrupted or password-protected."); }

        using (pdf)
        {
            if (pdf.NumberOfPages == 0)
                throw new PdfValidationException("PDF has no pages.");

            if (pdf.NumberOfPages > MaxPages)
                throw new PdfValidationException(
                    $"PDF has {pdf.NumberOfPages} pages — the limit is {MaxPages}.");

            var chunks = new List<(int, int, string)>();
            for (int p = 1; p <= pdf.NumberOfPages; p++)
            {
                Page page = pdf.GetPage(p);
                string pageText = page.Text ?? "";
                var pageChunks = ChunkText(pageText);
                for (int i = 0; i < pageChunks.Count; i++)
                    chunks.Add((p, i, pageChunks[i]));
            }

            if (chunks.Count == 0)
                throw new PdfValidationException(
                    "PDF contains no extractable text (it may be a scanned image).");

            return (pdf.NumberOfPages, chunks);
        }
    }

    /// Splits page text into ~800-char chunks with 100-char overlap, at sentence boundaries where possible.
    private static List<string> ChunkText(string text, int targetSize = 800, int overlap = 100)
    {
        text = NormalizeWhitespace(text);
        if (string.IsNullOrWhiteSpace(text)) return new();

        var result = new List<string>();
        int start = 0;
        while (start < text.Length)
        {
            int end = Math.Min(start + targetSize, text.Length);
            if (end < text.Length)
            {
                // Try to break at sentence or word boundary
                int period = text.LastIndexOf('.', end - 1, Math.Min(120, end - start));
                if (period > start + targetSize / 2) end = period + 1;
                else
                {
                    int space = text.LastIndexOf(' ', end - 1, Math.Min(80, end - start));
                    if (space > start) end = space;
                }
            }
            result.Add(text[start..end].Trim());
            if (end >= text.Length) break;
            start = Math.Max(end - overlap, start + 1);
        }
        return result.Where(c => c.Length > 20).ToList();
    }

    private static string NormalizeWhitespace(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new System.Text.StringBuilder(s.Length);
        bool lastWasSpace = false;
        foreach (var c in s)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace) { sb.Append(' '); lastWasSpace = true; }
            }
            else { sb.Append(c); lastWasSpace = false; }
        }
        return sb.ToString().Trim();
    }
}