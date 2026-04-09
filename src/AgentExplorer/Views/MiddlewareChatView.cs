using Terminal.Gui;
using AgentExplorer.Agents.L04_Middleware;

namespace AgentExplorer.Views;

/// <summary>
/// Lesson 4: Extended chat view that adds a role selector and displays
/// audit log events (tool calls, context injections) inline in the chat.
/// </summary>
public sealed class MiddlewareChatView : View
{
    private readonly ThinkingTextView _chatHistory;
    private readonly TextField _inputField;
    private readonly FrameView _inputFrame;
    private readonly MiddlewareAssistant _agent;
    private readonly List<string> _pendingAuditEvents = [];

    private static readonly string[] Roles = ["Operator", "Supervisor", "Manager"];

    public MiddlewareChatView()
    {
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;

        _agent = new MiddlewareAssistant();

        // Role selector — horizontal radio buttons
        var roleSelector = new RadioGroup
        {
            X = 1,
            RadioLabels = Roles,
            Orientation = Orientation.Horizontal,
            SelectedItem = 0,
        };

        var roleFrame = new FrameView
        {
            Title = "User Role",
            Width = Dim.Fill(),
            Height = 3,
            BorderStyle = LineStyle.Rounded
        };
        roleFrame.Add(roleSelector);

        // Chat history
        var chatFrame = new FrameView
        {
            Title = $"L4: Middleware ({_agent.CurrentRole})",
            Y = Pos.Bottom(roleFrame),
            Width = Dim.Fill(),
            Height = Dim.Fill()! - 6,
            BorderStyle = LineStyle.Rounded
        };

        _chatHistory = new ThinkingTextView
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        chatFrame.Add(_chatHistory);

        // Role change handler
        roleSelector.SelectedItemChanged += (_, e) =>
        {
            var selected = Roles[roleSelector.SelectedItem];
            if (selected != _agent.CurrentRole)
            {
                _agent.CurrentRole = selected;
                chatFrame.Title = $"L4: Middleware ({selected})";
                _chatHistory.Append($"\n[Role changed to: {selected}]\n");
            }
        };

        // Input area
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

        // Audit event subscriber — queue during thinking, flush on first token
        _agent.AuditLog.OnEntry += entry =>
        {
            if (entry.EventType is "AgentRun" or "ToolCall" or "ToolResult" or "ContextInjection")
            {
                var prefix = entry.EventType switch
                {
                    "AgentRun" => "~~ ",
                    "ToolCall" => ">> ",
                    "ToolResult" => "<< ",
                    "ContextInjection" => "** ",
                    _ => "   "
                };
                lock (_pendingAuditEvents)
                {
                    _pendingAuditEvents.Add($"[{prefix}{entry.Detail}]");
                }
            }
        };

        _chatHistory.Append("Welcome to L4: Middleware & Context Providers.");
        _chatHistory.Append("Select a role from the dropdown to change context.");
        _chatHistory.Append("Tool calls and context injections are shown inline.\n");

        Add(roleFrame, chatFrame, _inputFrame);

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
        lock (_pendingAuditEvents) { _pendingAuditEvents.Clear(); }

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
                            _inputFrame.Title = "Message";
                            _chatHistory.StopThinking();
                            FlushAuditEvents();
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

    private void FlushAuditEvents()
    {
        List<string> events;
        lock (_pendingAuditEvents)
        {
            events = [.. _pendingAuditEvents];
            _pendingAuditEvents.Clear();
        }

        foreach (var evt in events)
        {
            _chatHistory.Append(evt);
        }
    }
}
