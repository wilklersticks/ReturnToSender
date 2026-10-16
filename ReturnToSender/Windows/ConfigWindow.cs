using System.Linq;
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
    private string filterSearch = string.Empty;

    public ConfigWindow(Plugin plugin) : base("Return to Sender Settings###RtsConfig")
    {
        this.plugin = plugin;
        commandDraft = plugin.Configuration.CustomReturnCommand;
        Size = new Vector2(400, 520);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        DrawFilterSection();
        ImGui.Spacing();
        DrawReturnSection();
        ImGui.Spacing();
        DrawBoxSection();

        ImGui.Spacing();
        ImGui.TextDisabled("The list is cleared when you log out. /rts toggles the box.");
    }

    // ---------------------------------------------------------------- which emotes

    private void DrawFilterSection()
    {
        ImGui.TextUnformatted("Which emotes to track");
        ImGui.Separator();

        var mode = (int)Config.FilterMode;
        var changed = false;
        changed |= ImGui.RadioButton("All emotes", ref mode, (int)EmoteFilterMode.All);
        ImGui.SameLine();
        changed |= ImGui.RadioButton("Dotes only", ref mode, (int)EmoteFilterMode.DotesOnly);
        ImGui.SameLine();
        changed |= ImGui.RadioButton("My list", ref mode, (int)EmoteFilterMode.Custom);
        if (changed)
            SetFilterMode((EmoteFilterMode)mode);

        if (Config.FilterMode == EmoteFilterMode.DotesOnly && EmoteCatalog.DoteId == 0)
            ImGui.TextColored(ImGuiColors.DalamudOrange, "  Couldn't find /dote in the game data.");

        if (Config.FilterMode != EmoteFilterMode.Custom)
        {
            ImGui.TextDisabled("  Emotes outside the filter aren't recorded at all.");
            return;
        }

        DrawCustomList();
    }

    private void DrawCustomList()
    {
        // Current picks as removable tags
        if (Config.CustomEmotes.Count == 0)
        {
            ImGui.TextColored(ImGuiColors.DalamudOrange, "  Your list is empty, so nothing is being tracked. Add emotes below.");
        }
        else
        {
            var lineStart = true;
            foreach (var id in Config.CustomEmotes.ToList())
            {
                var info = EmoteCatalog.Get(id);
                var label = info != null ? $"{info.Name}  ×###tag{id}" : $"Emote #{id}  ×###tag{id}";

                // Wrap tags onto the next line when they'd run past the edge.
                var width = ImGui.CalcTextSize(label.Split("###")[0]).X + ImGui.GetStyle().FramePadding.X * 2;
                if (!lineStart)
                {
                    var room = ImGui.GetContentRegionAvail().X;
                    if (room - ImGui.GetStyle().ItemSpacing.X > width) ImGui.SameLine();
                }
                lineStart = false;

                if (ImGui.SmallButton(label))
                {
                    Config.CustomEmotes.Remove(id);
                    SaveFilter();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip($"Stop tracking {info?.Name ?? "this emote"} ({info?.Command})");
            }
        }

        // Search box with suggestions
        ImGui.Spacing();
        ImGui.SetNextItemWidth(-1);
        var enter = ImGui.InputTextWithHint("##filtersearch", "Add an emote: type a name or command, e.g. hug or /pet",
                                            ref filterSearch, 40, ImGuiInputTextFlags.EnterReturnsTrue);

        var matches = EmoteCatalog.Search(filterSearch)
                                  .Where(e => !Config.CustomEmotes.Contains(e.Id))
                                  .ToList();

        if (enter && matches.Count > 0)
        {
            AddCustom(matches[0].Id);
            ImGui.SetKeyboardFocusHere(-1); // keep typing the next one
            return;
        }

        if (filterSearch.Trim().Length > 0)
        {
            if (matches.Count == 0)
            {
                ImGui.TextDisabled("  No matching emotes.");
            }
            else
            {
                ImGui.TextDisabled("  Click to add (Enter adds the first):");
                foreach (var e in matches)
                {
                    if (ImGui.Selectable($"  {e.Name}   {e.Command}###add{e.Id}"))
                    {
                        AddCustom(e.Id);
                        break;
                    }
                }
            }
        }
    }

    private void AddCustom(ushort id)
    {
        if (!Config.CustomEmotes.Contains(id))
            Config.CustomEmotes.Add(id);
        filterSearch = string.Empty;
        SaveFilter();
    }

    private void SetFilterMode(EmoteFilterMode mode)
    {
        Config.FilterMode = mode;
        SaveFilter();
    }

    private void SaveFilter()
    {
        Config.Save();
        plugin.EmoteLog.ApplyFilter();
    }

    // ---------------------------------------------------------------- returning

    private void DrawReturnSection()
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
            var cmd = Normalize(commandDraft);
            if (EmoteLog.IsEmoteCommand(cmd))
            {
                Config.CustomReturnCommand = cmd;
                Config.Save();
            }
        }
        if (!EmoteLog.IsEmoteCommand(Normalize(commandDraft)))
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
    }

    // ---------------------------------------------------------------- box

    private void DrawBoxSection()
    {
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
    }

    private static string Normalize(string command)
    {
        command = command.Trim();
        return command.StartsWith('/') ? command : "/" + command;
    }
}
