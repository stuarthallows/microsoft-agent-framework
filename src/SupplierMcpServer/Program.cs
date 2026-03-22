using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SupplierMcpServer;

// --- MCP Server Entry Point ---
//
// This is a standalone .NET app that runs as an MCP server using stdio transport.
// The agent (in AgentExplorer) launches this process and communicates via stdin/stdout.
//
// The hosting pattern:
//   1. AddMcpServer() registers the MCP protocol handler
//   2. WithStdioServerTransport() tells it to communicate via stdin/stdout (not HTTP)
//   3. WithTools<SupplierTools>() discovers all [McpServerTool] methods in the class
//
// When the agent calls McpClientFactory.CreateAsync with a StdioClientTransport
// pointing to this project, MCP handles the handshake, tool discovery, and
// invocation protocol automatically.

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<SupplierTools>();

await builder.Build().RunAsync();
