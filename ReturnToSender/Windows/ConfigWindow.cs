using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;

namespace ReturnToSender.Windows;

public class ConfigWindow : Window
{
    private readonly Plugin plugin;
    private Configuration Config => plugin.Configuration;
    private string commandDraft;

    public ConfigWindow(Plugin plugin) : base("Return to Sender Settings###RtsConfig")
    {
        this.plugin = plugin;
        commandDraft = plugin.Configuration.CustomReturnCommand;
        Size = new Vector2(380, 360);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        ImGui.TextUnformatted("Returning");
        ImGui.Separator();

        var same = Config.ReturnSameEmote;
        if (ImGui.Checkbox("Return with the same emote they used", ref same))
        {
            Config.ReturnSameEmote = same;
            Config.Save();
        }

        ImGui.TextUnformatted(same ? "Fallback emote:" : "Always return with:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120);
        if (ImGui.InputText("##cmd", ref commandDraft, 32))
        {
            var cmd = commandDraft.Trim();
            if (!cmd.StartsWith('/')) cmd = "/" + cmd;
            if (EmoteLog.IsEmoteCommand(cmd))
            {
                Config.CustomReturnCommand = cmd;
                Config.Save();
            }
        }
        if (!EmoteLog.IsEmoteCommand(commandDraft.Trim().StartsWith('/') ? commandDraft.Trim() : "/" + commandDraft.Trim()))
            ImGui.TextColored(ImGuiColors.DalamudOrange, "  Not a known emote command. Example: /dote, /hug, /pet");
        else if (same)
            ImGui.TextDisabled("  Used when their emote has no command of its own.");

        var motion = Config.MotionOnly;
        if (ImGui.Checkbox("Motion only (no emote message in chat)", ref motion))
        {
            Config.MotionOnly = motion;
            Config.Save();
        }

        var restore = Config.RestoreTarget;
        if (ImGui.Checkbox("Switch back to my previous target afterwards", ref restore))
        {
            Config.RestoreTarget = restore;
            Config.Save();
        }

        ImGui.Spacing();
        ImGui.TextUnformatted("Box");
        ImGui.Separator();

        var keep = Config.KeepReturned;
        if (ImGui.Checkbox("Keep returned emotes in the list (dimmed)", ref keep))
        {
            Config.KeepReturned = keep;
            Config.Save();
        }

        var max = Config.MaxEntries;
        ImGui.SetNextItemWidth(150);
        if (ImGui.SliderInt("Entries to keep", ref max, 5, 50))
        {
            Config.MaxEntries = max;
            Config.Save();
            plugin.EmoteLog.Trim();
        }

        var opacity = Config.BoxOpacity;
        ImGui.SetNextItemWidth(150);
        if (ImGui.SliderFloat("Background opacity", ref opacity, 0.1f, 1f, "%.2f"))
        {
            Config.BoxOpacity = opacity;
            Config.Save();
        }

        var locked = Config.LockBox;
        if (ImGui.Checkbox("Lock box position and size", ref locked))
        {
            Config.LockBox = locked;
            Config.Save();
        }

        ImGui.Spacing();
        ImGui.TextDisabled("The list is cleared when you log out. /rts toggles the box.");
    }
}
