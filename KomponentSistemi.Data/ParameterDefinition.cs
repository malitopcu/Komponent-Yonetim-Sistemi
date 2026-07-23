namespace KomponentSistemi.Data;

public class ParameterDefinition
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Unit { get; set; }
    public string DataType { get; set; } = "";
    public bool IsSearchable { get; set; }

    // Bu parametre hangi sıcak sütuna yazılır? "primary", "secondary" ya da null (JSON'a gider)
    public string? HotColumn { get; set; }

    // İlişki: bu parametre hangi komponent tipine ait?
    public int ComponentTypeId { get; set; }
    public ComponentType ComponentType { get; set; } = null!;
}