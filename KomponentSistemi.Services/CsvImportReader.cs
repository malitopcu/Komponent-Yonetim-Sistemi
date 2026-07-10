using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace KomponentSistemi.Services;

public class CsvImportReader
{
    // Okuma sonucunu paketleyen basit yapı
    public class ReadResult
    {
        public List<Dictionary<string, string>> Rows { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
    
    // Ana okuma metodu: dosyayı okur, ham satırları döndürür
    public ReadResult Read(string filePath, string? delimiter = null, string encodingName = "utf-8")
    {
        var result = new ReadResult();
        var encoding = Encoding.GetEncoding(encodingName);

        // Ayraç verilmemişse otomatik algıla
        char delim = string.IsNullOrEmpty(delimiter)
            ? DetectDelimiter(filePath, encoding)
            : delimiter[0];

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delim.ToString(),
            HasHeaderRecord = true,
            BadDataFound = null,      // bozuk veriyi kendimiz ele alacağız
            MissingFieldFound = null  // eksik alan hatası fırlatmasın
        };

        using var reader = new StreamReader(filePath, encoding);
        using var csv = new CsvReader(reader, config);

        // Başlık satırını oku
        csv.Read();
        csv.ReadHeader();
        string[] headers = csv.HeaderRecord ?? Array.Empty<string>();

        // Satırları tek tek oku
        while (csv.Read())
        {
            try
            {
                var row = new Dictionary<string, string>();
                foreach (var header in headers)
                {
                    row[header] = csv.GetField(header) ?? "";
                }
                result.Rows.Add(row);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Satır {csv.Parser.Row}: {ex.Message}");
            }
        }

        return result;
    }
    
    // Dosyanın ilk satırlarına bakıp ayracı tahmin eder
    private static char DetectDelimiter(string filePath, Encoding encoding)
    {
        char[] adaylar = { ',', ';', '\t' };
        using var reader = new StreamReader(filePath, encoding);
        string? ilkSatir = reader.ReadLine();

        if (string.IsNullOrEmpty(ilkSatir))
            return ','; // dosya boşsa varsayılan

        // Hangi aday en çok bölme yapıyorsa (en çok sütun) o kazanır
        char enIyi = ',';
        int enFazlaSutun = 0;
        foreach (char aday in adaylar)
        {
            int sutunSayisi = ilkSatir.Split(aday).Length;
            if (sutunSayisi > enFazlaSutun)
            {
                enFazlaSutun = sutunSayisi;
                enIyi = aday;
            }
        }

        return enIyi;
    }
}