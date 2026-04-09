using Terminal.Gui;

namespace AgentExplorer.Views;

/// <summary>
/// A read-only TextView with a built-in braille spinner animation
/// for showing a "thinking" indicator while waiting for a response.
/// </summary>
public sealed class ThinkingTextView : TextView
{
    private static readonly string[] SpinnerFrames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];
    private int _spinnerFrame;
    private bool _isThinking;
    private bool _timerStarted;

    public ThinkingTextView()
    {
        ReadOnly = true;
        WordWrap = true;
        CanFocus = false;
    }

    /// <summary>
    /// Start the timer on first use. Must be called after the view is
    /// added to a window (so App is available).
    /// </summary>
    public void EnsureTimerStarted()
    {
        if (_timerStarted) return;
        _timerStarted = true;

        Application.AddTimeout(TimeSpan.FromMilliseconds(80), () =>
        {
            if (_isThinking)
            {
                _spinnerFrame = (_spinnerFrame + 1) % SpinnerFrames.Length;
                UpdateThinkingLine();
            }
            return true;
        });
    }

    /// <summary>
    /// Show the spinner on a new "Assistant:" line.
    /// </summary>
    public void StartThinking()
    {
        _isThinking = true;
        Append($"Assistant: {SpinnerFrames[0]}");
    }

    /// <summary>
    /// Replace the spinner line. Call this when the first token arrives.
    /// Returns false if thinking wasn't active.
    /// </summary>
    public bool StopThinking()
    {
        if (!_isThinking) return false;
        _isThinking = false;
        ReplaceThinkingLine("");
        return true;
    }

    /// <summary>
    /// Replace the spinner line with specific text (e.g. "Assistant: (no response)").
    /// </summary>
    public void StopThinkingWith(string replacement)
    {
        _isThinking = false;
        ReplaceThinkingLine(replacement);
    }

    public bool IsThinking => _isThinking;

    public void Append(string text)
    {
        Text += text + "\n";
        MoveEnd();
    }

    public void AppendChunk(string chunk)
    {
        Text += chunk;
        MoveEnd();
    }

    private void UpdateThinkingLine()
    {
        var text = Text ?? "";
        var prefix = "Assistant: ";
        var lineStart = text.LastIndexOf(prefix, StringComparison.Ordinal);
        if (lineStart < 0) return;

        var afterPrefix = lineStart + prefix.Length;
        Text = text[..afterPrefix] + SpinnerFrames[_spinnerFrame] + "\n";
    }

    private void ReplaceThinkingLine(string replacement)
    {
        var text = Text ?? "";
        var prefix = "Assistant: ";
        var lineStart = text.LastIndexOf(prefix, StringComparison.Ordinal);
        if (lineStart >= 0)
        {
            Text = text[..lineStart] + replacement;
        }
    }
}
