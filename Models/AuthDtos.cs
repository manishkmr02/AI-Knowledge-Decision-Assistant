namespace KnowledgeAssistant.Models;

public record RegisterRequest(string OrganizationName, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record MeResponse(int UserId, int OrganizationId, string OrganizationName,
                         string Email, string AssistantToken);