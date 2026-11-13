namespace KnowledgeAssistant.Models;

public record DocumentDto(int Id, string FileName, int PageCount, long SizeBytes, DateTime UploadedAt);
public record UploadResponse(DocumentDto Document, string Message);