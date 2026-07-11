using KomponentSistemi.Data;

namespace KomponentSistemi.Services;

// UI, Data'yı göremez; bu yüzden servislerin "doğumu" burada yapılır.
// UI sadece "bana bir sorgu servisi ver" der, içeride ne olduğunu bilmez.
public static class ServiceFactory
{
    public static ComponentQueryService CreateComponentQueryService()
        => new ComponentQueryService(new AppDbContext());
    
    public static SearchService CreateSearchService()
        => new SearchService(new AppDbContext());
}