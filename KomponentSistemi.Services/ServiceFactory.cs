using KomponentSistemi.Data;

namespace KomponentSistemi.Services;

// UI, Data'yı göremez; bu yüzden servislerin "doğumu" burada yapılır.
public static class ServiceFactory
{
    public static ComponentQueryService CreateComponentQueryService()
        => new ComponentQueryService(new AppDbContext());

    public static SearchService CreateSearchService()
        => new SearchService(new AppDbContext());

    public static ImportService CreateImportService()
        => new ImportService(new AppDbContext());
}