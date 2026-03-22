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
    private readonly FrameView _inputFrame;
    private readonly IChatAgent _agent;
    private bool _isThinking;

    private static readonly string[] SpinnerFrames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];
    private int _spinnerFrame;

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

        AppendToHistory($"Welcome to {agent.DisplayName}.");
        AppendToHistory("Type a message below and press Enter to chat.\n");

        Add(chatFrame, _inputFrame);

        Initialized += (_, _) =>
        {
            _inputField.SetFocus();

            // Start the thinking animation timer — runs on the UI thread,
            // only updates the display when _isThinking is true.
            App!.AddTimeout(TimeSpan.FromMilliseconds(80), () =>
            {
                if (_isThinking)
                {
                    _spinnerFrame = (_spinnerFrame + 1) % SpinnerFrames.Length;
                    UpdateThinkingLine();
                }
                return true; // keep the timer alive
            });
        };
    }

    private void OnInputAccepting(object? sender, CommandEventArgs e)
    {
        var userMessage = _inputField.Text?.Trim();
        if (string.IsNullOrEmpty(userMessage))
            return;

        e.Handled = true;
        _inputField.Text = "";

        AppendToHistory($"You: {userMessage}");
        AppendToHistory($"Assistant: {SpinnerFrames[0]}");
        _isThinking = true;
        _inputFrame.Title = "Waiting for response...";

        var app = App!;
        _ = Task.Run(async () =>
        {
            try
            {
                // Keep the thinking animation running until we have actual
                // text to display. During tool calling, the stream pauses
                // while MAF invokes tools and feeds results back to the LLM —
                // only then does the final response stream begin.
                var receivedText = false;
                await foreach (var chunk in _agent.StreamResponseAsync(userMessage))
                {
                    if (string.IsNullOrEmpty(chunk)) continue;

                    app.Invoke(() =>
                    {
                        if (!receivedText)
                        {
                            _isThinking = false;
                            _inputFrame.Title = "Message";
                            ReplaceThinkingLine("Assistant: ");
                            receivedText = true;
                        }
                        AppendChunk(chunk);
                    });
                }
                app.Invoke(() =>
                {
                    if (!receivedText)
                    {
                        // Stream ended without any text (edge case)
                        _isThinking = false;
                        _inputFrame.Title = "Message";
                        ReplaceThinkingLine("Assistant: (no response)\n");
                    }
                    else
                    {
                        AppendToHistory("\n");
                    }
                });
            }
            catch (Exception ex)
            {
                app.Invoke(() =>
                {
                    _isThinking = false;
                    _inputFrame.Title = "Message";
                    ReplaceThinkingLine("Assistant: ");
                    AppendToHistory($"[Error: {ex.Message}]");
                    AppendToHistory("Hint: Is Ollama running? Try: ollama serve\n");
                });
            }
        });
    }

    private void UpdateThinkingLine()
    {
        var text = _chatHistory.Text ?? "";
        // The thinking line looks like "Assistant: ⠋\n" — find and replace the spinner char
        var prefix = "Assistant: ";
        var lineStart = text.LastIndexOf(prefix, StringComparison.Ordinal);
        if (lineStart < 0) return;

        var afterPrefix = lineStart + prefix.Length;
        _chatHistory.Text = text[..afterPrefix] + SpinnerFrames[_spinnerFrame] + "\n";
    }

    private void ReplaceThinkingLine(string replacement)
    {
        var text = _chatHistory.Text ?? "";
        var prefix = "Assistant: ";
        var lineStart = text.LastIndexOf(prefix, StringComparison.Ordinal);
        if (lineStart >= 0)
        {
            _chatHistory.Text = text[..lineStart] + replacement;
        }
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
