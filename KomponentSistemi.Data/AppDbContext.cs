using Microsoft.EntityFrameworkCore;

namespace KomponentSistemi.Data;

public class AppDbContext : DbContext
{
    public DbSet<ComponentType> ComponentTypes => Set<ComponentType>();
    public DbSet<ParameterDefinition> ParameterDefinitions => Set<ParameterDefinition>();
    public DbSet<Component> Components => Set<Component>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<ImportProfile> ImportProfiles => Set<ImportProfile>();
    public DbSet<MpnProfile> MpnProfiles => Set<MpnProfile>();

    public DbSet<BomList> BomLists => Set<BomList>();
    public DbSet<BomItem> BomItems => Set<BomItem>();

    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<ImportChange> ImportChanges => Set<ImportChange>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        // Uygulama hangi klasörden çalışırsa çalışsın aynı dosyaya baksın diye sabit yol.
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string dbPath = System.IO.Path.Combine(home, "KomponentSistemi", "components.db");
        options.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tekilleştirmenin veritabanı tarafındaki güvencesi.
        modelBuilder.Entity<Component>()
            .HasIndex(c => new { c.Mpn, c.Manufacturer })
            .IsUnique();

        modelBuilder.Entity<Component>().HasIndex(c => c.PrimaryValueSi);
        modelBuilder.Entity<Component>().HasIndex(c => c.SecondaryValueSi);

        // Aynı komponenti ikinci kez eklemek yeni satır değil, adet artışı olmalı.
        modelBuilder.Entity<BomItem>()
            .HasIndex(i => new { i.BomListId, i.ComponentId })
            .IsUnique();

        // Aynı dosyayı iki kez yüklemek yeni teklif değil, güncelleme olmalı.
        modelBuilder.Entity<Offer>()
            .HasIndex(o => new { o.ComponentId, o.Source })
            .IsUnique();

        modelBuilder.HasDbFunction(typeof(SqlJson).GetMethod(nameof(SqlJson.Extract))!)
            .HasName("json_extract");

        SeedData.Apply(modelBuilder);
    }
}