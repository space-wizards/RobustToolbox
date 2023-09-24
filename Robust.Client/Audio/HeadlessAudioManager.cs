using System.Numerics;
using Robust.Client.Audio.Sources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Maths;

namespace Robust.Client.Audio;

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
    public IAudioSource CreateAudioSource(AudioResource audioResource)
    {
        return DummyAudioSource.Instance;
    }

    /// <inheritdoc />
    public IBufferedAudioSource? CreateBufferedAudioSource(int buffers, bool floatAudio = false)
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
}
