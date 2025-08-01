using Microsoft.EntityFrameworkCore;

namespace ChatCompartido.Data;

public sealed class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options) {}

    public DbSet<ChatMessageEntity> Messages => Set<ChatMessageEntity>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<ChatMessageEntity>(e =>
        {
            e.ToTable("Messages");
            e.HasKey(x => x.Id);
            e.Property(x => x.User).HasMaxLength(80).IsRequired();
            e.Property(x => x.Text).HasMaxLength(2000);
            e.Property(x => x.GifUrl).HasMaxLength(1000);
            e.Property(x => x.Timestamp).IsRequired();
            // Índices útiles
            e.HasIndex(x => x.Timestamp);
        });
    }
}

public sealed class ChatMessageEntity
{
    public int Id { get; set; }
    public string User { get; set; } = "";
    public string? Text { get; set; }
    public string? GifUrl { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
    // (Opcional) Sala/Room
    // public string Room { get; set; } = "general";
}
