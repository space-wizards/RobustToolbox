using System;
using System.Collections.Generic;
using System.IO;
using Robust.Shared.Audio.Sources;

namespace Robust.Client.Audio;

/// <summary>
/// Public audio API for stuff that can't go through <see cref="AudioSystem"/>
/// </summary>
[NotContentImplementable]
public interface IAudioManager
{
    IAudioSource? CreateAudioSource(AudioStream stream);

    AudioStream LoadAudioOggVorbis(Stream stream, string? name = null);

    AudioStream LoadAudioWav(Stream stream, string? name = null);

    AudioStream LoadAudioRaw(ReadOnlySpan<short> samples, int channels, int sampleRate, string? name = null);

    void SetMasterGain(float gain);

    /// <summary>
    /// Gets the list of available audio output device names.
    /// </summary>
    IReadOnlyList<string> GetAudioDevices();

    /// <summary>
    /// Gets the name of the currently active audio device.
    /// Empty string means "system default".
    /// </summary>
    string GetCurrentDeviceName();

    /// <summary>
    /// Whether the engine supports live audio device switching. (May not be supported on MacOS)
    /// </summary>
    bool CanSwitchDevice();

    /// <summary>
    /// Requests switching to the given audio device.
    /// This will show a confirmation dialog to the user.
    /// </summary>
    /// <param name="deviceName">Device name, or null/empty for system default.</param>
    /// <returns>True if the switch was initiated, false if unsupported or another switch is pending.</returns>
    bool RequestDeviceSwitch(string? deviceName);

    /// <summary>
    /// Raised when the audio device has changed.
    /// </summary>
    event Action? AudioDeviceChanged;
}
