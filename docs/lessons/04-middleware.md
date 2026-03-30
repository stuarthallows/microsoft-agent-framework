# Lesson 4: Middleware & Context Providers

## What you'll learn
- How MAF's pipeline architecture separates cross-cutting concerns from agent logic
- Three types of middleware: agent run, function calling, and context providers
- The builder pattern for wrapping agents without modifying them
- Role-based context injection for adapting agent behavior per user

## Architecture

```
                    User message
                        │
             ┌──────────▼───────────┐
             │ Agent Run Middleware │  ← Audit: logs input/output
             │  (AuditMiddleware)   │
             └──────────┬───────────┘
                        │
             ┌──────────▼───────────┐
             │  Context Provider    │  ← Injects role-specific instructions
             │ (RoleContextProvider)│    (Operator/Supervisor/Manager)
             └──────────┬───────────┘
                        │
             ┌──────────▼──────────┐
             │    Base Agent       │  ← Same as L2 with tools
             │  (ChatClientAgent)  │
             └──────────┬──────────┘
                        │
          ┌─────────────┼─────────────┐
          │     LLM decides to        │
          │     call a tool           │
          └─────────────┼─────────────┘
                        │
             ┌──────────▼───────────┐
             │ Function Middleware  │  ← Logs tool name, args, result
             │ (ToolCallMiddleware) │
             └──────────┬───────────┘
                        │
                   Tool executes
```

## The three middleware layers

### 1. Agent Run Middleware
Wraps the entire agent execution — sees all input messages and the final response. Use for audit trails, timing, and compliance logging.

```csharp
var agent = baseAgent.AsBuilder()
    .Use(runFunc: AuditRunMiddleware, runStreamingFunc: AuditStreamingMiddleware)
    .Build();
```

### 2. Function Calling Middleware
Intercepts each tool invocation — sees the function name, arguments, and result. Use for tool-level audit trails, approval gates, or cost tracking.

```csharp
var agent = baseAgent.AsBuilder()
    .Use(ToolCallMiddleware)
    .Build();
```

### 3. Context Providers (AIContextProvider)
Inject additional instructions, messages, or tools before each LLM call. Registered via `ChatClientAgentOptions.AIContextProviders`. Use for role-based behavior, RAG context, or memory injection.

```csharp
var agent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    AIContextProviders = [new RoleContextProvider(auditLog)],
});
```

## Files

| File | Purpose |
|------|---------|
| `Agents/L04_Middleware/AuditLog.cs` | Thread-safe in-memory audit log with event subscription |
| `Agents/L04_Middleware/RoleContextProvider.cs` | Context provider injecting role-specific instructions |
| `Agents/L04_Middleware/MiddlewareAssistant.cs` | Agent wired with all three middleware layers |
| `Views/MiddlewareChatView.cs` | Chat view with role selector and inline audit display |
| `Views/ThinkingTextView.cs` | Extracted spinner animation shared by ChatView and MiddlewareChatView |

## Running it

```bash
dotnet run --project src/AgentExplorer
```

Select the **L4: Middleware** tab. Try:
- Ask "What's the status of INJ-01?" as Operator → machine-level detail
- Use the **role dropdown** to switch to Manager → ask the same question → broader context
- Watch the `[>> ...]` and `[<< ...]` lines showing tool calls inline
- Watch `[** Role context injected: ...]` showing the context provider firing

## Key Learnings

### 1. `.AsBuilder().Use().Build()` creates a NEW wrapped agent
The original base agent is not modified. Middleware is additive — you compose layers around the agent. This means multiple callers can wrap the same base agent with different middleware for different purposes.

**See:** `Agents/L04_Middleware/MiddlewareAssistant.cs` — the `.AsBuilder().Use().Build()` chain

### 2. Agent run middleware sees the full conversation
The `runFunc` receives all input messages and returns the full response. The streaming variant (`runStreamingFunc`) yields chunks. Both call `innerAgent.RunAsync`/`RunStreamingAsync` to pass through — if they don't, the agent never runs.

**See:** `Agents/L04_Middleware/MiddlewareAssistant.cs` — `AuditRunMiddleware` and `AuditStreamingMiddleware` methods

### 3. Function calling middleware intercepts individual tool calls
Each tool invocation passes through the function middleware with a `FunctionInvocationContext` containing the function name and arguments. Call `next(context, ct)` to actually execute the tool. This is where you'd implement approval gates ("do you want to allow this tool call?").

**See:** `Agents/L04_Middleware/MiddlewareAssistant.cs` — `ToolCallMiddleware` method

### 4. Context providers inject instructions, not intercept calls
Unlike middleware which wraps execution, context providers ADD context before the LLM sees the messages. They return an `AIContext` with additional `Instructions`, `Messages`, or `Tools`. The LLM sees these alongside the system prompt and conversation history.

**See:** `Agents/L04_Middleware/RoleContextProvider.cs` — `ProvideAIContextAsync` override

### 5. Context providers must not store session-specific state
A single `AIContextProvider` instance is shared across all sessions. Session-specific data (like which role the user has) must be stored elsewhere — in this demo we use a property on the provider, but in production you'd use `AgentSession` metadata or an external service.

**See:** `Agents/L04_Middleware/RoleContextProvider.cs` — `CurrentRole` property

### 6. The AuditLog event pattern connects middleware to UI
The `AuditLog.OnEntry` event bridges the gap between middleware (which runs during agent execution) and the TUI (which needs to display events). The middleware writes to the log, the log fires an event, and the UI subscribes to show tool calls inline. This decouples the middleware from the presentation layer.

**See:** `Agents/L04_Middleware/AuditLog.cs` — `OnEntry` event, `Views/MiddlewareChatView.cs` — audit subscriber and `FlushAuditEvents` method

## Official documentation

- [Agent Pipeline Architecture](https://learn.microsoft.com/agent-framework/agents/agent-pipeline) — the three-layer pipeline (agent middleware, context layer, chat client layer) and execution flow
- [Defining Middleware](https://learn.microsoft.com/agent-framework/agents/middleware/defining-middleware) — step-by-step guide to creating agent run, function calling, and chat client middleware
- [Agent Middleware Overview](https://learn.microsoft.com/agent-framework/agents/middleware/) — middleware types, chaining, and the builder pattern
- [Context Providers](https://learn.microsoft.com/agent-framework/agents/conversations/context-providers) — `AIContextProvider` base class, `ProvideAIContextAsync`, `StoreAIContextAsync`, and session state patterns

## What's next
In **Lesson 5: Sequential Workflow**, we move from single agents to multi-agent orchestration — a chain of specialist agents processing a sales order through validation, costing, planning, and purchasing.
