namespace KnowledgeAssistant.Models;

public class UsageCounter
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public int QuestionsAsked { get; set; }
    public int QuestionsAnswered { get; set; }
    public int QuestionsUnanswered { get; set; }
}