# Lesson 2: Tool-Using Agent

## What you'll learn
- How to give an agent tools (function calling) so it can query real data
- How `AIFunctionFactory.Create` turns any C# method into a callable tool
- The role of `[Description]` attributes in guiding the LLM's tool selection
- How the agent autonomously decides to call tools vs respond directly

## Architecture

```
                    User: "What's the stock level of HDPE resin?"
                                    │
                                    ▼
                        ┌───────────────────────┐
                        │  ToolUsingAssistant   │
                        │  ┌─────────────────┐  │
                        │  │ AIAgent         │  │
                        │  │  Instructions + │  │
                        │  │  Tools list     │  │
                        │  └────────┬────────┘  │
                        └───────────┼───────────┘
                                    │
                    ┌───────────────┼───────────────┐
                    │           LLM decides         │
                    │    "I need GetStockLevel"     │
                    └───────────────┼───────────────┘
                                    │
                        ┌───────────▼────────────┐
                        │  MAF Tool Loop         │
                        │  1. Invoke method      │
                        │  2. Feed result to LLM │
                        │  3. LLM generates      │
                        │     final response     │
                        └───────────┬────────────┘
                                    │
                                    ▼
              "HDPE Resin: 2,450 kg in Warehouse A, Bay 1.
               Stock level is healthy."
```

## How tool calling works in MAF

### Step 1: Define tools as C# methods
Any static or instance method can become a tool. Use `[Description]` attributes to tell the LLM what the method does and what each parameter means.

```csharp
[Description("Get the current stock level for a raw material or finished good.")]
public static string GetStockLevel(
    [Description("The name of the material or product to check")] string itemName)
{
    // Look up data and return a string result
}
```

### Step 2: Register tools with `AIFunctionFactory.Create`
This uses reflection to extract the method name, description, parameters, and return type — then wraps it as an `AITool` that the framework can invoke.

```csharp
var tools = new List<AITool>
{
    AIFunctionFactory.Create(ProductionTools.GetStockLevel),
    AIFunctionFactory.Create(ProductionTools.CheckMachineStatus),
};
```

### Step 3: Pass tools via `ChatOptions.Tools`
```csharp
chatClient.AsAIAgent(new ChatClientAgentOptions
{
    ChatOptions = new()
    {
        Instructions = systemPrompt,
        Tools = tools,
    }
});
```

### Step 4: The agent decides autonomously
The LLM sees the tool descriptions alongside the user's message. It decides:
- **No tool needed:** "What is OEE?" → responds from general knowledge
- **Tool needed:** "What's the stock level of HDPE?" → calls `GetStockLevel("HDPE Resin")`
- **Multiple tools:** "How much resin for 1000 mask clips?" → calls `CalculateMaterialRequirement("VT-1042", 1000)`

The framework handles the invocation loop automatically — call method, feed result back, let LLM generate the final answer.

## Files

| File | Purpose |
|------|---------|
| `MockData/ProductionData.cs` | Machines, production runs, status, OEE |
| `MockData/InventoryData.cs` | Stock levels, material specs, bill of materials |
| `Agents/L02_ToolAgent/ProductionTools.cs` | Tool method definitions with `[Description]` attributes |
| `Agents/L02_ToolAgent/ToolUsingAssistant.cs` | Agent wired to Ollama with tools registered |
| `Shared/IChatAgent.cs` | Common interface for lesson agents |

## Running it

```bash
dotnet run --project src/AgentExplorer
```

Select the **L2: Tools** tab. Try these queries:
- "What's the stock level of HDPE resin?"
- "What machine is INJ-01 running?"
- "How much material do I need for 5000 units of VT-3005?"
- "What's the spec for Nylon PA6?"
- "What is OEE?" (should respond without calling a tool)

## Key Learnings

### 1. Tools are just C# methods — no special SDK
There's no tool framework to learn. You write a normal C# method, add `[Description]` attributes, and `AIFunctionFactory.Create` does the rest. This means your existing codebase methods can become agent tools with minimal change.

**See:** `Agents/L02_ToolAgent/ProductionTools.cs:28-39` — `GetStockLevel` is a plain method

### 2. `[Description]` is how the LLM chooses tools
The LLM never sees your C# code — it only sees the method name, description, and parameter descriptions. If your descriptions are vague, the LLM will make bad choices about when and how to call tools. Write descriptions from the LLM's perspective: what does this tool do, when should it be used, what should I pass?

**See:** `Agents/L02_ToolAgent/ProductionTools.cs:26-28` — description on method and parameter

### 3. The system prompt changes when tools are available
Compare L1's prompt ("You do NOT have access to live data yet") with L2's ("You have access to tools that can query real production data"). The prompt must match the agent's actual capabilities — otherwise the LLM will ignore tools it has, or try to use tools it doesn't.

**See:** `Agents/L02_ToolAgent/ToolUsingAssistant.cs:33-56` vs `Agents/L01_Foundation/ProductionAssistant.cs:52-78`

### 4. Tool results become conversation context
When a tool returns a string, MAF injects it into the conversation as a message. The LLM then uses that result to formulate its response to the user. The user never sees the raw tool output — they see the LLM's interpretation of it. This is why tool methods should return clear, structured text.

**See:** `Agents/L02_ToolAgent/ProductionTools.cs:88-104` — `CalculateMaterialRequirement` returns formatted text

### 5. The agent abstraction enables reuse
Both L1 and L2 agents implement `IChatAgent`, allowing the `ChatView` to work with either one without knowing the difference. This is the same abstraction pattern MAF itself uses — `AIAgent` wraps any `IChatClient`. Our `IChatAgent` mirrors this at the TUI level.

**See:** `Shared/IChatAgent.cs` — the 4-line interface, `Views/ChatView.cs:18` — accepts any `IChatAgent`

### 6. Temperature 0 matters even more with tools
With tools, the LLM is making structured decisions: which tool to call, what arguments to pass. Randomness here doesn't mean "creative phrasing" — it means "wrong tool" or "hallucinated parameter values". Temperature 0 ensures deterministic, correct tool selection.

**See:** `Agents/L02_ToolAgent/ToolUsingAssistant.cs:78` — `Temperature = 0f`

## What's next
In **Lesson 3: MCP Integration**, we'll move tools out of the agent's process into an external MCP server. This is how real production systems work — tool teams ship independently of agent teams, and agents discover tools dynamically at runtime.
