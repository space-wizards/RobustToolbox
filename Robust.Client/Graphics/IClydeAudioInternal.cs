using Robust.Client.ResourceManagement;

namespace Robust.Client.Graphics;

/// <summary>
/// Handles clientside audio.
/// </summary>
internal interface IClydeAudioInternal
{
    void InitializePostWindowing();
    void Shutdown();

    /// <summary>
    /// Flushes all pending queues for disposing of AL sources.
    /// </summary>
    void FlushALDisposeQueues();

    IClydeAudioSource CreateAudioSource(AudioResource audioResource);

    /// <summary>
    /// Sets position for the audio listener.
    /// </summary>
    void SetPosition();
}
