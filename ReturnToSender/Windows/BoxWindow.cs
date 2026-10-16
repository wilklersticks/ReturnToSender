using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;

namespace ReturnToSender.Windows;

/// <summary>The small box listing recent emotes at you.</summary>
public class BoxWindow : Window
{
    private static readonly Vector4 UnreturnedColor = new(1.00f, 0.82f, 0.40f, 1f); // warm gold
    private static readonly Vector4 UnreturnedBg = new(1.00f, 0.82f, 0.40f, 0.10f);

    private readonly Plugin plugin;
    private Configuration Config => plugin.Configuration;
    private EmoteLog Log => plugin.EmoteLog;

    public BoxWindow(Plugin plugin) : base("Return to Sender###RtsBox")
    {
        this.plugin = plugin;
        Size = new Vector2(340, 220);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(240, 90),
            MaximumSize = new Vector2(800, 900),
        };
        RespectCloseHotkey = false; // Esc shouldn't close a box you keep on screen
    }

    public override void PreDraw()
    {
        var unreturned = Log.UnreturnedCount;
        WindowName = unreturned > 0
            ? $"Return to Sender ({unreturned})###RtsBox"
            : "Return to Sender###RtsBox";

        BgAlpha = Config.BoxOpacity;
        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse;
        if (Config.LockBox)
            Flags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;
    }

    public override void OnOpen() => plugin.SaveBoxOpen(true);
    public override void OnClose() => plugin.SaveBoxOpen(false);

    public override void Draw()
    {
        DrawToolbar();
        ImGui.Separator();

        if (plugin.Hook is { IsValid: false })
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, "Emote detection failed to start (likely a game patch). See /xllog.");
            return;
        }

        if (Config.FilterMode == EmoteFilterMode.Custom && Config.CustomEmotes.Count == 0)
        {
            ImGui.TextColored(ImGuiColors.DalamudOrange, "Your emote list is empty, so nothing is tracked.");
            if (ImGui.SmallButton("Open settings"))
                plugin.ToggleConfig();
            return;
        }

        if (Log.Entries.Count == 0)
        {
            ImGui.TextDisabled(Config.FilterMode == EmoteFilterMode.DotesOnly
                ? "No one has doted on you yet."
                : "No one has emoted at you yet.");
            return;
        }

        using var child = ImRaii_Child("##rtslist");
        // Copy so buttons can modify the list safely mid-draw.
        foreach (var entry in Log.Entries.ToList())
            DrawEntry(entry);
    }

    private void DrawToolbar()
    {
        using (new Disabled(Log.UnreturnedCount == 0))
        {
            if (ImGui.SmallButton("Mark all returned"))
                Log.MarkAllReturned();
        }

        ImGui.SameLine();
        using (new Disabled(Log.Entries.Count == 0))
        {
            if (ImGui.SmallButton("Clear"))
                Log.Clear();
        }

        // Active filter, e.g. "Dotes only" (click to change)
        var filter = Log.FilterLabel;
        if (filter != null)
        {
            ImGui.SameLine();
            ImGui.TextDisabled($"· {filter}");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                ImGui.SetTooltip("Only these emotes are tracked. Click to change.");
            }
            if (ImGui.IsItemClicked())
                plugin.ToggleConfig();
        }

        // Settings cog on the right
        var cogWidth = ImGui.GetFrameHeight();
        ImGui.SameLine(ImGui.GetContentRegionMax().X - cogWidth);
        if (ImGuiComponents.IconButton("##rtscog", FontAwesomeIcon.Cog))
            plugin.ToggleConfig();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Settings");
    }

    private void DrawEntry(EmoteEntry entry)
    {
        ImGui.PushID(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(entry));

        var start = ImGui.GetCursorScreenPos();
        var rowHeight = ImGui.GetFrameHeight();

        // Soft gold background behind unreturned rows.
        if (!entry.Returned)
        {
            var end = new Vector2(start.X + ImGui.GetContentRegionAvail().X, start.Y + rowHeight);
            ImGui.GetWindowDrawList().AddRectFilled(start, end, ImGui.GetColorU32(UnreturnedBg), 4f);
        }

        // Return button
        var canReturn = plugin.Returner.CanReturn(entry, out var reason);
        using (new Disabled(!canReturn))
        {
            if (ImGui.Button(entry.Returned ? "Again" : "Return", new Vector2(56 * ImGuiScale(), 0)))
            {
                if (plugin.Returner.Return(entry))
                    Log.MarkReturned(entry);
            }
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip(canReturn
                ? $"Target {entry.SenderName} and use {plugin.Returner.CommandFor(entry)}"
                : reason);
        }

        // Name (click to target) - emote ×n - time ago
        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        var nameColor = entry.Returned ? ImGuiColors.DalamudGrey : UnreturnedColor;
        ImGui.TextColored(nameColor, entry.SenderName);
        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            ImGui.SetTooltip($"{entry.Who}\nClick to target. Right-click for options.");
        }
        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            var sender = Returner.FindSender(entry);
            if (sender != null) Plugin.TargetManager.Target = sender;
        }
        RowContextMenu(entry);

        ImGui.SameLine();
        var detail = entry.Count > 1 ? $"{entry.EmoteName} ×{entry.Count}" : entry.EmoteName;
        if (entry.Returned)
            ImGui.TextDisabled(detail);
        else
            ImGui.TextUnformatted(detail);

        ImGui.SameLine();
        ImGui.TextDisabled(TimeAgo(entry.LastTime));

        ImGui.PopID();
    }

    private void RowContextMenu(EmoteEntry entry)
    {
        if (ImGui.BeginPopupContextItem("##rowmenu"))
        {
            if (!entry.Returned && ImGui.MenuItem("Mark as returned"))
                Log.MarkReturned(entry);
            if (entry.Returned && ImGui.MenuItem("Mark as unreturned"))
                entry.Returned = false;
            if (ImGui.MenuItem("Remove from list"))
                Log.Remove(entry);
            if (Config.FilterMode == EmoteFilterMode.Custom)
            {
                ImGui.Separator();
                if (ImGui.MenuItem($"Stop tracking {entry.EmoteName}"))
                {
                    Config.CustomEmotes.Remove(entry.EmoteId);
                    Config.Save();
                    Log.ApplyFilter();
                }
            }
            ImGui.EndPopup();
        }
    }

    private static string TimeAgo(DateTime time)
    {
        var span = DateTime.Now - time;
        if (span.TotalSeconds < 60) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        return time.ToString("MMM d");
    }

    private static float ImGuiScale() => Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale;

    private static ChildScope ImRaii_Child(string id) => new(id);

    private readonly struct ChildScope : IDisposable
    {
        public ChildScope(string id) => ImGui.BeginChild(id, Vector2.Zero, false);
        public void Dispose() => ImGui.EndChild();
    }

    private readonly struct Disabled : IDisposable
    {
        private readonly bool active;
        public Disabled(bool disabled)
        {
            active = disabled;
            if (active) ImGui.BeginDisabled();
        }
        public void Dispose()
        {
            if (active) ImGui.EndDisabled();
        }
    }
}
