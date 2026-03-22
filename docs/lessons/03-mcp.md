# Lesson 3: MCP Integration

## What you'll learn
- How MCP separates tool implementation from tool consumption
- Creating a .NET MCP server with `[McpServerTool]` attributes
- Connecting an agent to an MCP server via stdio transport
- Mixing MCP tools and local tools in the same agent

## Architecture

```
                    User: "What's the price of HDPE resin?"
                                    │
                                    ▼
                        ┌───────────────────────┐
                        │  McpAssistant         │
                        │  ┌─────────────────┐  │
                        │  │ AIAgent         │  │
                        │  │  Local tools +  │  │
                        │  │  MCP tools      │  │
                        │  └────────┬────────┘  │
                        └───────────┼───────────┘
                                    │
             ┌──────────────────────┼──────────────────────┐
             │                      │                      │
    ┌────────▼────────┐   ┌────────▼──────────┐   ┌────────▼──────────┐
    │ Local Tool      │   │ MCP Tool          │   │ MCP Tool          │
    │ GetStockLevel   │   │ GetSupplierPricing│   │ CheckMsdsExpiry   │
    │ (in-process)    │   │ (separate process)│   │ (separate process)│
    └─────────────────┘   └────────┬──────────┘   └────────┬──────────┘
                                   │                       │
                          ┌────────▼───────────────────────▼───────┐
                          │  SupplierMcpServer (child process)     │
                          │  Communicates via stdin/stdout (MCP)   │
                          │  Has its own data, deps, release cycle │
                          └────────────────────────────────────────┘
```

## How MCP works

### The problem MCP solves
In L2, tools were C# methods compiled into the same binary as the agent. This works for a learning project but creates problems at scale:
- Every tool change requires redeploying the agent
- Tool teams can't ship independently
- All tools share the agent's dependencies and runtime
- You can't reuse tools across different agents

MCP (Model Context Protocol) solves this by putting tools in a separate process that communicates via a standardised protocol.

### Server side: `[McpServerTool]`

An MCP server is a regular .NET app with tools decorated with `[McpServerTool]`:

```csharp
[McpServerToolType]
public class SupplierTools
{
    [McpServerTool, Description("Get supplier pricing for a material")]
    public static string GetSupplierPricing(string materialName) => ...;
}
```

The hosting setup uses `Microsoft.Extensions.Hosting`:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddMcpServer().WithStdioServerTransport().WithTools<SupplierTools>();
await builder.Build().RunAsync();
```

### Client side: `McpClient.CreateAsync`

The agent connects to the MCP server via stdio transport:

```csharp
var mcpClient = await McpClient.CreateAsync(
    new StdioClientTransport(new()
    {
        Name = "SupplierServer",
        Command = "dotnet",
        Arguments = ["run", "--project", "path/to/SupplierMcpServer"],
    }));

var mcpTools = await mcpClient.ListToolsAsync();
```

The returned `mcpTools` are `AITool` objects — the same interface as `AIFunctionFactory.Create`. You can mix them freely with local tools.

## Files

| File | Purpose |
|------|---------|
| `src/SupplierMcpServer/Program.cs` | MCP server entry point with hosting |
| `src/SupplierMcpServer/SupplierTools.cs` | 4 MCP tools: pricing, MSDS, ratings, catalog search |
| `src/SupplierMcpServer/SupplierData.cs` | Mock supplier data (pricing, DIFOT, MSDS records) |
| `src/AgentExplorer/Agents/L03_MCP/McpAssistant.cs` | Agent combining MCP + local tools |
| `src/AgentExplorer/Agents/L03_MCP/LazyMcpAgent.cs` | Deferred init wrapper (MCP connection is async) |

## Running it

```bash
dotnet run --project src/AgentExplorer
```

Select the **L3: MCP** tab. The first message will trigger the MCP server connection. Try:
- "What's the price of HDPE resin?" → MCP tool (GetSupplierPricing)
- "Is the MSDS for UV Stabiliser still valid?" → MCP tool (CheckMsdsExpiry)
- "What's Qenos's supplier rating?" → MCP tool (GetSupplierRating)
- "What stock of HDPE do we have?" → Local tool (GetStockLevel)
- "How much Nylon do I need for 10000 mask clips?" → Local tool (CalculateMaterialRequirement)

## Key Learnings

### 1. MCP tools and local tools are indistinguishable to the agent
Both appear as `AITool` objects in the `ChatOptions.Tools` list. The agent doesn't know which tools are local and which are MCP — it just sees names, descriptions, and parameters. This is the abstraction working as designed.

**See:** `Agents/L03_MCP/McpAssistant.cs:103-106` — `allTools` merges both lists

### 2. The MCP server is a standalone .NET app
It has its own `.csproj`, its own dependencies, its own data layer. It could be versioned, deployed, and maintained independently of the agent. In a real system, the purchasing team could own the supplier MCP server while the production team owns the agent.

**See:** `src/SupplierMcpServer/` — entirely self-contained project

### 3. `[McpServerTool]` mirrors `[Description]` + `AIFunctionFactory`
The pattern is nearly identical to L2's local tools. The differences are:
- `[McpServerTool]` attribute instead of relying on `AIFunctionFactory.Create`
- `[McpServerToolType]` on the class for discovery
- The hosting boilerplate (`AddMcpServer().WithStdioServerTransport().WithTools<T>()`)

**See:** `SupplierMcpServer/SupplierTools.cs:26-28` vs `Agents/L02_ToolAgent/ProductionTools.cs:28-30`

### 4. Stdio transport launches the server as a child process
`StdioClientTransport` starts the MCP server as a subprocess and communicates via stdin/stdout using the MCP JSON-RPC protocol. The agent manages the process lifecycle — it starts on connect and stops on dispose.

**See:** `Agents/L03_MCP/McpAssistant.cs:84-91` — `StdioClientTransport` configuration

### 5. Async initialisation needs a wrapper pattern
`McpClient.CreateAsync` is async (it performs the MCP handshake). Since `MainWindow`'s constructor is synchronous, we can't create the `McpAssistant` there. The `LazyMcpAgent` wrapper defers initialisation to the first message — a common pattern when connecting to external services at startup.

**See:** `Agents/L03_MCP/LazyMcpAgent.cs` — deferred init pattern

### 6. The system prompt should describe tool categories
When an agent has both local and MCP tools, the system prompt should help the LLM understand which category of tool to use for which type of question. We explicitly list "LOCAL TOOLS" vs "SUPPLIER TOOLS" in the prompt.

**See:** `Agents/L03_MCP/McpAssistant.cs:40-56` — categorised tool descriptions in the prompt

## What's next
In **Lesson 4: Middleware & Context Providers**, we'll add cross-cutting concerns that apply to all agent interactions — audit logging, role-based data filtering, and request timing — without modifying the agent code.
