using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ContractorDesk.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<DocumentRecord> Documents => Set<DocumentRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQLite has no timezone-aware type; timestamps are always written as UTC, so mark them UTC on
        // the way back out — otherwise they serialize without a "Z" and clients read them as local time.
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            v => v,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        modelBuilder.Entity<DocumentRecord>(entity =>
        {
            entity.ToTable("Documents");
            entity.HasKey(d => d.Id);

            entity.Property(d => d.Type).HasConversion<string>().HasMaxLength(20);
            entity.Property(d => d.DocumentNumber).IsRequired().HasMaxLength(100).UseCollation("NOCASE");
            entity.Property(d => d.CustomerName).IsRequired().HasMaxLength(300);
            entity.Property(d => d.ProjectName).HasMaxLength(300);
            entity.Property(d => d.Payload).IsRequired();
            entity.Property(d => d.CreatedAt).HasConversion(utcConverter);
            entity.Property(d => d.UpdatedAt).HasConversion(utcConverter);

            // Document numbers are unique per type (case-insensitive via the NOCASE collation).
            entity.HasIndex(d => new { d.Type, d.DocumentNumber }).IsUnique();
            entity.HasIndex(d => d.UpdatedAt);
        });
    }
}
