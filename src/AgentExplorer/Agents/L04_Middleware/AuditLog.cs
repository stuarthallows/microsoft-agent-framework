using System.Collections.Concurrent;

namespace AgentExplorer.Agents.L04_Middleware;

/// <summary>
/// Thread-safe in-memory audit log that captures agent events.
/// The TUI subscribes to the OnEntry event to display tool calls and
/// middleware activity inline in the chat.
///
/// In a production ERP system (like Vector's QA compliance requirements),
/// this would write to a persistent store — database, event stream, or
/// structured log service. The in-memory version demonstrates the pattern.
/// </summary>
public class AuditLog
{
    public record AuditEntry(
        DateTime Timestamp,
        string EventType,   // AgentRun, ToolCall, ToolResult, ContextInjection
        string Detail);

    private readonly ConcurrentQueue<AuditEntry> _entries = new();

    /// <summary>
    /// Fired when a new entry is added. The ChatView subscribes to this
    /// to show tool calls and middleware events inline in the chat.
    /// </summary>
    public event Action<AuditEntry>? OnEntry;

    public void Log(string eventType, string detail)
    {
        var entry = new AuditEntry(DateTime.Now, eventType, detail);
        _entries.Enqueue(entry);
        OnEntry?.Invoke(entry);
    }

    public IReadOnlyList<AuditEntry> GetEntries() => _entries.ToArray();
}
