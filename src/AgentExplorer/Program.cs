using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using AgentExplorer.Views;

// Use the built-in "Dark" theme — Terminal.Gui v2 ships with it
ConfigurationManager.RuntimeConfig = """{ "Theme": "Dark" }""";
ConfigurationManager.Enable(ConfigLocations.All);

IApplication app = Application.Create().Init();
app.Run<MainWindow>();
app.Dispose();
