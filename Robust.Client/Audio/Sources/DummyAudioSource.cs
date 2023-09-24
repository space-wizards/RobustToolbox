using System.Numerics;

namespace Robust.Client.Audio.Sources;

/// <summary>
///     Hey look, it's AudioSource's evil twin brother!
/// </summary>
[Virtual]
internal class DummyAudioSource : IAudioSource
{
    public static DummyAudioSource Instance { get; } = new();

    public bool IsPlaying => default;
    public bool IsLooping { get; set; }

    public void Dispose()
    {
        // Nada.
    }

    public void StartPlaying()
    {
        // Nada.
    }

    public void StopPlaying()
    {
        // Nada.
    }

    public bool IsGlobal { get; }

    public bool SetPosition(Vector2 position)
    {
        return true;
    }

    public void SetPitch(float pitch)
    {
        // Nada.
    }

    public void SetGlobal()
    {
        // Nada.
    }

    public void SetVolume(float decibels)
    {
        // Nada.
    }

    public void SetVolumeDirect(float gain)
    {
        // Nada.
    }

    public void SetMaxDistance(float maxDistance)
    {
        // Nada.
    }

    public void SetRolloffFactor(float rolloffFactor)
    {
        // Nada.
    }

    public void SetReferenceDistance(float refDistance)
    {
        // Nada.
    }

    public void SetOcclusion(float blocks)
    {
        // Nada.
    }

    public void SetPlaybackPosition(float seconds)
    {
        // Nada.
    }

    public void SetVelocity(Vector2 velocity)
    {
        // Nada.
    }
}