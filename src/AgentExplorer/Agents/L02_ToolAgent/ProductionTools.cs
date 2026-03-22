using System.ComponentModel;
using AgentExplorer.MockData;

namespace AgentExplorer.Agents.L02_ToolAgent;

/// <summary>
/// Lesson 2: Function tools that the agent can call autonomously.
///
/// Each method is a tool the LLM can invoke during conversation. MAF discovers
/// them via AIFunctionFactory.Create, which uses reflection to read:
///   - The method name (becomes the tool name)
///   - The [Description] attribute (tells the LLM what the tool does)
///   - Parameter names and their [Description] attributes (tells the LLM what to pass)
///   - The return type (string — the result shown to the LLM)
///
/// The LLM decides WHEN to call a tool based on the user's question and the
/// tool descriptions. It also decides WHICH tool and WHAT arguments to pass.
/// The framework handles the plumbing — serialising arguments, invoking the
/// method, and feeding the result back into the conversation.
/// </summary>
public static class ProductionTools
{
    [Description("Get the current stock level for a raw material or finished good. Returns quantity, unit, location, and whether stock is below reorder point.")]
    public static string GetStockLevel(
        [Description("The name of the material or product to check, e.g. 'HDPE Resin' or 'VT-1042'")] string itemName)
    {
        var match = InventoryData.Stock
            .FirstOrDefault(s => s.Name.Contains(itemName, StringComparison.OrdinalIgnoreCase));

        if (match is null)
            return $"No stock record found for '{itemName}'. Check the item name and try again.";

        var belowReorder = match.Quantity <= match.ReorderPoint;
        return $"{match.Name}: {match.Quantity} {match.Unit} in {match.Location}. " +
               $"Reorder point: {match.ReorderPoint} {match.Unit}. " +
               (belowReorder ? "WARNING: Stock is at or below reorder point!" : "Stock level is healthy.");
    }

    [Description("Look up the material specification for a raw material including resin type, grade, supplier, colour, and MSDS expiry date.")]
    public static string LookupMaterialSpec(
        [Description("The name of the material, e.g. 'HDPE Resin' or 'Nylon PA6'")] string materialName)
    {
        var match = InventoryData.Materials
            .FirstOrDefault(m => m.Name.Contains(materialName, StringComparison.OrdinalIgnoreCase));

        if (match is null)
            return $"No material specification found for '{materialName}'.";

        return $"{match.Name} — Type: {match.Type}, Grade: {match.Grade}, " +
               $"Supplier: {match.Supplier}, Colour: {match.Colour}, " +
               $"MSDS Expiry: {match.MsdsExpiry:dd MMM yyyy}";
    }

    [Description("Check the current status of a production machine including what part it's running, its OEE percentage, and which cell it belongs to.")]
    public static string CheckMachineStatus(
        [Description("The machine name, e.g. 'INJ-01' or 'EXT-02'")] string machineName)
    {
        var match = ProductionData.Machines
            .FirstOrDefault(m => m.Name.Equals(machineName, StringComparison.OrdinalIgnoreCase));

        if (match is null)
            return $"No machine found with name '{machineName}'. Available machines: {string.Join(", ", ProductionData.Machines.Select(m => m.Name))}";

        var partInfo = match.CurrentPart is not null
            ? $"Running part {match.CurrentPart}, OEE: {match.OeePercent}%"
            : "No part assigned";

        return $"{match.Name} ({match.Cell}): Status = {match.Status}. {partInfo}";
    }

    // --- Typed tool return vs string return ---
    //
    // The tools above return formatted strings. That works well for simple,
    // single-record lookups where the result is easy to express in one sentence.
    //
    // For production systems where tools return complex data — multiple stock items,
    // nested BoM trees, multi-row query results — returning a typed record/object
    // and letting the framework JSON-serialise it is cleaner than manually building
    // strings. The LLM handles JSON well, and typed returns give you:
    //   - Compile-time safety on the result structure
    //   - Automatic serialisation (no manual StringBuilder work)
    //   - Consistent format the LLM can reliably parse
    //
    // MAF serialises non-string return values to JSON before injecting them into
    // the conversation. The LLM reads the JSON and formulates a natural-language
    // response from it — the user never sees raw JSON.
    //

    public record MaterialRequirementResult(
        string PartNumber,
        string PartName,
        int RequestedQuantity,
        List<MaterialRequirementLine> Materials);

    public record MaterialRequirementLine(
        string MaterialName,
        decimal QuantityNeeded,
        string Unit,
        decimal? CurrentStock,
        bool IsSufficient);

    [Description("Calculate the total raw material quantities needed to produce a given number of units of a part, based on the bill of materials. Returns a structured result with each material's required quantity and current stock sufficiency.")]
    public static MaterialRequirementResult CalculateMaterialRequirement(
        [Description("The part number, e.g. 'VT-1042'")] string partNumber,
        [Description("The number of units to produce")] int quantity)
    {
        var bomEntries = InventoryData.BillOfMaterials
            .Where(b => b.PartNumber.Equals(partNumber, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (bomEntries.Count == 0)
            return new MaterialRequirementResult(partNumber, "Unknown", quantity, []);

        var lines = bomEntries.Select(entry =>
        {
            var totalNeeded = entry.QuantityPerUnit * quantity;
            var stock = InventoryData.Stock
                .FirstOrDefault(s => s.Name.Equals(entry.MaterialName, StringComparison.OrdinalIgnoreCase));

            return new MaterialRequirementLine(
                entry.MaterialName,
                totalNeeded,
                entry.Unit,
                stock?.Quantity,
                stock is not null && stock.Quantity >= totalNeeded);
        }).ToList();

        return new MaterialRequirementResult(
            partNumber,
            bomEntries[0].PartName,
            quantity,
            lines);
    }
}
