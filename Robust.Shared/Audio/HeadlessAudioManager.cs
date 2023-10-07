using System.Numerics;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Maths;

namespace Robust.Shared.Audio;

/// <summary>
/// Headless client audio.
/// </summary>
internal sealed class HeadlessAudioManager : SharedAudioManager, IAudioInternal
{
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
    public IAudioSource? CreateAudioSource(AudioStream stream)
    {
        return DummyAudioSource.Instance;
    }

    /// <inheritdoc />
    public IBufferedAudioSource CreateBufferedAudioSource(int buffers, bool floatAudio = false)
    {
        return DummyBufferedAudioSource.Instance;
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
    public void SetMasterVolume(float value)
    {
    }

    /// <inheritdoc />
    public void SetAttenuation(Attenuation attenuation)
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
}
