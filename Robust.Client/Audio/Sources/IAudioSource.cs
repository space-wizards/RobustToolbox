using System;
using System.Numerics;
using JetBrains.Annotations;

namespace Robust.Client.Audio.Sources;

/// <summary>
/// Engine audio source that directly interacts with OpenAL.
/// </summary>
internal interface IAudioSource : IDisposable
{
    void StartPlaying();
    void StopPlaying();

    bool IsPlaying { get; }

    bool IsLooping { get; set; }
    bool IsGlobal { get; }

    [MustUseReturnValue]
    bool SetPosition(Vector2 position);
    void SetPitch(float pitch);
    void SetGlobal();
    void SetVolume(float decibels);
    void SetVolumeDirect(float gain);
    void SetMaxDistance(float maxDistance);
    void SetRolloffFactor(float rolloffFactor);
    void SetReferenceDistance(float refDistance);
    void SetOcclusion(float blocks);
    void SetPlaybackPosition(float seconds);
    void SetVelocity(Vector2 velocity);
}
