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

    // BOM (proje / malzeme listesi) tabloları
    public DbSet<BomList> BomLists => Set<BomList>();
    public DbSet<BomItem> BomItems => Set<BomItem>();

    // İçe aktarma geçmişi (geri alma için)
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<ImportChange> ImportChanges => Set<ImportChange>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        // Veritabanını kullanıcının ana klasöründe sabit bir yere koy
        // (böylece hangi klasörden çalışırsa çalışsın aynı dosyaya bakar)
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string dbPath = System.IO.Path.Combine(home, "KomponentSistemi", "components.db");
        options.UseSqlite($"Data Source={dbPath}");
    }

    // İlişkiler, indeksler, ince ayarlar
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // MPN benzersiz olmalı — tekilleştirmenin veritabanı güvencesi
        modelBuilder.Entity<Component>()
            .HasIndex(c => new { c.Mpn, c.Manufacturer })
            .IsUnique();

        modelBuilder.Entity<Component>().HasIndex(c => c.PrimaryValueSi);
        modelBuilder.Entity<Component>().HasIndex(c => c.SecondaryValueSi);

        // Bir komponent, bir projede en fazla bir satır olur.
        // (İkinci kez "projeye ekle" → yeni satır değil, adet artışı.)
        modelBuilder.Entity<BomItem>()
            .HasIndex(i => new { i.BomListId, i.ComponentId })
            .IsUnique();

        // Bir komponentin bir satıcıdan EN FAZLA bir teklifi olur.
        // (Aynı dosyayı 2 kez yüklemek → yeni teklif değil, mevcut teklifin güncellenmesi.)
        modelBuilder.Entity<Offer>()
            .HasIndex(o => new { o.ComponentId, o.Source })
            .IsUnique();
        
        modelBuilder.HasDbFunction(typeof(SqlJson).GetMethod(nameof(SqlJson.Extract))!)
            .HasName("json_extract");
        
        SeedData.Apply(modelBuilder);
    }
}