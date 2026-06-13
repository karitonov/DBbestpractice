using CSBestpPactice.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CSBestpPactice.Infrastructure.Repositories.EfCore;

internal sealed class AppDbContext : DbContext
{
    public DbSet<Product> Products { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).HasConversion<string>();
            entity.Property(p => p.Name).IsRequired();
            entity.Property(p => p.UnitPrice).HasColumnType("TEXT");
        });
    }
}
