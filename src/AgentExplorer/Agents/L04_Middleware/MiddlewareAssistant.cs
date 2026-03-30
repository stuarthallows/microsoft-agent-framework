using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using AgentExplorer.Agents.L02_ToolAgent;
using AgentExplorer.Shared;

namespace AgentExplorer.Agents.L04_Middleware;

/// <summary>
/// Lesson 4: An agent wrapped with middleware layers demonstrating
/// cross-cutting concerns without modifying the base agent code.
///
/// The pipeline:
///   1. Agent Run middleware (audit logging) — sees all messages in/out
///   2. Function Calling middleware (tool tracing) — sees each tool invocation
///   3. Context Provider (role injection) — injects role-specific instructions
///   4. Base agent with L2's production tools
///
/// The key pattern: baseAgent.AsBuilder().Use(...).Build() creates a NEW
/// wrapped agent. The original baseAgent is unchanged — middleware is
/// additive, not invasive.
/// </summary>
public class MiddlewareAssistant : IChatAgent
{
    private readonly AIAgent _agent;
    private readonly AgentSession _session;
    private readonly RoleContextProvider _roleProvider;

    public AuditLog AuditLog { get; }
    public string DisplayName => $"L4: Middleware ({_roleProvider.CurrentRole})";

    public string CurrentRole
    {
        get => _roleProvider.CurrentRole;
        set => _roleProvider.CurrentRole = value;
    }

    private const string SystemPrompt = """
        You are the Production Floor Assistant for Vector Technologies, a plastics
        manufacturing company based in Adelaide with operations in Thailand.

        You have access to tools for querying production data: stock levels,
        material specs, machine status, and material requirements.

        Always use tools for data queries — do not invent numbers.

        Your response style and detail level should be guided by the role context
        injected into this conversation. Adjust accordingly.

        Boundaries:
        - Never authorise starting a run, releasing a quality hold, or overriding
          safety procedures.
        - For questions outside production, suggest the relevant department.

        Style: Be direct and concise. Vary your phrasing naturally.
        """;

    public MiddlewareAssistant()
    {
        var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434";
        var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "qwen3:8b";

        AuditLog = new AuditLog();
        _roleProvider = new RoleContextProvider(AuditLog);

        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(ProductionTools.GetStockLevel),
            AIFunctionFactory.Create(ProductionTools.LookupMaterialSpec),
            AIFunctionFactory.Create(ProductionTools.CheckMachineStatus),
            AIFunctionFactory.Create(ProductionTools.CalculateMaterialRequirement),
        };

        // 1. Create the base agent with the context provider in options.
        //    AIContextProviders are registered at the agent level — they run
        //    before each LLM call to inject additional context.
        var baseAgent = new OllamaApiClient(new Uri(endpoint), model)
            .AsAIAgent(new ChatClientAgentOptions
            {
                Name = "VectorMiddlewareAssistant",
                ChatOptions = new()
                {
                    Instructions = SystemPrompt,
                    Temperature = 0f,
                    Tools = tools,
                },
                AIContextProviders = [_roleProvider],
            });

        // 2. Wrap with middleware using the builder pattern.
        //    Each .Use() adds a layer. The original baseAgent is NOT modified —
        //    .Build() returns a new agent that wraps it.
        //
        //    Note: Context providers go in ChatClientAgentOptions (above),
        //    while agent-run and function-calling middleware use the builder.
        var auditLog = AuditLog;
        _agent = baseAgent
            .AsBuilder()
            .Use(
                runFunc: (messages, session, options, innerAgent, ct) =>
                    AuditRunMiddleware(auditLog, messages, session, options, innerAgent, ct),
                runStreamingFunc: (messages, session, options, innerAgent, ct) =>
                    AuditStreamingMiddleware(auditLog, messages, session, options, innerAgent, ct))
            .Use((agent, context, next, ct) =>
                ToolCallMiddleware(auditLog, agent, context, next, ct))
            .Build();

        _session = _agent.CreateSessionAsync().GetAwaiter().GetResult();
    }

    // --- Agent Run Middleware ---
    // Wraps the entire agent execution. Sees all input messages and the
    // final response. Good for audit trails and compliance logging.
    private static async Task<AgentResponse> AuditRunMiddleware(
        AuditLog log,
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        AIAgent innerAgent,
        CancellationToken ct)
    {
        var messageList = messages.ToList();
        var lastUserMessage = messageList.LastOrDefault(m => m.Role == ChatRole.User);
        log.Log("AgentRun", $"Input: {lastUserMessage?.Text?[..Math.Min(lastUserMessage.Text.Length, 100)] ?? "(no text)"}");

        var response = await innerAgent.RunAsync(messageList, session, options, ct);

        log.Log("AgentRun", $"Output: {response.Text?[..Math.Min(response.Text?.Length ?? 0, 100)] ?? "(no text)"}");
        return response;
    }

    // Streaming variant — same pattern but yields chunks
    private static async IAsyncEnumerable<AgentResponseUpdate> AuditStreamingMiddleware(
        AuditLog log,
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        AIAgent innerAgent,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var messageList = messages.ToList();
        var lastUserMessage = messageList.LastOrDefault(m => m.Role == ChatRole.User);
        log.Log("AgentRun", $"Input: {lastUserMessage?.Text?[..Math.Min(lastUserMessage.Text.Length, 100)] ?? "(no text)"}");

        await foreach (var update in innerAgent.RunStreamingAsync(messageList, session, options, ct))
        {
            yield return update;
        }
    }

    // --- Function Calling Middleware ---
    // Intercepts each tool invocation. Sees the function name, arguments,
    // and result. This is how you implement tool-level audit trails,
    // approval gates, or cost tracking for expensive tool calls.
    private static async ValueTask<object?> ToolCallMiddleware(
        AuditLog log,
        AIAgent agent,
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
        CancellationToken ct)
    {
        log.Log("ToolCall", $"Calling: {context.Function.Name}({FormatArgs(context)})");

        var result = await next(context, ct);

        var resultPreview = result?.ToString()?[..Math.Min(result.ToString()!.Length, 120)] ?? "(null)";
        log.Log("ToolResult", $"{context.Function.Name} → {resultPreview}");

        return result;
    }

    private static string FormatArgs(FunctionInvocationContext context)
    {
        if (context.Arguments is null || context.Arguments.Count == 0)
            return "";
        return string.Join(", ", context.Arguments.Select(kv => $"{kv.Key}={kv.Value}"));
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
}
