using System;
using System.IO;

namespace Robust.Client.Audio;

/// <summary>
/// Public audio API for stuff that can't go through <see cref="AudioSystem"/>
/// </summary>
public interface IAudioManager
{
    AudioStream LoadAudioOggVorbis(Stream stream, string? name = null);

    AudioStream LoadAudioWav(Stream stream, string? name = null);

    AudioStream LoadAudioRaw(ReadOnlySpan<short> samples, int channels, int sampleRate, string? name = null);

    void SetMasterGain(float gain);
}
