namespace SupplierMcpServer;

/// <summary>
/// Mock supplier data for Vector Technologies.
/// In a real system this would come from a database or external API —
/// the MCP server abstracts that away from the agent.
/// </summary>
public static class SupplierData
{
    public record SupplierInfo(
        string Name,
        string Location,
        decimal DifotPercent,       // Delivery In Full On Time
        string Rating,              // A, B, C
        int ActiveMaterials,
        string Notes);

    public record MaterialPricing(
        string MaterialName,
        string Supplier,
        decimal PricePerKg,
        string Currency,
        int LeadTimeDays,
        DateTime LastUpdated);

    public record MsdsRecord(
        string MaterialName,
        string Supplier,
        DateTime ExpiryDate,
        string DocumentRef,
        bool IsExpired);

    public static readonly SupplierInfo[] Suppliers =
    [
        new("Qenos",           "Melbourne, AU",   94.2m,  "A", 3, "Primary HDPE supplier, long-term contract"),
        new("LyondellBasell",  "Rotterdam, NL",   88.5m,  "B", 2, "PP copolymer, sea freight 6-8 weeks"),
        new("BASF",            "Ludwigshafen, DE", 96.1m, "A", 4, "Nylon PA6 and UV stabilisers"),
        new("INEOS",           "Cologne, DE",      85.3m, "B", 1, "ABS supplier, occasional quality holds"),
        new("Chemours",        "Wilmington, US",   91.7m, "A", 2, "Titanium dioxide, consistent quality"),
        new("Clariant",        "Muttenz, CH",      89.0m, "B", 1, "Masterbatch and colorants"),
        new("Omya",            "Oftringen, CH",    97.3m, "A", 3, "Calcium carbonate, excellent DIFOT"),
    ];

    public static readonly MaterialPricing[] Pricing =
    [
        new("HDPE Resin",        "Qenos",          2.85m,  "AUD", 14,  new DateTime(2026, 3, 1)),
        new("HDPE Resin",        "LyondellBasell", 2.95m,  "AUD", 42,  new DateTime(2026, 2, 15)),
        new("PP Copolymer",      "LyondellBasell", 3.10m,  "AUD", 42,  new DateTime(2026, 3, 5)),
        new("Nylon PA6",         "BASF",           6.50m,  "AUD", 28,  new DateTime(2026, 3, 10)),
        new("ABS Natural",       "INEOS",          4.20m,  "AUD", 21,  new DateTime(2026, 2, 28)),
        new("UV Stabiliser",     "BASF",           18.90m, "AUD", 28,  new DateTime(2026, 3, 8)),
        new("Titanium Dioxide",  "Chemours",       5.60m,  "AUD", 35,  new DateTime(2026, 3, 12)),
        new("Blue Masterbatch",  "Clariant",       12.40m, "AUD", 21,  new DateTime(2026, 3, 1)),
        new("Calcium Carbonate", "Omya",           0.85m,  "AUD", 7,   new DateTime(2026, 3, 15)),
    ];

    public static readonly MsdsRecord[] MsdsRecords =
    [
        new("HDPE Resin",        "Qenos",          new DateTime(2027, 3, 15), "MSDS-QEN-HDPE-2024",   false),
        new("PP Copolymer",      "LyondellBasell", new DateTime(2026, 11, 30),"MSDS-LYB-PP-2023",     false),
        new("Nylon PA6",         "BASF",           new DateTime(2027, 1, 20), "MSDS-BASF-PA6-2024",   false),
        new("ABS Natural",       "INEOS",          new DateTime(2026, 9, 10), "MSDS-INEOS-ABS-2023",  false),
        new("UV Stabiliser",     "BASF",           new DateTime(2026, 4, 5),  "MSDS-BASF-UV770-2023", false),
        new("Titanium Dioxide",  "Chemours",       new DateTime(2027, 6, 1),  "MSDS-CHEM-TIO2-2024",  false),
        new("Blue Masterbatch",  "Clariant",       new DateTime(2026, 12, 15),"MSDS-CLAR-BLU4-2023",  false),
        new("Calcium Carbonate", "Omya",           new DateTime(2027, 4, 20), "MSDS-OMYA-CC2T-2024",  false),
    ];
}
