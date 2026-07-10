using Microsoft.EntityFrameworkCore;

namespace KomponentSistemi.Data;

public class AppDbContext : DbContext
{
    // Her DbSet, bir tabloya karşılık gelir
    public DbSet<ComponentType> ComponentTypes => Set<ComponentType>();
    public DbSet<ParameterDefinition> ParameterDefinitions => Set<ParameterDefinition>();
    public DbSet<Component> Components => Set<Component>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<ImportProfile> ImportProfiles => Set<ImportProfile>();

    // Veritabanı dosyasının yeri ve türü
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite("Data Source=components.db");
    }

    // İlişkiler, indeksler, ince ayarlar
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // MPN benzersiz olmalı — tekilleştirmenin veritabanı güvencesi
        modelBuilder.Entity<Component>()
            .HasIndex(c => new { c.Mpn, c.Manufacturer })
            .IsUnique();

        // Sıcak arama sütunlarına indeks — hızlı aralık sorgusu
        modelBuilder.Entity<Component>().HasIndex(c => c.CapacitanceF);
        modelBuilder.Entity<Component>().HasIndex(c => c.VoltageV);
    }
}