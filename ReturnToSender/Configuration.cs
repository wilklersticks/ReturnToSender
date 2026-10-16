using System;
using System.Collections.Generic;
using Dalamud.Configuration;

namespace ReturnToSender;

public enum EmoteFilterMode
{
    /// <summary>Track every emote aimed at you.</summary>
    All = 0,
    /// <summary>Track only /dote.</summary>
    DotesOnly = 1,
    /// <summary>Track only the emotes in <see cref="Configuration.CustomEmotes"/>.</summary>
    Custom = 2,
}

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    // ---- Box ----
    /// <summary>Whether the box is open; remembered between sessions.</summary>
    public bool BoxOpen { get; set; } = true;
    public bool LockBox { get; set; } = false;
    public float BoxOpacity { get; set; } = 0.75f;
    public int MaxEntries { get; set; } = 20;
    /// <summary>Keep returned emotes in the list (dimmed) instead of removing them.</summary>
    public bool KeepReturned { get; set; } = true;

    // ---- Which emotes to track ----
    public EmoteFilterMode FilterMode { get; set; } = EmoteFilterMode.All;
    /// <summary>Emote IDs (Emote sheet rows) used when <see cref="FilterMode"/> is Custom.</summary>
    public List<ushort> CustomEmotes { get; set; } = new();

    // ---- Returning ----
    /// <summary>Return with the same emote they used. If false, always use <see cref="CustomReturnCommand"/>.</summary>
    public bool ReturnSameEmote { get; set; } = true;
    public string CustomReturnCommand { get; set; } = "/dote";
    /// <summary>Add "motion" so the return doesn't print an emote message in chat.</summary>
    public bool MotionOnly { get; set; } = false;
    /// <summary>Put your previous target back after returning.</summary>
    public bool RestoreTarget { get; set; } = true;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
