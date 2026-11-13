namespace KnowledgeAssistant.Models;

public class User
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Owner"; // Owner | Member
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}