using Microsoft.EntityFrameworkCore;

namespace KomponentSistemi.Data;

public static class SeedData
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        // ===== KOMPONENT TİPLERİ =====
        modelBuilder.Entity<ComponentType>().HasData(
            new ComponentType { Id = 1, Name = "Kondansatör" },
            new ComponentType { Id = 2, Name = "Direnç" },
            new ComponentType { Id = 3, Name = "Diyot" },
            new ComponentType { Id = 4, Name = "Transistör" },
            new ComponentType { Id = 5, Name = "Osilatör" },
            new ComponentType { Id = 6, Name = "Regülatör" }
        );

        modelBuilder.Entity<ParameterDefinition>().HasData(
            // --- Kondansatör (Id=1) ---
            new ParameterDefinition
            {
                Id = 101, ComponentTypeId = 1, Key = "capacitance", DisplayName = "Kapasitans", Unit = "F",
                DataType = "numeric", IsSearchable = true, HotColumn = "primary"
            },
            new ParameterDefinition
            {
                Id = 102, ComponentTypeId = 1, Key = "voltage", DisplayName = "Anma Gerilimi", Unit = "V",
                DataType = "numeric", IsSearchable = true, HotColumn = "secondary"
            },
            new ParameterDefinition
            {
                Id = 103, ComponentTypeId = 1, Key = "dielectric", DisplayName = "Dielektrik", Unit = null,
                DataType = "text", IsSearchable = true, HotColumn = null
            },
            new ParameterDefinition
            {
                Id = 104, ComponentTypeId = 1, Key = "tolerance", DisplayName = "Tolerans", Unit = null,
                DataType = "text", IsSearchable = false, HotColumn = null
            },
            new ParameterDefinition
            {
                Id = 105, ComponentTypeId = 1, Key = "package", DisplayName = "Paket", Unit = null, DataType = "text",
                IsSearchable = true, HotColumn = null
            },

            // --- Direnç (Id=2) ---
            new ParameterDefinition
            {
                Id = 201, ComponentTypeId = 2, Key = "resistance", DisplayName = "Direnç", Unit = "Ω",
                DataType = "numeric", IsSearchable = true, HotColumn = "primary"
            },
            new ParameterDefinition
            {
                Id = 202, ComponentTypeId = 2, Key = "power", DisplayName = "Güç", Unit = "W", DataType = "numeric",
                IsSearchable = true, HotColumn = "secondary"
            },
            new ParameterDefinition
            {
                Id = 203, ComponentTypeId = 2, Key = "tolerance", DisplayName = "Tolerans", Unit = null,
                DataType = "text", IsSearchable = false, HotColumn = null
            },
            new ParameterDefinition
            {
                Id = 204, ComponentTypeId = 2, Key = "package", DisplayName = "Paket", Unit = null, DataType = "text",
                IsSearchable = true, HotColumn = null
            },

            // --- Diyot (Id=3) ---
            new ParameterDefinition
            {
                Id = 301, ComponentTypeId = 3, Key = "reverse_voltage", DisplayName = "Ters Gerilim (Vr)", Unit = "V",
                DataType = "numeric", IsSearchable = true, HotColumn = "primary"
            },
            new ParameterDefinition
            {
                Id = 302, ComponentTypeId = 3, Key = "forward_current", DisplayName = "İleri Akım (If)", Unit = "A",
                DataType = "numeric", IsSearchable = true, HotColumn = "secondary"
            },
            new ParameterDefinition
            {
                Id = 303, ComponentTypeId = 3, Key = "forward_voltage", DisplayName = "İleri Gerilim (Vf)", Unit = "V",
                DataType = "numeric", IsSearchable = false, HotColumn = null
            },
            new ParameterDefinition
            {
                Id = 304, ComponentTypeId = 3, Key = "subtype", DisplayName = "Alt Tür", Unit = null, DataType = "text",
                IsSearchable = true, HotColumn = null
            },
            new ParameterDefinition
            {
                Id = 305, ComponentTypeId = 3, Key = "package", DisplayName = "Paket", Unit = null, DataType = "text",
                IsSearchable = true, HotColumn = null
            },

            // --- Transistör (Id=4) ---
            new ParameterDefinition
            {
                Id = 401, ComponentTypeId = 4, Key = "voltage", DisplayName = "Gerilim (Vds/Vce)", Unit = "V",
                DataType = "numeric", IsSearchable = true, HotColumn = "primary"
            },
            new ParameterDefinition
            {
                Id = 402, ComponentTypeId = 4, Key = "current", DisplayName = "Akım", Unit = "A", DataType = "numeric",
                IsSearchable = true, HotColumn = "secondary"
            },
            new ParameterDefinition
            {
                Id = 403, ComponentTypeId = 4, Key = "power", DisplayName = "Güç", Unit = "W", DataType = "numeric",
                IsSearchable = false, HotColumn = null
            },
            new ParameterDefinition
            {
                Id = 404, ComponentTypeId = 4, Key = "subtype", DisplayName = "Alt Tür", Unit = null, DataType = "text",
                IsSearchable = true, HotColumn = null
            },
            new ParameterDefinition
            {
                Id = 405, ComponentTypeId = 4, Key = "package", DisplayName = "Paket", Unit = null, DataType = "text",
                IsSearchable = true, HotColumn = null
            },

            // --- Osilatör (Id=5) ---
            new ParameterDefinition
            {
                Id = 501, ComponentTypeId = 5, Key = "frequency", DisplayName = "Frekans", Unit = "Hz",
                DataType = "numeric", IsSearchable = true, HotColumn = "primary"
            },
            new ParameterDefinition
            {
                Id = 502, ComponentTypeId = 5, Key = "supply_voltage", DisplayName = "Besleme Gerilimi", Unit = "V",
                DataType = "numeric", IsSearchable = true, HotColumn = "secondary"
            },
            new ParameterDefinition
            {
                Id = 503, ComponentTypeId = 5, Key = "frequency_tolerance", DisplayName = "Frekans Toleransı",
                Unit = "ppm", DataType = "numeric", IsSearchable = false, HotColumn = null
            },
            new ParameterDefinition
            {
                Id = 504, ComponentTypeId = 5, Key = "package", DisplayName = "Paket", Unit = null, DataType = "text",
                IsSearchable = true, HotColumn = null
            },
            new ParameterDefinition
            {
                // DigiKey "Package / Case" osilatörde boyutsuz ("4-SMD, No Lead"); gerçek boyut
                // "Size / Dimension" sütununda. Footprint seçimi için boyutu ayrı param olarak tutuyoruz.
                Id = 505, ComponentTypeId = 5, Key = "size", DisplayName = "Boyut", Unit = null, DataType = "text",
                IsSearchable = false, HotColumn = null
            },

            // --- Regülatör (Id=6) ---
            new ParameterDefinition
            {
                Id = 601, ComponentTypeId = 6, Key = "output_voltage", DisplayName = "Çıkış Gerilimi", Unit = "V",
                DataType = "numeric", IsSearchable = true, HotColumn = "primary"
            },
            new ParameterDefinition
            {
                Id = 602, ComponentTypeId = 6, Key = "output_current", DisplayName = "Çıkış Akımı", Unit = "A",
                DataType = "numeric", IsSearchable = true, HotColumn = "secondary"
            },
            new ParameterDefinition
            {
                Id = 603, ComponentTypeId = 6, Key = "subtype", DisplayName = "Alt Tür", Unit = null, DataType = "text",
                IsSearchable = true, HotColumn = null
            },
            new ParameterDefinition
            {
                Id = 604, ComponentTypeId = 6, Key = "package", DisplayName = "Paket", Unit = null, DataType = "text",
                IsSearchable = true, HotColumn = null
            }
        );
    }
}