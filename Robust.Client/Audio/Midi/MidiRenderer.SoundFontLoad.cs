using System;
using Robust.Shared.Utility;

namespace Robust.Client.Audio.Midi;

internal sealed partial class MidiRenderer
{
    [Obsolete("Use LoadSoundfontResource or LoadSoundfontUser instead")]
    public void LoadSoundfont(string filename, bool resetPresets = true)
    {
        LoadSoundfontCore(
            MidiManager.PrefixPath(MidiManager.PrefixLegacy, filename),
            resetPresets);
    }

    public void LoadSoundfontResource(ResPath path, bool resetPresets = false)
    {
        LoadSoundfontCore(
            MidiManager.PrefixPath(MidiManager.PrefixResources, path.ToString()),
            resetPresets);
    }

    public void LoadSoundfontUser(ResPath path, bool resetPresets = false)
    {
        LoadSoundfontCore(
            MidiManager.PrefixPath(MidiManager.PrefixUser, path.ToString()),
            resetPresets);
    }

    internal void LoadSoundfontDisk(string path, bool resetPresets = false)
    {
        LoadSoundfontCore(
            path,
            resetPresets);
    }

    private void LoadSoundfontCore(string filenameString, bool resetPresets)
    {
        lock (_playerStateLock)
        {
            _synth.LoadSoundFont(filenameString, resetPresets);
            MidiSoundfont = 1;
        }
    }
}
