using System;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace ReturnToSender;

/// <summary>
/// Sends an emote back when YOU click Return. It targets the sender and runs the emote's
/// chat command, exactly as if you had typed it. Nothing here runs on its own.
/// </summary>
public sealed class Returner
{
    private readonly Configuration config;

    public Returner(Configuration config) => this.config = config;

    /// <summary>The sender if they're still nearby, else null.</summary>
    public static IGameObject? FindSender(EmoteEntry entry)
    {
        var obj = Plugin.ObjectTable.SearchById(entry.SenderId);
        if (obj != null && obj.Name.TextValue == entry.SenderName)
            return obj;

        // Fall back to a name search in case their object ID changed (e.g. they re-entered range).
        foreach (var o in Plugin.ObjectTable)
            if (o is Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter && o.Name.TextValue == entry.SenderName)
            {
                entry.SenderId = o.GameObjectId;
                return o;
            }

        return null;
    }

    public string CommandFor(EmoteEntry entry)
        => config.ReturnSameEmote && !string.IsNullOrEmpty(entry.Command)
            ? entry.Command
            : config.CustomReturnCommand.Trim();

    public bool CanReturn(EmoteEntry entry, out string reason)
    {
        if (FindSender(entry) == null)
        {
            reason = "They're no longer nearby.";
            return false;
        }

        var cmd = CommandFor(entry);
        if (!EmoteLog.IsEmoteCommand(cmd))
        {
            reason = $"\"{cmd}\" isn't an emote command. Check the return emote in settings.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public bool Return(EmoteEntry entry)
    {
        if (!CanReturn(entry, out var reason))
        {
            Plugin.Chat.PrintError(reason, "Return to Sender");
            return false;
        }

        var sender = FindSender(entry)!;
        var previous = Plugin.TargetManager.Target;

        Plugin.TargetManager.Target = sender;
        var cmd = CommandFor(entry) + (config.MotionOnly ? " motion" : string.Empty);
        SendCommand(cmd);

        if (config.RestoreTarget)
        {
            // Give the emote a few frames to start before switching back.
            Plugin.Framework.RunOnTick(() => Plugin.TargetManager.Target = previous, delayTicks: 3);
        }

        return true;
    }

    /// <summary>Runs a chat command as if typed into the chat box. Only ever called with validated emote commands.</summary>
    private static unsafe void SendCommand(string command)
    {
        if (command.Length > 100 || command.Contains('\n') || command.Contains('\r') || !command.StartsWith('/'))
            throw new ArgumentException("Refusing to send an unexpected command.", nameof(command));

        var uiModule = UIModule.Instance();
        if (uiModule == null)
            return;

        var str = Utf8String.FromString(command);
        try
        {
            uiModule->ProcessChatBoxEntry(str);
        }
        finally
        {
            str->Dtor(true);
        }
    }
}
