namespace AgentExplorer.Shared;

/// <summary>
/// Common interface for lesson agents that can stream chat responses.
/// Allows ChatView to work with any lesson's agent without knowing the specifics.
/// </summary>
public interface IChatAgent
{
    string DisplayName { get; }
    IAsyncEnumerable<string> StreamResponseAsync(string userMessage);
}
