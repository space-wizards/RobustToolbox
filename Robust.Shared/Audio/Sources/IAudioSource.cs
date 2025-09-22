using System;
using System.Numerics;
using Robust.Shared.Audio.Effects;

namespace Robust.Shared.Audio.Sources;

/// <summary>
/// Engine audio source that directly interacts with OpenAL.
/// </summary>
/// <remarks>
/// This just exists so client can interact with OpenAL and server can interact with nothing.
/// </remarks>
public interface IAudioSource : IDisposable
{
    void Pause();

    /// <summary>
    /// Tries to start playing the audio if not already playing.
    /// </summary>
    void StartPlaying();

    /// <summary>
    /// Stops playing a source if it is currently playing.
    /// </summary>
    void StopPlaying();

    /// <summary>
    /// Restarts the audio source from the beginning.
    /// </summary>
    void Restart();

    /// <summary>
    /// Is the audio source currently playing.
    /// </summary>
    bool Playing { get; set; }

    /// <summary>
    /// Will the audio source loop when finished playing?
    /// </summary>
    bool Looping { get; set; }

    /// <summary>
    /// Is the audio source relative to the listener or to the world (global or local).
    /// </summary>
    bool Global { get; set; }

    /// <summary>
    /// Position of the audio relative to listener.
    /// </summary>
    Vector2 Position { get; set; }

    float Pitch { get; set; }

    /// <summary>
    /// Decibels of the audio. Converted to gain when setting.
    /// </summary>
    float Volume { get; set; }

    /// <summary>
    /// Direct gain for audio.
    /// </summary>
    float Gain { get; set; }

    float MaxDistance { get; set; }

    float RolloffFactor { get; set; }

    float ReferenceDistance { get; set; }

    float Occlusion { get; set; }

    /// <summary>
    /// Current playback position.
    /// </summary>
    float PlaybackPosition { get; set; }

    /// <summary>
    /// Audio source velocity. Used for the doppler effect.
    /// </summary>
    Vector2 Velocity { get; set; }

    /// <summary>
    /// Set the auxiliary sendfilter for this audio source.
    /// </summary>
    void SetAuxiliary(IAuxiliaryAudio? audio);
}
