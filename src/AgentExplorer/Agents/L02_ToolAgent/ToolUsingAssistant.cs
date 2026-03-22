using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using AgentExplorer.Shared;

namespace AgentExplorer.Agents.L02_ToolAgent;

/// <summary>
/// Lesson 2: An agent with function tools that can query Vector Technologies data.
///
/// The key difference from L1 is the Tools list in ChatOptions. When the agent
/// receives a user message, the LLM sees both the conversation and the tool
/// descriptions. It autonomously decides whether to:
///   1. Respond directly from general knowledge (no tool call)
///   2. Call one or more tools, then use the results to formulate a response
///
/// The framework handles the tool invocation loop — if the LLM requests a tool
/// call, MAF executes the method, feeds the result back, and lets the LLM
/// generate the final response. This can happen multiple times in a single turn.
/// </summary>
public class ToolUsingAssistant : IChatAgent
{
    private readonly AIAgent _agent;
    private readonly AgentSession _session;

    public string DisplayName => "L2: Production Chat (With Tools)";

    // The system prompt now tells the agent it HAS tools and should use them
    // for real data rather than guessing. Compare with L1's prompt which said
    // "you do NOT have access to live data yet."
    private const string SystemPrompt = """
        You are the Production Floor Assistant for Vector Technologies, a plastics
        manufacturing company based in Adelaide with operations in Thailand.

        You have access to tools that can query real production data:
        - Stock levels for raw materials and finished goods
        - Material specifications (resin type, grade, supplier, MSDS expiry)
        - Machine status (running/idle/maintenance, current part, OEE)
        - Material requirement calculations based on the bill of materials

        Always use the appropriate tool when the user asks about specific data.
        Do not guess or invent numbers — call the tool and report what it returns.
        If a tool returns no results, tell the user and suggest checking the item name.

        For general manufacturing questions that don't need data lookup, respond
        directly from your knowledge.

        Boundaries:
        - Never authorise starting a run, releasing a quality hold, or overriding
          safety procedures. Direct the user to the appropriate supervisor.
        - For questions outside production (sales, HR, purchasing), say you're the
          production assistant and suggest they contact the relevant department.

        Style: Be direct and concise. Vary your phrasing naturally.
        """;

    public ToolUsingAssistant()
    {
        var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434";
        var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "qwen3:8b";

        // Create tools from static methods using AIFunctionFactory.
        // This uses reflection to read [Description] attributes and parameter info,
        // then exposes them to the LLM as callable functions.
        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(ProductionTools.GetStockLevel),
            AIFunctionFactory.Create(ProductionTools.LookupMaterialSpec),
            AIFunctionFactory.Create(ProductionTools.CheckMachineStatus),
            AIFunctionFactory.Create(ProductionTools.CalculateMaterialRequirement),
        };

        _agent = new OllamaApiClient(new Uri(endpoint), model)
            .AsAIAgent(new ChatClientAgentOptions
            {
                Name = "VectorToolAssistant",
                ChatOptions = new()
                {
                    Instructions = SystemPrompt,
                    Temperature = 0f,
                    Tools = tools,
                }
            });

        _session = _agent.CreateSessionAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Send a message and stream the response token-by-token.
    /// Tool calls happen automatically within the stream — the framework
    /// invokes the tool, feeds the result back, and the LLM continues.
    /// </summary>
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
}
