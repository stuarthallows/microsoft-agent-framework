using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using AgentExplorer.Shared;

namespace AgentExplorer.Agents.L05_Sequential;

/// <summary>
/// Lesson 5: A sequential workflow that chains four specialist agents into
/// an order processing pipeline.
///
/// This is the first multi-agent pattern in the course. The key concepts:
///
///   1. AgentWorkflowBuilder.BuildSequential creates a pipeline from an
///      array of agents. Each agent runs in order, seeing the full
///      conversation history from all prior agents.
///
///   2. InProcessExecution.RunStreamingAsync executes the workflow and
///      returns a StreamingRun for real-time event streaming.
///
///   3. The TurnToken kicks off execution — RunStreamingAsync sets up the
///      run but doesn't start processing until TrySendMessageAsync is called.
///
///   4. AgentResponseUpdateEvent identifies which agent is speaking via
///      ExecutorId, allowing the UI to show pipeline progression.
///
/// The pipeline: Order Validation → Cost Estimation → Production Planning
///             → Material Verification
///
/// Each user message triggers a fresh pipeline run — orders are independent,
/// so no session state is maintained between turns.
/// </summary>
public class SequentialAssistant : IChatAgent
{
    private readonly Workflow _workflow;

    public string DisplayName => "L5: Sequential Order Pipeline";

    public SequentialAssistant()
    {
        var agents = OrderPipelineAgents.Create();
        _workflow = AgentWorkflowBuilder.BuildSequential(agents);
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(string userMessage)
    {
        var messages = new List<ChatMessage> { new(ChatRole.User, userMessage) };

        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(_workflow, messages);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        string? lastExecutorId = null;

        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            if (evt is AgentResponseUpdateEvent e)
            {
                // Emit a visual header when the pipeline moves to the next agent
                var agentId = e.ExecutorId ?? "unknown";
                if (agentId != lastExecutorId)
                {
                    lastExecutorId = agentId;
                    yield return $"\n\n--- {FormatAgentName(agentId)} ---\n";
                }

                if (e.Update?.Text is not null)
                {
                    yield return e.Update.Text;
                }
            }
            else if (evt is WorkflowOutputEvent)
            {
                break;
            }
        }
    }

    private static string FormatAgentName(string executorId)
    {
        // ExecutorId includes a GUID suffix (e.g. "OrderValidator_79846478bc...")
        if (executorId.StartsWith("OrderValidator")) return "Stage 1: Order Validation";
        if (executorId.StartsWith("CostCalculator")) return "Stage 2: Cost Estimation";
        if (executorId.StartsWith("ProductionPlanner")) return "Stage 3: Production Planning";
        if (executorId.StartsWith("MaterialChecker")) return "Stage 4: Material Verification";
        return executorId;
    }
}
