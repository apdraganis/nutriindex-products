using Microsoft.EntityFrameworkCore;
using NutriIndex.Products.Models;

namespace NutriIndex.Products.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Barcode);

            entity.Property(e => e.CostPer100Kcal).HasPrecision(18, 1);
            entity.Property(e => e.CostPer10gProtein).HasPrecision(18, 1);
        });
    }
}
