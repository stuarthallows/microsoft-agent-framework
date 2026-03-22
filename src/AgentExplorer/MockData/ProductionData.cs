namespace AgentExplorer.MockData;

/// <summary>
/// Simulated production floor data for Vector Technologies.
/// Represents machines, current production runs, and their status.
/// </summary>
public static class ProductionData
{
    public record Machine(
        string Name,
        string Cell,
        string Status,       // Running, Idle, Maintenance
        string? CurrentPart,
        decimal OeePercent);

    public record ProductionRun(
        string PartNumber,
        string PartName,
        string Machine,
        int TargetQuantity,
        int CompletedQuantity,
        DateTime StartTime);

    public static readonly Machine[] Machines =
    [
        new("INJ-01", "Cell A", "Running",     "VT-1042", 87.5m),
        new("INJ-02", "Cell A", "Running",     "VT-2018", 91.2m),
        new("INJ-03", "Cell B", "Idle",        null,      0m),
        new("INJ-04", "Cell B", "Running",     "VT-1042", 84.3m),
        new("INJ-05", "Cell C", "Maintenance", null,      0m),
        new("EXT-01", "Cell D", "Running",     "VT-3005", 78.9m),
        new("EXT-02", "Cell D", "Running",     "VT-3012", 92.1m),
        new("BLO-01", "Cell E", "Idle",        null,      0m),
    ];

    public static readonly ProductionRun[] ActiveRuns =
    [
        new("VT-1042", "ResMed Mask Clip",       "INJ-01", 10000, 6230, DateTime.Today.AddHours(6)),
        new("VT-2018", "Cable Gland Housing",    "INJ-02", 5000,  3800, DateTime.Today.AddHours(7)),
        new("VT-1042", "ResMed Mask Clip",       "INJ-04", 10000, 4100, DateTime.Today.AddHours(6.5)),
        new("VT-3005", "Conduit Section 25mm",   "EXT-01", 2000,  1450, DateTime.Today.AddHours(5)),
        new("VT-3012", "Drainage Pipe Fitting",  "EXT-02", 3000,  2900, DateTime.Today.AddHours(4)),
    ];
}
