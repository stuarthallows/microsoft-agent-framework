using AgentExplorer.Shared;

namespace AgentExplorer.Agents.L03_MCP;

/// <summary>
/// Wrapper that lazily initialises the McpAssistant on first use.
///
/// McpAssistant.CreateAsync() is async (it launches the MCP server process
/// and performs the MCP handshake). We can't do that in the MainWindow
/// constructor, so this wrapper defers initialisation to the first message.
/// </summary>
public class LazyMcpAgent : IChatAgent
{
    private McpAssistant? _agent;
    private string? _initError;

    public string DisplayName => "L3: Production Chat (MCP + Local Tools)";

    public async IAsyncEnumerable<string> StreamResponseAsync(string userMessage)
    {
        if (_agent is null && _initError is null)
        {
            yield return "[Connecting to MCP server...]\n";
            await InitialiseAsync();
        }

        if (_initError is not null)
        {
            yield return $"[MCP error: {_initError}]\n";
            yield return "Hint: Make sure the SupplierMcpServer project builds.\n";
            yield break;
        }

        await foreach (var chunk in _agent!.StreamResponseAsync(userMessage))
        {
            yield return chunk;
        }
    }

    private async Task InitialiseAsync()
    {
        try
        {
            _agent = await McpAssistant.CreateAsync();
        }
        catch (Exception ex)
        {
            _initError = ex.Message;
        }
    }
}
