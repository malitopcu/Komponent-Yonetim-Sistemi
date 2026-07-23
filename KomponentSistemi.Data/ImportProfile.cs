namespace KomponentSistemi.Data;

public class ImportProfile
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string Delimiter { get; set; } = ",";

    public string Encoding { get; set; } = "utf-8";

    // "." (DigiKey) ya da "," (Mouser TR)
    public string DecimalSeparator { get; set; } = ".";

    // Kaynak sütun adı → bizim parametremiz, JSON olarak:
    // {"Capacitance": "capacitance", "Voltage - Rated": "voltage"}
    public string MappingsJson { get; set; } = "{}";
}