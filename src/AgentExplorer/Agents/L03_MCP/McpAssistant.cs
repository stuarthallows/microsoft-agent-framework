using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OllamaSharp;
using AgentExplorer.Agents.L02_ToolAgent;
using AgentExplorer.Shared;

namespace AgentExplorer.Agents.L03_MCP;

/// <summary>
/// Lesson 3: An agent that combines local tools (from L2) with tools discovered
/// dynamically from an external MCP server.
///
/// The key difference from L2: some tools now live in a SEPARATE PROCESS — the
/// SupplierMcpServer project. The agent connects to it via stdio MCP transport,
/// discovers what tools are available, and merges them with its local tools.
///
/// From the agent's perspective, MCP tools and local tools are identical —
/// they're all AITool objects in the same Tools list. The LLM doesn't know
/// or care where a tool runs. This is the MCP value proposition: tool teams
/// can ship independently of agent teams.
///
/// In production, the MCP server could be:
///   - A separate microservice (HTTP transport instead of stdio)
///   - Managed by a different team with its own release cycle
///   - Shared across multiple agents
///   - Versioned independently of the agent
/// </summary>
public class McpAssistant : IChatAgent, IAsyncDisposable
{
    private readonly AIAgent _agent;
    private readonly AgentSession _session;
    private readonly McpClient _mcpClient;

    public string DisplayName => "L3: Production Chat (MCP + Local Tools)";

    private const string SystemPrompt = """
        You are the Production Floor Assistant for Vector Technologies, a plastics
        manufacturing company based in Adelaide with operations in Thailand.

        You have access to two categories of tools:

        LOCAL TOOLS (production data):
        - Stock levels for raw materials and finished goods
        - Material specifications (resin type, grade, supplier)
        - Machine status (running/idle/maintenance, current part, OEE)
        - Material requirement calculations based on the bill of materials

        SUPPLIER TOOLS (via MCP server — external data):
        - Supplier pricing for raw materials (price per kg, lead times)
        - MSDS (Material Safety Data Sheet) expiry status
        - Supplier performance ratings and DIFOT scores
        - Supplier catalog search

        Use the appropriate tool for each query. Supplier-related questions
        (pricing, ratings, MSDS) should use the supplier tools. Production-related
        questions (stock, machines, BoM) should use the local tools.

        Do not guess or invent data — always call the relevant tool.

        Boundaries:
        - Never authorise starting a run, releasing a quality hold, or overriding
          safety procedures. Direct the user to the appropriate supervisor.
        - For questions outside production and purchasing (sales, HR), say you're
          the production assistant and suggest they contact the relevant department.

        Style: Be direct and concise. Vary your phrasing naturally.
        """;

    private McpAssistant(AIAgent agent, AgentSession session, McpClient mcpClient)
    {
        _agent = agent;
        _session = session;
        _mcpClient = mcpClient;
    }

    /// <summary>
    /// Factory method — async because MCP client connection is async.
    /// The MCP handshake (connecting to the server, discovering tools) must
    /// complete before the agent can be used.
    /// </summary>
    public static async Task<McpAssistant> CreateAsync()
    {
        var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434";
        var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "qwen3:8b";

        // Connect to the SupplierMcpServer via stdio transport.
        // This launches the server as a child process and communicates
        // via stdin/stdout using the MCP protocol.
        var mcpClient = await McpClient.CreateAsync(
            new StdioClientTransport(new()
            {
                Name = "SupplierServer",
                Command = "dotnet",
                Arguments = ["run", "--project",
                    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SupplierMcpServer")],
            }));

        // Discover tools from the MCP server — these are returned as
        // McpClientTool objects which implement AITool.
        var mcpTools = await mcpClient.ListToolsAsync();

        // Combine MCP tools with local tools from L2
        var localTools = new List<AITool>
        {
            AIFunctionFactory.Create(ProductionTools.GetStockLevel),
            AIFunctionFactory.Create(ProductionTools.LookupMaterialSpec),
            AIFunctionFactory.Create(ProductionTools.CheckMachineStatus),
            AIFunctionFactory.Create(ProductionTools.CalculateMaterialRequirement),
        };

        var allTools = new List<AITool>();
        allTools.AddRange(mcpTools);
        allTools.AddRange(localTools);

        var agent = new OllamaApiClient(new Uri(endpoint), model)
            .AsAIAgent(new ChatClientAgentOptions
            {
                Name = "VectorMcpAssistant",
                ChatOptions = new()
                {
                    Instructions = SystemPrompt,
                    Temperature = 0f,
                    Tools = allTools,
                }
            });

        var session = await agent.CreateSessionAsync();

        return new McpAssistant(agent, session, mcpClient);
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(string userMessage)
    {
        await foreach (var update in _agent.RunStreamingAsync(userMessage, _session))
        {
            if (update.Text is not null)
            {
                yield return update.Text;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _mcpClient.DisposeAsync();
    }
}
