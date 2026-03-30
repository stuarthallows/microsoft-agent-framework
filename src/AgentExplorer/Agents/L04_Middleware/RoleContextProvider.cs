using Microsoft.Agents.AI;

namespace AgentExplorer.Agents.L04_Middleware;

/// <summary>
/// Lesson 4: A context provider that injects role-specific instructions
/// into the conversation before each LLM call.
///
/// Context providers run BEFORE the LLM sees the messages. They can add
/// instructions, messages, or tools dynamically based on session state.
/// This one reads the user's role and adds tailored guidance so the agent
/// adjusts its detail level and authority boundaries.
///
/// From the Vector Technologies Admin user stories: the system has
/// role-based permissions — operators see machine-level data, supervisors
/// see cell summaries, and managers see site-wide dashboards.
///
/// IMPORTANT: AIContextProvider instances are shared across all sessions.
/// Never store session-specific state in fields — use AgentSession instead.
/// </summary>
public class RoleContextProvider(AuditLog auditLog) : AIContextProvider
{
    // The current role — in a real system this would come from auth/session.
    // For the TUI demo, we expose it as a mutable property that the UI can change.
    public string CurrentRole { get; set; } = "Operator";

    private static readonly Dictionary<string, string> RoleInstructions = new()
    {
        ["Operator"] = """
            The current user is a Production OPERATOR. Adjust your responses:
            - Focus on machine-level details: current part, cycle time, reject counts
            - Explain procedures step-by-step (starting runs, recording downtime, weight checks)
            - Use simple, direct language — operators are on the floor mid-shift
            - For anything requiring supervisor approval, say "please check with your supervisor"
            """,

        ["Supervisor"] = """
            The current user is a Production SUPERVISOR. Adjust your responses:
            - Include cell-level summaries alongside machine details
            - Reference shift reports, sign-off status, and team performance
            - Provide escalation guidance when issues are identified
            - You can discuss quality holds and maintenance scheduling
            - For site-wide decisions, direct them to the production manager
            """,

        ["Manager"] = """
            The current user is a Production MANAGER. Adjust your responses:
            - Include site-wide metrics, trends, and cross-cell comparisons
            - Provide decision support: capacity analysis, OEE trends, cost implications
            - Reference supplier performance and material availability
            - Discuss strategic concerns: machine utilisation, staffing, capital planning
            - You can discuss all operational areas including quality and purchasing
            """
    };

    protected override ValueTask<AIContext> ProvideAIContextAsync(
        InvokingContext context,
        CancellationToken cancellationToken = default)
    {
        var role = CurrentRole;
        var instructions = RoleInstructions.GetValueOrDefault(role, RoleInstructions["Operator"]);

        auditLog.Log("ContextInjection", $"Role context injected: {role}");

        return ValueTask.FromResult(new AIContext
        {
            // Can also add Messages or Tools.
            Instructions = instructions
        });
    }
}
