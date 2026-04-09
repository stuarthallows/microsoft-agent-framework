# Lesson 5: Sequential Workflow

## What you'll learn
- How MAF's workflow engine orchestrates multiple agents in a pipeline
- How `AgentWorkflowBuilder.BuildSequential` creates a sequential pipeline from an array of agents
- How `InProcessExecution.RunStreamingAsync` and the event stream drive execution
- How each agent sees the full conversation history from prior agents
- How to give each agent a focused role with a scoped subset of tools

## Architecture

```
              User: "Process order for 5000 units of VT-3005"
                                    │
                 ┌──────────────────▼───────────────────┐
                 │     Stage 1: Order Validator         │
                 │     Tools: CheckMachineStatus        │
                 │     → Validates part, checks machine │
                 └──────────────────┬───────────────────┘
                                    │ full conversation history
                 ┌──────────────────▼───────────────────┐
                 │     Stage 2: Cost Calculator         │
                 │     Tools: CalculateMaterial, Specs  │
                 │     → BoM expansion, cost estimate   │
                 └──────────────────┬───────────────────┘
                                    │ full conversation history
                 ┌──────────────────▼───────────────────┐
                 │     Stage 3: Production Planner      │
                 │     Tools: CheckMachineStatus        │
                 │     → Machine assignment, schedule   │
                 └──────────────────┬───────────────────┘
                                    │ full conversation history
                 ┌──────────────────▼───────────────────┐
                 │     Stage 4: Material Checker        │
                 │     Tools: GetStockLevel, CalcMat    │
                 │     → Stock check, shortage flags    │
                 └──────────────────┬───────────────────┘
                                    │
                          Final pipeline output
```

## How sequential orchestration works in MAF

### Three steps to run a pipeline

```csharp
// 1. Build: define the pipeline from an array of agents
var workflow = AgentWorkflowBuilder.BuildSequential(agents);

// 2. Start: create a streaming run with the initial messages
await using var run = await InProcessExecution.RunStreamingAsync(workflow, messages);
await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

// 3. Stream: process events as agents execute
await foreach (WorkflowEvent evt in run.WatchStreamAsync())
{
    if (evt is AgentResponseUpdateEvent e)
        Console.Write(e.Update.Text);          // streaming tokens
    else if (evt is WorkflowOutputEvent)
        break;                                  // pipeline complete
}
```

### The TurnToken pattern

`RunStreamingAsync` sets up the execution context but doesn't start processing. You must call `TrySendMessageAsync(new TurnToken(emitEvents: true))` to kick off the pipeline. The `emitEvents: true` flag causes `AgentResponseUpdateEvent` to be emitted for each streaming token — without it, you only get the final `WorkflowOutputEvent`.

### Full conversation history flows forward

Each agent in the pipeline receives the **entire** conversation history from all prior agents — the original user message plus every assistant response so far. This means the Cost Calculator can see the Order Validator's findings, and the Material Checker can see everything. Agents don't need explicit state passing; they read context from the conversation.

## Agent design: specialist roles with scoped tools

Each agent gets only the tools it needs:

| Agent | Tools | Why |
|-------|-------|-----|
| Order Validator | `CheckMachineStatus` | Needs to verify machines can produce the part |
| Cost Calculator | `CalculateMaterialRequirement`, `LookupMaterialSpec` | Needs BoM and material data for cost estimation |
| Production Planner | `CheckMachineStatus` | Needs machine availability for scheduling |
| Material Checker | `GetStockLevel`, `CalculateMaterialRequirement` | Needs stock levels and material quantities |

Tool scoping reinforces prompt boundaries — even if an agent's prompt doesn't mention stock levels, having the `GetStockLevel` tool available might tempt the LLM to use it. Restricting tools keeps each agent focused.

## Files

| File | Purpose |
|------|---------|
| `Agents/L05_Sequential/OrderPipelineAgents.cs` | Factory creating four specialist agents with scoped prompts and tools |
| `Agents/L05_Sequential/SequentialAssistant.cs` | `IChatAgent` implementation bridging workflow events to streaming text |
| `Views/MainWindow.cs` | Tab registration (reuses `ChatView`) |

## Running it

```bash
dotnet run --project src/AgentExplorer
```

Select the **L5: Sequential** tab. Try:
- `Process order for 5000 units of VT-3005` — HDPE conduit, should have sufficient materials
- `Process order for 50000 units of VT-1042` — large qty of Nylon PA6 mask clips, likely shortage
- `Process order for 1000 units of VT-9999` — invalid part number, caught at validation

Each query runs four agents sequentially. Expect 30-60 seconds per query with `qwen3:8b` — you'll see the thinking spinner, then output from each stage with `--- Stage N ---` headers.

## Key Learnings

### 1. `BuildSequential` creates a pipeline, not a simple loop
`AgentWorkflowBuilder.BuildSequential` wraps each agent in an executor and connects them as a directed graph. The `Workflow` object is a reusable graph definition — execution state lives in the `StreamingRun`, so the same workflow handles multiple orders.

**See:** `Agents/L05_Sequential/SequentialAssistant.cs:48` — workflow construction in the constructor

### 2. The conversation history IS the state
MAF's sequential orchestration passes the full conversation to each agent. There's no separate state object to manage — the Order Validator's response becomes context for the Cost Calculator, which becomes context for the Production Planner. Each agent reads what it needs from the conversation and adds its own findings.

**See:** `Agents/L05_Sequential/OrderPipelineAgents.cs:99` — Cost Calculator prompt: "The previous agent has validated the order. Using that context..."

### 3. ExecutorId identifies which agent is speaking
In the event stream, `AgentResponseUpdateEvent.ExecutorId` matches the agent's `Name` from `ChatClientAgentOptions`. This is how the UI shows pipeline progression — detecting when `ExecutorId` changes means a new stage has started.

**See:** `Agents/L05_Sequential/SequentialAssistant.cs:60` — detecting agent transitions in the event stream

### 4. Tool scoping enforces separation of concerns
Each agent gets only the tools relevant to its stage. This prevents tool confusion (the Order Validator doesn't need stock levels) and guides the LLM toward its intended role. All four agents share the same `ProductionTools` class from L2, but each gets a different subset.

**See:** `Agents/L05_Sequential/OrderPipelineAgents.cs:34` — tool lists per agent

### 5. The IChatAgent abstraction scales to multi-agent
The same `ChatView` from L1 works unchanged for a four-agent pipeline. `SequentialAssistant` bridges the workflow event stream to `IAsyncEnumerable<string>`, formatting agent transitions as visual headers. The UI layer doesn't know or care that multiple agents are involved.

**See:** `Agents/L05_Sequential/SequentialAssistant.cs:53` — `StreamResponseAsync` bridging workflow events to text chunks

### 6. Each agent gets its own OllamaApiClient
The factory creates a separate `OllamaApiClient` per agent. This is required because each agent maintains independent internal state (system prompt, tools, conversation tracking). They all point to the same Ollama endpoint and model, but their client instances are distinct.

**See:** `Agents/L05_Sequential/OrderPipelineAgents.cs:60` — `CreateAgent` factory method

## Official documentation

- [Sequential Orchestration](https://learn.microsoft.com/agent-framework/workflows/orchestrations/sequential) — `BuildSequential`, `InProcessExecution`, event streaming, and human-in-the-loop approval
- [Workflow Orchestrations Overview](https://learn.microsoft.com/agent-framework/workflows/orchestrations/) — all built-in patterns: sequential, concurrent, handoff, group chat, magentic
- [Microsoft Agent Framework Workflows](https://learn.microsoft.com/agent-framework/workflows/) — workflows vs agents, key features, graph-based architecture

## What's next
In **Lesson 6: Concurrent Workflows**, we move from sequential pipelines to fan-out/fan-in — checking inventory, machines, and suppliers in parallel, then aggregating the results.
