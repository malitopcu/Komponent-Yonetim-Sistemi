namespace KomponentSistemi.Data;

public class ImportProfile
{
    public int Id { get; set; }

    // Profilin adı ("DigiKey CSV", "Mouser TR" gibi)
    public string Name { get; set; } = "";

    // Ayraç: virgül, noktalı virgül, sekme...
    public string Delimiter { get; set; } = ",";

    // Dosya kodlaması ("utf-8", "windows-1254"...)
    public string Encoding { get; set; } = "utf-8";

    // Ondalık ayracı: "." (DigiKey) ya da "," (Mouser TR)
    public string DecimalSeparator { get; set; } = ".";

    // Sütun eşlemeleri: kaynağın sütun adı → bizim parametremiz
    // JSON olarak saklanır, ör: {"Capacitance": "capacitance", "Voltage - Rated": "voltage"}
    public string MappingsJson { get; set; } = "{}";
}