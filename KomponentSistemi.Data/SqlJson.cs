namespace KomponentSistemi.Data;

// EF sorguları içinde kullanılacak SQL fonksiyon "vekilleri".
// Gövdeleri asla çalışmaz; EF bunları görünce SQL karşılığını yazar.
public static class SqlJson
{
    public static string? Extract(string json, string path)
        => throw new NotSupportedException("Sadece EF sorgusu içinde kullanılabilir.");
}