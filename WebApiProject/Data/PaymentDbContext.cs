using Microsoft.EntityFrameworkCore;
using WebApiProject.Models;

namespace WebApiProject.Data;

public class PaymentDbContext : DbContext
{
  public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
      : base(options)
  {
  }

  public DbSet<Charge> Charges { get; set; }
  public DbSet<IdempotencyRecord> IdempotencyRecords { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<Charge>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
      entity.Property(e => e.Currency).HasMaxLength(3);
      entity.Property(e => e.Status).HasMaxLength(50);
      entity.HasIndex(e => e.CustomerId);
      entity.HasIndex(e => e.CreatedAt);
    });

    modelBuilder.Entity<IdempotencyRecord>(entity =>
    {
      entity.HasKey(e => e.IdempotencyKey);
      entity.Property(e => e.IdempotencyKey).HasMaxLength(255);
      entity.HasIndex(e => e.CreatedAt);
    });
  }
}
