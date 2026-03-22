# Lesson 1: Single Agent + TUI Shell

## What you'll learn
- How MAF's `AIAgent` wraps any `IChatClient` (the provider abstraction pattern)
- Streaming responses with `RunStreamingAsync` and `IAsyncEnumerable`
- Conversation state with `AgentSession`
- Building a Terminal.Gui v2 application with tabs and text views

## Architecture

```
┌─────────────────────────────────────────────────┐
│  Terminal.Gui TUI                               │
│  ┌────────────────────────────────────────────┐ │
│  │ MainWindow (Runnable)                      │ │
│  │  ┌─────────┬──────────┬─────────┐          │ │
│  │  │ L1:Chat │ L2:Tools │ L3:MCP  │ TabView  │ │
│  │  └─────────┴──────────┴─────────┘          │ │
│  │  ┌───────────────────────────────────────┐ │ │
│  │  │ ChatView                              │ │ │
│  │  │  ┌───────────────────────────────────┐│ │ │
│  │  │  │ TextView (chat history, readonly) ││ │ │
│  │  │  └───────────────────────────────────┘│ │ │
│  │  │  > [TextField (user input)]           │ │ │
│  │  └───────────────────────────────────────┘ │ │
│  └────────────────────────────────────────────┘ │
└────────────────────┬────────────────────────────┘
                     │ user message
                     ▼
          ┌─────────────────────┐
          │ ProductionAssistant │
          │  ┌────────────────┐ │
          │  │ AIAgent        │ │  ← MAF wraps the chat client
          │  │  ┌───────────┐ │ │
          │  │  │IChatClient│ │ │  ← OllamaApiClient implements this
          │  │  └───────────┘ │ │
          │  └────────────────┘ │
          │  ┌────────────────┐ │
          │  │ AgentSession   │ │  ← maintains conversation history
          │  └────────────────┘ │
          └──────────┬──────────┘
                     │ HTTP (localhost:11434)
                     ▼
              ┌─────────────┐
              │   Ollama    │
              │  llama3.2   │
              └─────────────┘
```

## Key concepts

### The provider abstraction (`IChatClient` → `AIAgent`)
MAF doesn't own the LLM connection. The `AsAIAgent()` extension method (from `Microsoft.Extensions.AI`) wraps any `IChatClient` into an `AIAgent`. This means swapping Ollama for Azure OpenAI is a one-line constructor change — the rest of your code stays identical.

```csharp
// Ollama (local)
AIAgent agent = new OllamaApiClient(new Uri(endpoint), model)
    .AsAIAgent(instructions: prompt, name: "MyAgent");

// Azure OpenAI (cloud) — same .AsAIAgent() call
AIAgent agent = new AzureOpenAIClient(new Uri(endpoint), credential)
    .GetChatClient(deployment)
    .AsAIAgent(instructions: prompt, name: "MyAgent");
```

### Streaming with `IAsyncEnumerable`
`RunStreamingAsync` returns `IAsyncEnumerable<AgentResponseUpdate>`, yielding tokens as they arrive from the model. Each `update.Text` contains a small chunk of text. This is what makes the TUI feel responsive — you see the answer forming in real-time rather than waiting for the full response.

### Conversation state with `AgentSession`
`AgentSession` tracks the conversation history. When you pass a session to `RunStreamingAsync`, the agent remembers previous turns. Without it, every message would be treated as a fresh conversation.

## Files

| File | Purpose |
|------|---------|
| `Program.cs` | Entry point — creates Terminal.Gui app, runs MainWindow |
| `Views/MainWindow.cs` | Tab layout with lesson selector |
| `Views/ChatView.cs` | Chat panel — sends messages, streams responses |
| `Agents/L01_Foundation/ProductionAssistant.cs` | MAF agent wrapper with system prompt |

## Running it

### Prerequisites
1. Install Ollama: `brew install ollama`
2. Pull a model: `ollama pull llama3.2`
3. Start the server: `ollama serve`

### Run
```bash
cd src/AgentExplorer
dotnet run
```

### Environment variables (optional)
```bash
export OLLAMA_ENDPOINT="http://localhost:11434"  # default
export OLLAMA_MODEL="llama3.2"                    # default
```

## System prompt design

The system prompt in `ProductionAssistant.cs` has detailed comments explaining every design decision. Key principles:
- **Scope it narrowly** — production topics only, for better focus with smaller models
- **State knowledge boundaries** — the agent can't query live data yet (tools come in L2)
- **Never authorise actions** — manufacturing safety requires human decision-makers
- **Keep it under ~300 tokens** — preserve context window for conversation history

## Key Learnings

### 1. MAF is a wrapper, not a runtime
MAF's `AIAgent` doesn't run inference itself — it wraps whatever `IChatClient` you provide. This is the **provider abstraction pattern**: your agent code is decoupled from the LLM. The practical benefit is that you can develop locally against Ollama and deploy against Azure OpenAI with a single constructor change, no agent logic rewritten.

**See:** `Agents/L01_Foundation/ProductionAssistant.cs:26-28` — the `AsAIAgent()` call

### 2. `AsAIAgent()` is an extension method, not a constructor
The method lives in the `Microsoft.Extensions.AI` namespace (not `Microsoft.Agents.AI`). This is easy to miss — you need `using Microsoft.Extensions.AI;` or the `OllamaApiClient` won't have the method available. MAF deliberately extends the M.E.AI ecosystem rather than replacing it.

**See:** `Agents/L01_Foundation/ProductionAssistant.cs:1-3` — the using directives

### 3. Sessions give agents memory
Without an `AgentSession`, each call to `RunAsync`/`RunStreamingAsync` is stateless — the agent forgets everything. Creating a session with `agent.CreateSessionAsync()` and passing it to subsequent calls is what makes multi-turn conversation work. In later lessons, sessions become even more important — they're how orchestrated workflows track state across multiple agents.

**See:** `Agents/L01_Foundation/ProductionAssistant.cs:31` — session creation, line `39` — passing it to streaming

### 4. Streaming is `IAsyncEnumerable`, not callbacks
MAF uses C#'s native `IAsyncEnumerable<AgentResponseUpdate>` for streaming — no callback registrations, no event handlers. You just `await foreach`. This composes naturally with other async C# patterns and makes it easy to wire into UI updates (as we do in the TUI).

**See:** `Agents/L01_Foundation/ProductionAssistant.cs:37-46` — the streaming method, `Views/ChatView.cs:79-89` — consuming the stream in the UI

### 5. System prompt design matters more with local models
The comments in `ProductionAssistant.cs` above the system prompt explain five design principles for manufacturing contexts: narrow scope, explicit knowledge boundaries, never authorise safety-critical actions, use domain terminology, and keep token budget tight. These aren't just good practice — with a local 8B-parameter model, a well-scoped prompt is the difference between useful and useless.

**See:** `Agents/L01_Foundation/ProductionAssistant.cs:16-47` — annotated system prompt with design rationale

### 6. Terminal.Gui v2 uses `Runnable`, not `Toplevel`
Terminal.Gui v2 is a significant API change from v1. The entry point is `Application.Create().Init()` → `app.Run<MyRunnable>()`. Windows extend `Runnable` (or `Runnable<T>` for return values). Tab creation uses property initialisers (`new Tab { DisplayText = "...", View = ... }`), not constructor parameters.

**See:** `Program.cs` — entry point pattern, `Views/MainWindow.cs:11` — `Runnable` base class

## What's next
In **Lesson 2: Tool-Using Agent**, we'll give this assistant real capabilities — tools to query stock levels, look up material specs, and check machine status. The agent will decide *when* to call each tool, and you'll see those decisions in a new "Agent Inspector" panel.
