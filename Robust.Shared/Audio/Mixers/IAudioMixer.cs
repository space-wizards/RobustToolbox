using System;

using Robust.Shared.Prototypes;

namespace Robust.Shared.Audio.Mixers;

/// <summary>
/// Controls the parameters of the audio sources to which this mixer is assigned.
/// Mixers can also output signal to other mixers, creating a hierarchy.
/// </summary>
public interface IAudioMixer : IAudioMixerSubscriber, IDisposable
{
    /// <summary>
    /// Mixer to pass signal to.
    /// </summary>
    IAudioMixer? Out { get; }
    /// <summary>
    /// Audio mixer prototype id that is associated with this mixer.
    /// </summary>
    ProtoId<AudioMixerPrototype>? ProtoId { get; }
    /// <summary>
    /// Gain assigned to this mixer before passing to any output mixers.
    /// </summary>
    float SelfGain { get; set; }
    /// <summary>
    /// Gain of this mixer after passing through all output chain.
    /// </summary>
    float Gain { get; }
    /// <summary>
    /// Name of the CVar bound to this mixer to store gain value.
    /// </summary>
    internal string? GainCVar { get; set; }

    /// <summary>
    /// Subscribes to this mixer instance.
    /// </summary>
    void Subscribe(IAudioMixerSubscriber subscriber);
    /// <summary>
    /// Unsubscribes from this mixer instance.
    /// </summary>
    void Unsubscribe(IAudioMixerSubscriber subscriber);
    /// <summary>
    /// Set specified mixer as an output for this mixer, pass <see langword="null"/> to set as root mixer.
    /// </summary>
    void SetOut(IAudioMixer? outMixer);
    /// <summary>
    /// Called when the value of the gain CVar is changed.
    /// </summary>
    internal void OnGainCVarChanged(float value);
}
