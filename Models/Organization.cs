using System.Reflection.Metadata;

namespace KnowledgeAssistant.Models
{
 
    public class Organization
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string AssistantToken { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<User> Users { get; set; } = new();
        public List<Document> Documents { get; set; } = new();
        public UsageCounter? Usage { get; set; }
    }
}
