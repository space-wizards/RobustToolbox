using System.Collections.Generic;
using System.Runtime.InteropServices;
using Robust.Shared;
using Robust.Shared.GameObjects;

namespace Robust.Client.Audio;

public sealed partial class AudioSystem
{
    /*
     * Handles limiting concurrent sounds for audio to avoid blowing the source budget on one sound getting spammed.
     */

    private readonly Dictionary<string, int> _playingCount = new();

    private int _maxConcurrent;

    private void InitializeLimit()
    {
        Subs.CVar(CfgManager, CVars.AudioDefaultConcurrent, SetConcurrentLimit, true);
    }

    private void SetConcurrentLimit(int obj)
    {
        _maxConcurrent = obj;
    }

    private bool TryAudioLimit(string sound)
    {
        if (string.IsNullOrEmpty(sound))
            return true;

        ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(_playingCount, sound, out _);

        if (count >= _maxConcurrent)
            return false;

        count++;
        return true;
    }

    private void RemoveAudioLimit(string sound)
    {
        if (!_playingCount.TryGetValue(sound, out var count))
            return;

        count--;

        if (count <= 0)
        {
            _playingCount.Remove(sound);
            return;
        }

        _playingCount[sound] = count;
    }
}
