using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Maths;
using Robust.Shared.ResourceManagement.ResourceTypes;

namespace Robust.Client.Audio;

/// <summary>
/// Handles clientside audio.
/// </summary>
internal interface IAudioInternal
{
    void InitializePostWindowing();
    void Shutdown();

    /// <summary>
    /// Flushes all pending queues for disposing of AL sources.
    /// </summary>
    void FlushALDisposeQueues();

    IAudioSource? CreateAudioSource(AudioStream stream);

    IBufferedAudioSource CreateBufferedAudioSource(int buffers, bool floatAudio=false);

    /// <summary>
    /// Sets position for the audio listener.
    /// </summary>
    void SetPosition(Vector2 position);

    /// <summary>
    /// Sets rotation for the audio listener.
    /// </summary>
    void SetRotation(Angle angle);

    void SetMasterVolume(float value);

    /// <summary>
    /// Stops all audio from playing.
    /// </summary>
    void StopAllAudio();
}
