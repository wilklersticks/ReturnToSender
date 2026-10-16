using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ReturnToSender.Windows;

namespace ReturnToSender;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static ITargetManager TargetManager { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IChatGui Chat { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInterop { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string CommandName = "/rts";

    public Configuration Configuration { get; }
    public EmoteLog EmoteLog { get; }
    public Returner Returner { get; }
    public EmoteHook Hook { get; }

    public readonly WindowSystem WindowSystem = new("ReturnToSender");
    private BoxWindow BoxWindow { get; }
    private ConfigWindow ConfigWindow { get; }

    private bool wasLoggedIn;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        EmoteLog = new EmoteLog(Configuration);
        Returner = new Returner(Configuration);

        BoxWindow = new BoxWindow(this);
        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(BoxWindow);
        WindowSystem.AddWindow(ConfigWindow);
        BoxWindow.IsOpen = Configuration.BoxOpen;

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Show/hide the Return to Sender box. \"/rts config\" opens settings, \"/rts clear\" empties the list."
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi += ToggleBox;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfig;
        Framework.Update += OnFrameworkUpdate;

        // Hook last, once everything it reports to exists.
        Hook = new EmoteHook();
        Hook.EmotedAtMe += OnEmotedAtMe;
    }

    public void Dispose()
    {
        Hook.EmotedAtMe -= OnEmotedAtMe;
        Hook.Dispose();

        Framework.Update -= OnFrameworkUpdate;
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleBox;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfig;

        WindowSystem.RemoveAllWindows();
        CommandManager.RemoveHandler(CommandName);
    }

    private void OnEmotedAtMe(IPlayerCharacter sender, ushort emoteId) => EmoteLog.Add(sender, emoteId);

    private void OnFrameworkUpdate(IFramework framework)
    {
        // History isn't kept past logout.
        var loggedIn = ClientState.IsLoggedIn;
        if (wasLoggedIn && !loggedIn)
            EmoteLog.Clear();
        wasLoggedIn = loggedIn;
    }

    private void OnCommand(string command, string args)
    {
        switch (args.Trim().ToLowerInvariant())
        {
            case "config":
            case "settings":
                ToggleConfig();
                break;
            case "clear":
                EmoteLog.Clear();
                break;
            default:
                ToggleBox();
                break;
        }
    }

    public void ToggleBox() => BoxWindow.Toggle();
    public void ToggleConfig() => ConfigWindow.Toggle();

    public void SaveBoxOpen(bool open)
    {
        if (Configuration.BoxOpen == open) return;
        Configuration.BoxOpen = open;
        Configuration.Save();
    }
}
