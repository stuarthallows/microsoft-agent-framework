using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Terminal.Gui.Input;
using AgentExplorer.Shared;

namespace AgentExplorer.Views;

/// <summary>
/// Reusable chat panel that works with any IChatAgent.
/// Shows conversation history at top, text input at bottom.
/// Streams responses token-by-token and marshals UI updates to the main thread.
/// </summary>
public sealed class ChatView : View
{
    private readonly TextView _chatHistory;
    private readonly TextField _inputField;
    private readonly IChatAgent _agent;

    public ChatView(IChatAgent agent)
    {
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;

        _agent = agent;

        // Chat history in a bordered frame
        var chatFrame = new FrameView
        {
            Title = agent.DisplayName,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 3,
            BorderStyle = LineStyle.Rounded
        };

        _chatHistory = new TextView
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true,
            CanFocus = false
        };

        chatFrame.Add(_chatHistory);

        // Input area in a bordered frame
        var inputFrame = new FrameView
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
        inputFrame.Add(_inputField);

        AppendToHistory($"Welcome to {agent.DisplayName}.");
        AppendToHistory("Type a message below and press Enter to chat.\n");

        Add(chatFrame, inputFrame);

        Initialized += (_, _) => _inputField.SetFocus();
    }

    private void OnInputAccepting(object? sender, CommandEventArgs e)
    {
        var userMessage = _inputField.Text?.Trim();
        if (string.IsNullOrEmpty(userMessage))
            return;

        e.Handled = true;
        _inputField.Text = "";

        AppendToHistory($"You: {userMessage}");
        AppendToHistory("Assistant: ");

        var app = App!;
        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var chunk in _agent.StreamResponseAsync(userMessage))
                {
                    app.Invoke(() => AppendChunk(chunk));
                }
                app.Invoke(() => AppendToHistory("\n"));
            }
            catch (Exception ex)
            {
                app.Invoke(() =>
                {
                    AppendToHistory($"\n[Error: {ex.Message}]");
                    AppendToHistory("Hint: Is Ollama running? Try: ollama serve\n");
                });
            }
        });
    }

    private void AppendToHistory(string text)
    {
        _chatHistory.Text += text + "\n";
        ScrollToEnd();
    }

    private void AppendChunk(string chunk)
    {
        _chatHistory.Text += chunk;
        ScrollToEnd();
    }

    private void ScrollToEnd()
    {
        _chatHistory.MoveEnd();
    }
}
