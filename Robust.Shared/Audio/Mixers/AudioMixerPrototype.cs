using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Audio.Mixers;

/// <summary>
/// Preset for creating <see cref="IAudioMixer"/>s from.
/// </summary>
[Prototype("audioMixer")]
public sealed class AudioMixerPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// Name of the CVar bound to this mixer to store gain value.
    /// </summary>
    [DataField]
    public string? GainCVar;

    /// <summary>
    /// Mixer to pass signal to.
    /// </summary>
    [DataField]
    public ProtoId<AudioMixerPrototype>? Out;

    /// <summary>
    /// Default volume of the mixer, if no <see cref="GainCVar"/> is specified.
    /// </summary>
    [DataField]
    public float Volume
    {
        get => SharedAudioSystem.GainToVolume(Gain);
        set => Gain = SharedAudioSystem.VolumeToGain(value);
    }

    /// <summary>
    /// Default gain of the mixer, if no <see cref="GainCVar"/> is specified.
    /// </summary>
    [DataField]
    public float Gain = 1f;
}
