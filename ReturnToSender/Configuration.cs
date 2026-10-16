using System;
using Dalamud.Configuration;

namespace ReturnToSender;

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
