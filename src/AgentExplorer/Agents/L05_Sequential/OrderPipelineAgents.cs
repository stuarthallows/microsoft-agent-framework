using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using AgentExplorer.Agents.L02_ToolAgent;

namespace AgentExplorer.Agents.L05_Sequential;

/// <summary>
/// Lesson 5: Factory for creating the four specialist agents that form the
/// order processing pipeline.
///
/// Each agent gets:
///   - A focused system prompt defining its role in the pipeline
///   - A subset of ProductionTools relevant to that role
///   - Temperature 0 for deterministic responses
///
/// The agents are designed to work sequentially — each sees the full
/// conversation history from prior agents, so later agents can reference
/// earlier findings without needing explicit state passing.
/// </summary>
public static class OrderPipelineAgents
{
    public static AIAgent[] Create()
    {
        var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434";
        var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "qwen3:8b";

        return
        [
            CreateAgent(endpoint, model, "OrderValidator", OrderValidatorPrompt,
            [
                AIFunctionFactory.Create(ProductionTools.CheckMachineStatus),
            ]),

            CreateAgent(endpoint, model, "CostCalculator", CostCalculatorPrompt,
            [
                AIFunctionFactory.Create(ProductionTools.CalculateMaterialRequirement),
                AIFunctionFactory.Create(ProductionTools.LookupMaterialSpec),
            ]),

            CreateAgent(endpoint, model, "ProductionPlanner", ProductionPlannerPrompt,
            [
                AIFunctionFactory.Create(ProductionTools.CheckMachineStatus),
            ]),

            CreateAgent(endpoint, model, "MaterialChecker", MaterialCheckerPrompt,
            [
                AIFunctionFactory.Create(ProductionTools.GetStockLevel),
                AIFunctionFactory.Create(ProductionTools.CalculateMaterialRequirement),
            ]),
        ];
    }

    private static AIAgent CreateAgent(string endpoint, string model, string name,
        string instructions, List<AITool> tools)
    {
        return new OllamaApiClient(new Uri(endpoint), model)
            .AsAIAgent(new ChatClientAgentOptions
            {
                Name = name,
                ChatOptions = new()
                {
                    Instructions = instructions,
                    Temperature = 0f,
                    Tools = tools,
                },
            });
    }

    // --- Agent System Prompts ---
    //
    // Each prompt is narrowly focused on one pipeline stage. The agents see
    // the full conversation history from prior stages, so they can reference
    // earlier findings. Prompts explicitly tell each agent NOT to repeat
    // work done by previous stages.

    private const string OrderValidatorPrompt = """
        You are the Order Validation Agent for Vector Technologies, a plastics
        manufacturing company.

        When you receive an order request:
        1. Extract the part number and requested quantity
        2. Check machine status for injection moulders (INJ-01 to INJ-05) or
           extruders (EXT-01 to EXT-02) to verify production capability
        3. Confirm the order is feasible

        Output a structured validation summary:
        - Part number and name
        - Requested quantity
        - Validation status: VALID or INVALID with reason
        - Machine capability notes

        If the part number is not recognised in any machine's current or recent
        production, mark the order as INVALID.

        Be concise. No pleasantries. Do not discuss costs or materials — later
        agents handle that.
        """;

    private const string CostCalculatorPrompt = """
        You are the Cost Estimation Agent for Vector Technologies, a plastics
        manufacturing company.

        The previous agent has validated the order. Using that context:
        1. Calculate material requirements using the bill of materials tool
        2. Look up material specifications for each required material
        3. Summarise material costs based on grades and suppliers

        Output a cost summary:
        - Each material with quantity needed and grade
        - Supplier for each material
        - Flag any materials that may be expensive or hard to source

        Be concise and factual. Use the tools — do not invent numbers.
        Do not repeat the validation summary — it's already in the conversation.
        """;

    private const string ProductionPlannerPrompt = """
        You are the Production Planning Agent for Vector Technologies, a plastics
        manufacturing company.

        Previous agents have validated the order and estimated costs. Now:
        1. Check the status of relevant machines (INJ-01 to INJ-05 for injection
           moulded parts, EXT-01 to EXT-02 for extruded parts)
        2. Identify available machines or machines finishing their current run
        3. Recommend a machine assignment

        Output a planning summary:
        - Recommended machine and its current status/OEE
        - Any capacity concerns
        - Schedule considerations

        Be concise. Do not repeat validation or cost information.
        """;

    private const string MaterialCheckerPrompt = """
        You are the Material Verification Agent for Vector Technologies, a plastics
        manufacturing company.

        Previous agents have validated, costed, and planned the order. Now:
        1. Calculate material requirements for the order quantity
        2. Check stock levels for each required material
        3. Compare required vs available quantities

        Output a final material check:
        - Each material: required quantity vs current stock
        - Status per material: SUFFICIENT or SHORTAGE
        - Overall order readiness: READY, PARTIAL (some shortages), or BLOCKED
        - Reorder recommendations for any low-stock materials

        Be concise and precise. Use the tools — do not invent stock levels.
        """;
}
