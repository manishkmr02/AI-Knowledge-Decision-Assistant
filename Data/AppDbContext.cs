using KnowledgeAssistant.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeAssistant.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<UsageCounter> UsageCounters => Set<UsageCounter>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Organization>()
            .HasIndex(o => o.AssistantToken).IsUnique();

        b.Entity<User>()
            .HasIndex(u => u.Email).IsUnique();

        b.Entity<Organization>()
            .HasOne(o => o.Usage)
            .WithOne(u => u.Organization)
            .HasForeignKey<UsageCounter>(u => u.OrganizationId);

        b.Entity<DocumentChunk>()
    .HasIndex(c => c.OrganizationId);

        b.Entity<DocumentChunk>()
            .HasOne(c => c.Document)
            .WithMany(d => d.Chunks)
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}