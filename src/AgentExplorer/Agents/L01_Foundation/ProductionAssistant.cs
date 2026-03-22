using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using AgentExplorer.Shared;

namespace AgentExplorer.Agents.L01_Foundation;

/// <summary>
/// Lesson 1: A simple production floor assistant for Vector Technologies.
/// Uses Ollama as the local LLM backend and MAF's AIAgent for conversation management.
/// </summary>
public class ProductionAssistant : IChatAgent
{
    private readonly AIAgent _agent;
    private readonly AgentSession _session;

    public string DisplayName => "L1: Production Chat (No Tools)";

    // --- System Prompt Design Notes ---
    //
    // SCOPE: We scope this to production-floor topics only (machines, runs, shifts,
    // OEE, stock levels, labels, die changes). A narrower scope helps smaller local
    // models stay focused — they won't hallucinate answers about sales margins or
    // HR policies. In Lesson 7 (Handoff), we'll add specialist agents for those
    // domains and let a triage agent route between them.
    //
    // PERSONALITY: Direct and concise — production staff need quick answers mid-shift,
    // not essays. But not robotic — a brief acknowledgement before the answer keeps
    // the interaction natural. Avoid jargon the model might misuse; use Vector's
    // actual terminology (cells, runs, OEE, carton labels) so staff recognize it.
    //
    // KNOWLEDGE BOUNDARIES: Without tools (added in Lesson 2), this agent can only
    // discuss production concepts — it cannot query live machine status or stock
    // levels. The prompt explicitly says "based on general knowledge" so it doesn't
    // fabricate specific numbers. Once we add tools, we'll update this to say
    // "use the provided tools to look up real-time data."
    //
    // SAFETY: The assistant should never authorize actions (starting runs, approving
    // quality holds, overriding safety checks). It can explain procedures and suggest
    // next steps, but decisions stay with humans. This is critical in manufacturing
    // where incorrect guidance could cause safety incidents or scrap.
    //
    // TOKEN BUDGET: Keep the prompt under ~300 tokens. Local models (qwen3:8b)
    // have limited context windows, and every token here reduces space for
    // conversation history. Be precise, not verbose.
    //
    // TEMPERATURE: Set to 0 for fully deterministic token selection — the model
    // always picks the highest-probability next token. This prevents the sampler
    // from introducing randomness that could lead to fabricated details. To avoid
    // robotic-sounding repetition, we add a style instruction telling the model
    // to vary its phrasing intentionally. This separates two concerns:
    //   - Temperature controls *random sampling* (we don't want that)
    //   - The prompt controls *intentional behaviour* (we do want natural language)
    //
    private const string SystemPrompt = """
        You are the Production Floor Assistant for Vector Technologies, a plastics
        manufacturing company based in Adelaide with operations in Thailand.

        Your role:
        - Help production staff (operators, technicians, supervisors, planners) with
          questions about production runs, machine status, shift handovers, OEE metrics,
          die changes, carton labelling, and stock levels of raw materials and finished goods.
        - Explain standard procedures: starting/closing runs, recording downtime,
          performing weight checks, and logging reject categories.
        - Reference Vector's structure: machines are grouped into cells, each cell has
          a supervisor, and production is tracked per-shift with sign-off reports.

        Boundaries:
        - You do NOT have access to live data yet. Answer based on general manufacturing
          knowledge and Vector's known processes. Say "I'd need to check the system" rather
          than inventing specific numbers.
        - Never authorise starting a run, releasing a quality hold, or overriding safety
          procedures. Direct the user to the appropriate supervisor or QA manager.
        - For questions outside production (sales orders, HR, purchasing), say you're the
          production assistant and suggest they contact the relevant department.

        Style: Be direct and concise. Production staff are busy — lead with the answer,
        then add context if needed. Use Vector's terminology where appropriate.
        Vary your phrasing naturally between responses — avoid repeating the same
        sentence structures. Keep your language fresh and conversational while
        remaining factual.
        """;

    public ProductionAssistant()
    {
        var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434";
        var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "qwen3:8b";

        _agent = new OllamaApiClient(new Uri(endpoint), model)
            .AsAIAgent(new ChatClientAgentOptions
            {
                Name = "VectorProductionAssistant",
                ChatOptions = new()
                {
                    Instructions = SystemPrompt,
                    Temperature = 0f
                }
            });

        // Create a session to maintain conversation history
        _session = _agent.CreateSessionAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Send a message and stream the response token-by-token.
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
