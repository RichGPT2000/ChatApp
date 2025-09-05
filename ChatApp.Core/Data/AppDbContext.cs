using ChatApp.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Core.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Chat>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired();
            e.Property(x => x.CreatedAtUtc).HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });

        b.Entity<Message>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Sender).IsRequired();
            e.Property(x => x.Text).IsRequired();
            e.Property(x => x.SentAtUtc).HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            e.HasOne(x => x.Chat)
             .WithMany(c => c.Messages)
             .HasForeignKey(x => x.ChatId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ChatId, x.SentAtUtc });
        });
    }
}
