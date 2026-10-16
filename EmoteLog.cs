using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace ReturnToSender;

public sealed class EmoteEntry
{
    public string SenderName { get; init; } = string.Empty;
    public string World { get; init; } = string.Empty;
    public ulong SenderId { get; set; }
    public ushort EmoteId { get; init; }
    public string EmoteName { get; init; } = string.Empty;
    /// <summary>Chat command for this emote, e.g. "/dote". Empty if it has none.</summary>
    public string Command { get; init; } = string.Empty;
    public int Count { get; set; } = 1;
    public DateTime LastTime { get; set; } = DateTime.Now;
    public bool Returned { get; set; }

    public string Who => string.IsNullOrEmpty(World) ? SenderName : $"{SenderName}@{World}";
}

/// <summary>In-memory list of recent emotes at you. Cleared on logout; never saved to disk.</summary>
public sealed class EmoteLog
{
    private readonly Configuration config;
    private readonly List<EmoteEntry> entries = new();

    public EmoteLog(Configuration config) => this.config = config;

    /// <summary>Newest first.</summary>
    public IReadOnlyList<EmoteEntry> Entries => entries;
    public int UnreturnedCount => entries.Count(e => !e.Returned);

    /// <summary>Whether the current filter says this emote should be tracked.</summary>
    public bool IsTracked(ushort emoteId) => config.FilterMode switch
    {
        EmoteFilterMode.DotesOnly => emoteId == EmoteCatalog.DoteId,
        EmoteFilterMode.Custom => config.CustomEmotes.Contains(emoteId),
        _ => true,
    };

    /// <summary>Short description of the filter for the box header, or null when tracking everything.</summary>
    public string? FilterLabel => config.FilterMode switch
    {
        EmoteFilterMode.DotesOnly => "Dotes only",
        EmoteFilterMode.Custom => config.CustomEmotes.Count == 1
            ? $"Only {EmoteCatalog.Get(config.CustomEmotes[0])?.Name ?? "1 emote"}"
            : $"{config.CustomEmotes.Count} chosen emotes",
        _ => null,
    };

    public void Add(IPlayerCharacter sender, ushort emoteId)
    {
        if (!IsTracked(emoteId))
            return;

        var info = EmoteCatalog.Get(emoteId);
        var name = sender.Name.TextValue;
        var world = sender.HomeWorld.ValueNullable?.Name.ExtractText() ?? string.Empty;

        // Repeats of the same emote from the same person collapse into one line (×2, ×3 ...)
        // as long as you haven't returned it yet.
        var existing = entries.FirstOrDefault(e =>
            !e.Returned && e.EmoteId == emoteId && e.SenderName == name && e.World == world);

        if (existing != null)
        {
            existing.Count++;
            existing.LastTime = DateTime.Now;
            existing.SenderId = sender.GameObjectId;
            entries.Remove(existing);
            entries.Insert(0, existing);
        }
        else
        {
            entries.Insert(0, new EmoteEntry
            {
                SenderName = name,
                World = world,
                SenderId = sender.GameObjectId,
                EmoteId = emoteId,
                EmoteName = info?.Name ?? $"Emote #{emoteId}",
                Command = info?.Command ?? string.Empty,
            });
        }

        Trim();
    }

    /// <summary>Drop entries the current filter no longer tracks. Call after changing the filter.</summary>
    public void ApplyFilter() => entries.RemoveAll(e => !IsTracked(e.EmoteId));

    public void MarkReturned(EmoteEntry entry)
    {
        entry.Returned = true;
        if (!config.KeepReturned)
            entries.Remove(entry);
    }

    public void MarkAllReturned()
    {
        foreach (var e in entries) e.Returned = true;
        if (!config.KeepReturned) entries.Clear();
    }

    public void Remove(EmoteEntry entry) => entries.Remove(entry);
    public void Clear() => entries.Clear();

    public void Trim()
    {
        var max = Math.Max(1, config.MaxEntries);
        if (entries.Count <= max) return;

        // Drop the oldest returned entries first, then the oldest of anything.
        for (var i = entries.Count - 1; i >= 0 && entries.Count > max; i--)
            if (entries[i].Returned) entries.RemoveAt(i);
        while (entries.Count > max)
            entries.RemoveAt(entries.Count - 1);
    }

    /// <summary>True if <paramref name="command"/> is a real emote command like "/dote".</summary>
    public static bool IsEmoteCommand(string command)
        => command.Trim().StartsWith('/') && EmoteCatalog.FindByCommand(command) != null;
}
