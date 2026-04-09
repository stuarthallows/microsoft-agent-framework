using Terminal.Gui;
using AgentExplorer.Shared;

namespace AgentExplorer.Views;

/// <summary>
/// Reusable chat panel that works with any IChatAgent.
/// Shows conversation history at top, text input at bottom.
/// Streams responses token-by-token and marshals UI updates to the main thread.
/// </summary>
public sealed class ChatView : View
{
    private readonly ThinkingTextView _chatHistory;
    private readonly TextField _inputField;
    private readonly FrameView _inputFrame;
    private readonly IChatAgent _agent;

    public ChatView(IChatAgent agent)
    {
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;

        _agent = agent;

        var chatFrame = new FrameView
        {
            Title = agent.DisplayName,
            Width = Dim.Fill(),
            Height = Dim.Fill()! - 3,
            BorderStyle = LineStyle.Rounded
        };

        _chatHistory = new ThinkingTextView
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };

        chatFrame.Add(_chatHistory);

        _inputFrame = new FrameView
        {
            Title = "Message",
            Y = Pos.Bottom(chatFrame),
            Width = Dim.Fill(),
            Height = 3,
            BorderStyle = LineStyle.Rounded
        };

        _inputField = new TextField
        {
            Width = Dim.Fill(),
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        _inputField.Accepting += OnInputAccepting;
        _inputFrame.Add(_inputField);

        _chatHistory.Append($"Welcome to {agent.DisplayName}.");
        _chatHistory.Append("Type a message below and press Enter to chat.\n");

        Add(chatFrame, _inputFrame);

        Initialized += (_, _) =>
        {
            _inputField.SetFocus();
            _chatHistory.EnsureTimerStarted();
        };
    }

    private void OnInputAccepting(object? sender, CommandEventArgs e)
    {
        var userMessage = _inputField.Text?.Trim();
        if (string.IsNullOrEmpty(userMessage))
            return;

        e.Cancel = true;
        _inputField.Text = "";

        _chatHistory.Append($"You: {userMessage}");
        _chatHistory.StartThinking();
        _inputFrame.Title = "Waiting for response...";

        _ = Task.Run(async () =>
        {
            try
            {
                var receivedText = false;
                await foreach (var chunk in _agent.StreamResponseAsync(userMessage))
                {
                    if (string.IsNullOrEmpty(chunk)) continue;

                    Application.Invoke(() =>
                    {
                        if (!receivedText)
                        {
                            _chatHistory.StopThinking();
                            _inputFrame.Title = "Message";
                            _chatHistory.Append("Assistant: ");
                            receivedText = true;
                        }
                        _chatHistory.AppendChunk(chunk);
                    });
                }
                Application.Invoke(() =>
                {
                    if (!receivedText)
                    {
                        _chatHistory.StopThinkingWith("Assistant: (no response)\n");
                        _inputFrame.Title = "Message";
                    }
                    else
                    {
                        _chatHistory.Append("\n");
                    }
                });
            }
            catch (Exception ex)
            {
                Application.Invoke(() =>
                {
                    _chatHistory.StopThinking();
                    _inputFrame.Title = "Message";
                    _chatHistory.Append("Assistant: ");
                    _chatHistory.Append($"[Error: {ex.Message}]");
                    _chatHistory.Append("Hint: Is Ollama running? Try: ollama serve\n");
                });
            }
        });
    }
}
