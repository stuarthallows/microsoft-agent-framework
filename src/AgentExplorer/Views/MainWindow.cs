using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using AgentExplorer.Agents.L01_Foundation;
using AgentExplorer.Agents.L02_ToolAgent;
using AgentExplorer.Agents.L03_MCP;

namespace AgentExplorer.Views;

/// <summary>
/// Main application window with a tab layout for switching between lessons.
/// </summary>
public sealed class MainWindow : Runnable
{
    public MainWindow()
    {
        Title = "Agent Explorer - MAF Learning TUI";
        BorderStyle = LineStyle.Rounded;

        var tabView = new TabView
        {
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1
        };

        // Lesson 1: Chat without tools
        tabView.AddTab(new Tab
        {
            DisplayText = "L1: Chat",
            View = new ChatView(new ProductionAssistant())
        }, andSelect: true);

        // Lesson 2: Chat with tools
        tabView.AddTab(new Tab
        {
            DisplayText = "L2: Tools",
            View = new ChatView(new ToolUsingAssistant())
        }, andSelect: false);

        // Lesson 3: Chat with MCP + local tools
        tabView.AddTab(new Tab
        {
            DisplayText = "L3: MCP",
            View = new ChatView(new LazyMcpAgent())
        }, andSelect: false);

        // Status bar
        var statusBar = new Label
        {
            Text = " Esc: Quit | Enter: Send message",
            Y = Pos.Bottom(tabView),
            Width = Dim.Fill(),
            Height = 1,
            SchemeName = "Menu"
        };

        Add(tabView, statusBar);
    }
}
