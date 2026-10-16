using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;

namespace ReturnToSender;

/// <summary>
/// Hooks the game function that plays an emote on a character. It runs for every emote
/// your client sees, whether or not a chat message is printed, so motion-only emotes and
/// hidden emote messages are still caught. We only read it; the original always runs.
/// </summary>
public sealed class EmoteHook : IDisposable
{
    // Same signature PatMe uses. If detection stops after a game patch, this is what needs updating.
    private const string OnEmoteSignature = "E8 ?? ?? ?? ?? 48 8D 8B ?? ?? ?? ?? 4C 89 74 24";

    private delegate void OnEmoteDelegate(ulong unk, ulong instigatorAddr, ushort emoteId, ulong targetId, ulong unk2);

    private readonly Hook<OnEmoteDelegate>? hook;

    /// <summary>Raised when another player emotes at you.</summary>
    public event Action<IPlayerCharacter, ushort>? EmotedAtMe;

    public bool IsValid => hook != null;

    public EmoteHook()
    {
        try
        {
            hook = Plugin.GameInterop.HookFromSignature<OnEmoteDelegate>(OnEmoteSignature, Detour);
            hook.Enable();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[ReturnToSender] Could not hook the emote function; emotes won't be detected.");
            hook = null;
        }
    }

    private void Detour(ulong unk, ulong instigatorAddr, ushort emoteId, ulong targetId, ulong unk2)
    {
        try
        {
            var me = Plugin.ObjectTable.LocalPlayer;
            if (me != null && targetId == me.GameObjectId)
            {
                foreach (var obj in Plugin.ObjectTable)
                {
                    if ((ulong)obj.Address != instigatorAddr)
                        continue;

                    if (obj is IPlayerCharacter pc && pc.GameObjectId != me.GameObjectId)
                        EmotedAtMe?.Invoke(pc, emoteId);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            // Never let a plugin error break the game's emote handling.
            Plugin.Log.Error(ex, "[ReturnToSender] Error while reading an emote.");
        }

        hook!.Original(unk, instigatorAddr, emoteId, targetId, unk2);
    }

    public void Dispose() => hook?.Dispose();
}
