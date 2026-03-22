using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SupplierMcpServer;

/// <summary>
/// Lesson 3: MCP server tools for supplier and material data.
///
/// These tools are identical in concept to the L2 function tools — methods with
/// [Description] attributes that the LLM can call. The difference is where they
/// run: these execute in a SEPARATE PROCESS (the MCP server), not inside the
/// agent's process.
///
/// The key attribute change: [McpServerTool] instead of just [Description].
/// The [McpServerToolType] on the class tells the MCP SDK to discover all
/// [McpServerTool] methods in this class during server startup.
///
/// From the agent's perspective, MCP tools look identical to local tools —
/// they show up as AITool objects with the same name/description/parameter
/// interface. The agent doesn't know or care whether a tool is local or remote.
/// </summary>
[McpServerToolType]
public class SupplierTools
{
    [McpServerTool, Description("Get current pricing for a raw material from all suppliers. Returns price per kg, currency, lead time, and when the price was last updated.")]
    public static string GetSupplierPricing(
        [Description("The material name, e.g. 'HDPE Resin' or 'Nylon PA6'")] string materialName)
    {
        var matches = SupplierData.Pricing
            .Where(p => p.MaterialName.Contains(materialName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
            return $"No pricing data found for '{materialName}'.";

        return string.Join("\n", matches.Select(p =>
            $"{p.MaterialName} from {p.Supplier}: ${p.PricePerKg}/kg {p.Currency}, " +
            $"lead time {p.LeadTimeDays} days, last updated {p.LastUpdated:dd MMM yyyy}"));
    }

    [McpServerTool, Description("Check the MSDS (Material Safety Data Sheet) expiry status for a material. Returns the document reference, expiry date, and whether it needs renewal.")]
    public static string CheckMsdsExpiry(
        [Description("The material name to check MSDS status for")] string materialName)
    {
        var match = SupplierData.MsdsRecords
            .FirstOrDefault(m => m.MaterialName.Contains(materialName, StringComparison.OrdinalIgnoreCase));

        if (match is null)
            return $"No MSDS record found for '{materialName}'.";

        var daysUntilExpiry = (match.ExpiryDate - DateTime.Today).Days;
        var status = daysUntilExpiry switch
        {
            < 0 => "EXPIRED — renewal required immediately",
            < 30 => $"EXPIRING SOON — {daysUntilExpiry} days remaining, initiate renewal",
            < 90 => $"Due for renewal — {daysUntilExpiry} days remaining",
            _ => $"Valid — {daysUntilExpiry} days until expiry"
        };

        return $"{match.MaterialName} ({match.Supplier}): {status}. " +
               $"Expiry: {match.ExpiryDate:dd MMM yyyy}. Ref: {match.DocumentRef}";
    }

    [McpServerTool, Description("Get a supplier's performance rating including their DIFOT (Delivery In Full On Time) percentage, rating grade, location, and number of active materials.")]
    public static string GetSupplierRating(
        [Description("The supplier name, e.g. 'Qenos' or 'BASF'")] string supplierName)
    {
        var match = SupplierData.Suppliers
            .FirstOrDefault(s => s.Name.Contains(supplierName, StringComparison.OrdinalIgnoreCase));

        if (match is null)
            return $"No supplier found with name '{supplierName}'. Available suppliers: {string.Join(", ", SupplierData.Suppliers.Select(s => s.Name))}";

        return $"{match.Name} ({match.Location}): Rating {match.Rating}, " +
               $"DIFOT {match.DifotPercent}%, {match.ActiveMaterials} active materials. " +
               $"Notes: {match.Notes}";
    }

    [McpServerTool, Description("Search the supplier catalog for available materials matching a query. Returns all matching materials with their suppliers and pricing.")]
    public static string SearchSupplierCatalog(
        [Description("Search term for materials, e.g. 'resin' or 'stabiliser'")] string query)
    {
        var matches = SupplierData.Pricing
            .Where(p => p.MaterialName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        p.Supplier.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
            return $"No materials found matching '{query}'.";

        return $"Found {matches.Count} results:\n" +
               string.Join("\n", matches.Select(p =>
                   $"  - {p.MaterialName} from {p.Supplier}: ${p.PricePerKg}/kg, {p.LeadTimeDays}-day lead time"));
    }
}
