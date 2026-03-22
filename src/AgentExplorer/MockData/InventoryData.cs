namespace AgentExplorer.MockData;

/// <summary>
/// Simulated inventory and bill of materials data for Vector Technologies.
/// Covers raw materials (resins, additives), finished goods, and BoM relationships.
/// </summary>
public static class InventoryData
{
    public record StockItem(
        string Name,
        string Category,      // RawMaterial, FinishedGood
        decimal Quantity,
        string Unit,
        decimal ReorderPoint,
        string Location);

    public record MaterialSpec(
        string Name,
        string Type,           // Resin, Additive, Colorant
        string Grade,
        string Supplier,
        string Colour,
        DateTime MsdsExpiry);

    public record BomEntry(
        string PartNumber,
        string PartName,
        string MaterialName,
        decimal QuantityPerUnit,
        string Unit);

    public static readonly StockItem[] Stock =
    [
        // Raw materials
        new("HDPE Resin",          "RawMaterial",   2450m, "kg", 500m,  "Warehouse A, Bay 1"),
        new("PP Copolymer",        "RawMaterial",   1800m, "kg", 400m,  "Warehouse A, Bay 2"),
        new("Nylon PA6",           "RawMaterial",   320m,  "kg", 200m,  "Warehouse A, Bay 3"),
        new("ABS Natural",         "RawMaterial",   890m,  "kg", 300m,  "Warehouse A, Bay 4"),
        new("UV Stabiliser",       "RawMaterial",   45m,   "kg", 20m,   "Warehouse B, Shelf 1"),
        new("Titanium Dioxide",    "RawMaterial",   120m,  "kg", 50m,   "Warehouse B, Shelf 2"),
        new("Blue Masterbatch",    "RawMaterial",   65m,   "kg", 30m,   "Warehouse B, Shelf 3"),
        new("Calcium Carbonate",   "RawMaterial",   580m,  "kg", 200m,  "Warehouse B, Shelf 4"),

        // Finished goods
        new("VT-1042 ResMed Mask Clip",      "FinishedGood", 12500m, "pcs", 5000m,  "FG Store, Rack A1"),
        new("VT-2018 Cable Gland Housing",   "FinishedGood", 3200m,  "pcs", 2000m,  "FG Store, Rack A3"),
        new("VT-3005 Conduit Section 25mm",  "FinishedGood", 850m,   "pcs", 500m,   "FG Store, Rack B1"),
        new("VT-3012 Drainage Pipe Fitting", "FinishedGood", 4100m,  "pcs", 1500m,  "FG Store, Rack B2"),
    ];

    public static readonly MaterialSpec[] Materials =
    [
        new("HDPE Resin",        "Resin",     "HMA 025",  "Qenos",           "Natural",  new DateTime(2027, 3, 15)),
        new("PP Copolymer",      "Resin",     "HP548R",   "LyondellBasell",  "Natural",  new DateTime(2026, 11, 30)),
        new("Nylon PA6",         "Resin",     "Ultramid B3S", "BASF",        "Natural",  new DateTime(2027, 1, 20)),
        new("ABS Natural",       "Resin",     "Terluran GP-35", "INEOS",     "Natural",  new DateTime(2026, 9, 10)),
        new("UV Stabiliser",     "Additive",  "Tinuvin 770",   "BASF",       "White",    new DateTime(2026, 8, 5)),
        new("Titanium Dioxide",  "Colorant",  "R-960",    "Chemours",        "White",    new DateTime(2027, 6, 1)),
        new("Blue Masterbatch",  "Colorant",  "VT-BLU-4", "Clariant",       "Blue",     new DateTime(2026, 12, 15)),
        new("Calcium Carbonate", "Additive",  "Omyacarb 2T", "Omya",        "White",    new DateTime(2027, 4, 20)),
    ];

    public static readonly BomEntry[] BillOfMaterials =
    [
        // VT-1042 ResMed Mask Clip — Nylon part
        new("VT-1042", "ResMed Mask Clip",      "Nylon PA6",       0.012m, "kg"),
        new("VT-1042", "ResMed Mask Clip",      "UV Stabiliser",   0.0003m, "kg"),

        // VT-2018 Cable Gland Housing — ABS part
        new("VT-2018", "Cable Gland Housing",   "ABS Natural",     0.035m, "kg"),
        new("VT-2018", "Cable Gland Housing",   "Titanium Dioxide", 0.001m, "kg"),

        // VT-3005 Conduit Section — HDPE part
        new("VT-3005", "Conduit Section 25mm",  "HDPE Resin",      0.45m,  "kg"),
        new("VT-3005", "Conduit Section 25mm",  "UV Stabiliser",   0.005m, "kg"),
        new("VT-3005", "Conduit Section 25mm",  "Blue Masterbatch", 0.009m, "kg"),

        // VT-3012 Drainage Pipe Fitting — PP part
        new("VT-3012", "Drainage Pipe Fitting", "PP Copolymer",    0.18m,  "kg"),
        new("VT-3012", "Drainage Pipe Fitting", "Calcium Carbonate", 0.04m, "kg"),
    ];
}
