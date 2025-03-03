using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Audio;

/// <summary>
/// Contains audio defaults to set for sounds.
/// This can be used by <see cref="Content.Shared.Audio.SharedContentAudioSystem"/> to apply an audio preset.
/// </summary>
[Prototype]
public sealed partial class AudioPresetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// Should the engine automatically create an auxiliary audio effect slot for this.
    /// </summary>
    [DataField]
    public bool CreateAuxiliary;

    [DataField]
    public float Density;

    [DataField]
    public float Diffusion;

    [DataField]
    public float Gain;

    [DataField("gainHf")]
    public float GainHF;

    [DataField("gainLf")]
    public float GainLF;

    [DataField]
    public float DecayTime;

    [DataField("decayHfRatio")]
    public float DecayHFRatio;

    [DataField("decayLfRatio")]
    public float DecayLFRatio;

    [DataField]
    public float ReflectionsGain;

    [DataField]
    public float ReflectionsDelay;

    [DataField]
    public Vector3 ReflectionsPan;

    [DataField]
    public float LateReverbGain;

    [DataField]
    public float LateReverbDelay;

    [DataField]
    public Vector3 LateReverbPan;

    [DataField]
    public float EchoTime;

    [DataField]
    public float EchoDepth;

    [DataField]
    public float ModulationTime;

    [DataField]
    public float ModulationDepth;

    [DataField("airAbsorptionGainHf")]
    public float AirAbsorptionGainHF;

    [DataField("hfReference")]
    public float HFReference;

    [DataField("lfReference")]
    public float LFReference;

    [DataField]
    public float RoomRolloffFactor;

    [DataField("decayHfLimit")]
    public int DecayHFLimit;
}
