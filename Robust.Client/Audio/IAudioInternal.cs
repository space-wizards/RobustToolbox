using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Maths;

namespace Robust.Client.Audio;

/// <summary>
/// Handles clientside audio.
/// </summary>
internal interface IAudioInternal : IAudioManager
{
    void InitializePostWindowing();
    void Shutdown();

    /// <summary>
    /// Flushes all pending queues for disposing of AL sources.
    /// </summary>
    void FlushALDisposeQueues();

    /// <summary>
    /// Returns a buffered audio source.
    /// </summary>
    /// <returns>null if unable to create the source.</returns>
    IBufferedAudioSource? CreateBufferedAudioSource(int buffers, bool floatAudio=false);

    /// <summary>
    /// Sets the velocity for the audio listener.
    /// </summary>
    void SetVelocity(Vector2 velocity);

    /// <summary>
    /// Sets position for the audio listener.
    /// </summary>
    void SetPosition(Vector2 position);

    /// <summary>
    /// Sets rotation for the audio listener.
    /// </summary>
    void SetRotation(Angle angle);

    void SetAttenuation(Attenuation attenuation);

    void Remove(AudioStream stream);

    /// <summary>
    /// Stops all audio from playing.
    /// </summary>
    void StopAllAudio();

    /// <summary>
    /// Sets the Z-offset for the audio listener.
    /// </summary>
    void SetZOffset(float f);

    void _checkAlError([CallerMemberName] string callerMember = "", [CallerLineNumber] int callerLineNumber = -1);

    /// <summary>
    /// Manually calculates the specified gain for an attenuation source with the specified distance.
    /// </summary>
    float GetAttenuationGain(float distance, float rolloffFactor, float referenceDistance, float maxDistance);
}
