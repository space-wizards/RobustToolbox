using System;
using System.IO;
using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.Audio.AudioLoading;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Maths;

namespace Robust.Client.Audio;

/// <summary>
/// Headless client audio.
/// </summary>
internal sealed class HeadlessAudioManager : IAudioInternal
{
    private int _audioBuffer;

    /// <inheritdoc />
    public void InitializePostWindowing()
    {
    }

    /// <inheritdoc />
    public void Shutdown()
    {
    }

    /// <inheritdoc />
    public void FlushALDisposeQueues()
    {
    }

    /// <inheritdoc />
    public IAudioSource CreateAudioSource(AudioStream stream)
    {
        return DummyAudioSource.Instance;
    }

    /// <inheritdoc />
    public IBufferedAudioSource? CreateBufferedAudioSource(int buffers, bool floatAudio = false)
    {
        return DummyBufferedAudioSource.Instance;
    }

    /// <inheritdoc />
    public void SetVelocity(Vector2 velocity)
    {
    }

    /// <inheritdoc />
    public void SetPosition(Vector2 position)
    {
    }

    /// <inheritdoc />
    public void SetRotation(Angle angle)
    {
    }

    /// <inheritdoc />
    public void SetMasterGain(float newGain)
    {
    }

    /// <inheritdoc />
    public void SetAttenuation(Attenuation attenuation)
    {
    }

    /// <inheritdoc />
    public void Remove(AudioStream stream)
    {
    }

    /// <inheritdoc />
    public void StopAllAudio()
    {
    }

    /// <inheritdoc />
    public void SetZOffset(float f)
    {
    }

    /// <inheritdoc />
    public void _checkAlError(string callerMember = "", int callerLineNumber = -1)
    {
    }

    /// <inheritdoc />
    public float GetAttenuationGain(float distance, float rolloffFactor, float referenceDistance, float maxDistance)
    {
        return 0f;
    }

    public AudioStream LoadAudioOggVorbis(Stream stream, string? name = null)
    {
        var metadata = AudioLoaderOgg.LoadAudioMetadata(stream);
        return AudioStreamFromMetadata(metadata, name);
    }

    public AudioStream LoadAudioWav(Stream stream, string? name = null)
    {
        var metadata = AudioLoaderWav.LoadAudioMetadata(stream);
        return AudioStreamFromMetadata(metadata, name);
    }

    public AudioStream LoadAudioRaw(ReadOnlySpan<short> samples, int channels, int sampleRate, string? name = null)
    {
        var length = TimeSpan.FromSeconds((double) samples.Length / channels / sampleRate);
        return new AudioStream(this, _audioBuffer++, null, length, channels, name);
    }

    private AudioStream AudioStreamFromMetadata(AudioMetadata metadata, string? name)
    {
        return new AudioStream(this, _audioBuffer++, null, metadata.Length, metadata.ChannelCount, name, metadata.Title, metadata.Artist);
    }
}
