using System;
using System.Collections.Generic;
using System.Linq;
using Lumina.Excel.Sheets;

namespace ReturnToSender;

/// <summary>One emote players can use, e.g. Dote (/dote).</summary>
public sealed record EmoteInfo(ushort Id, string Name, string Command);

/// <summary>
/// Looks up emotes from the game's own data, so names and commands match your game language
/// and new emotes from patches appear automatically.
/// </summary>
public static class EmoteCatalog
{
    private static List<EmoteInfo>? all;
    private static ushort? doteId;

    /// <summary>Every emote that has a chat command, sorted by name.</summary>
    public static IReadOnlyList<EmoteInfo> All => all ??= Load();

    /// <summary>The emote ID for /dote, looked up by its command rather than hard-coded.</summary>
    public static ushort DoteId => doteId ??= FindByCommand("/dote")?.Id ?? 0;

    public static EmoteInfo? Get(ushort id) => All.FirstOrDefault(e => e.Id == id);

    // The box checks commands every frame, so remember answers instead of rescanning the sheet.
    private static readonly Dictionary<string, EmoteInfo?> commandCache = new(StringComparer.OrdinalIgnoreCase);

    public static EmoteInfo? FindByCommand(string command)
    {
        command = Normalize(command);
        if (commandCache.TryGetValue(command, out var cached))
            return cached;

        var found = ScanForCommand(command);
        commandCache[command] = found;
        return found;
    }

    private static EmoteInfo? ScanForCommand(string command)
    {
        foreach (var row in Plugin.DataManager.GetExcelSheet<Emote>())
        {
            var tc = row.TextCommand.ValueNullable;
            if (tc == null) continue;
            var t = tc.Value;
            if (Matches(t.Command.ExtractText(), command) || Matches(t.ShortCommand.ExtractText(), command) ||
                Matches(t.Alias.ExtractText(), command))
                return Get((ushort)row.RowId);
        }
        return null;
    }

    /// <summary>Emotes whose name or command contains the text (best matches first).</summary>
    public static IEnumerable<EmoteInfo> Search(string text, int max = 8)
    {
        text = text.Trim().TrimStart('/');
        if (text.Length == 0) return Enumerable.Empty<EmoteInfo>();

        return All
            .Select(e => (e, rank: Rank(e, text)))
            .Where(x => x.rank < 4)
            .OrderBy(x => x.rank).ThenBy(x => x.e.Name)
            .Take(max)
            .Select(x => x.e);
    }

    private static int Rank(EmoteInfo e, string text)
    {
        var cmd = e.Command.TrimStart('/');
        if (cmd.Equals(text, StringComparison.OrdinalIgnoreCase) || e.Name.Equals(text, StringComparison.OrdinalIgnoreCase)) return 0;
        if (cmd.StartsWith(text, StringComparison.OrdinalIgnoreCase) || e.Name.StartsWith(text, StringComparison.OrdinalIgnoreCase)) return 1;
        if (e.Name.Contains(text, StringComparison.OrdinalIgnoreCase) || cmd.Contains(text, StringComparison.OrdinalIgnoreCase)) return 2;
        return 4;
    }

    private static List<EmoteInfo> Load()
    {
        var list = new List<EmoteInfo>();
        foreach (var row in Plugin.DataManager.GetExcelSheet<Emote>())
        {
            var name = row.Name.ExtractText();
            var cmd = row.TextCommand.ValueNullable?.Command.ExtractText() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(cmd)) continue;
            list.Add(new EmoteInfo((ushort)row.RowId, name, cmd));
        }
        return list.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string Normalize(string command)
    {
        command = command.Trim();
        return command.StartsWith('/') ? command : "/" + command;
    }

    private static bool Matches(string candidate, string command)
        => !string.IsNullOrEmpty(candidate) && string.Equals(candidate, command, StringComparison.OrdinalIgnoreCase);
}
