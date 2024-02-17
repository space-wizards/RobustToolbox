using System;
using Robust.Shared.Maths;

namespace Robust.Shared.Audio.Effects;

public interface IAudioEffect
{
    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.ReverbDensity"/>.
    /// </summary>
    public float Density { get; set; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.ReverbDiffusion "/>.
    /// </summary>
    public float Diffusion { get; set; }

    /// <summary>
    /// Gets the preset value for <ReverbGainsee cref="EffectFloat.ReverbGain"/>.
    /// </summary>
    public float Gain { get; set; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.ReverbGainHF"/>.
    /// </summary>
    public float GainHF { get; set; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.EaxReverbGainLF"/>.
    /// </summary>
    public float GainLF { get; set; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.ReverbDecayTime"/>.
    /// </summary>
    public float DecayTime { get; set; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.ReverbDecayHFRatio"/>.
    /// </summary>
    public float DecayHFRatio { get; set; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.EaxReverbDecayLFRatio"/>.
    /// </summary>
    public float DecayLFRatio { get; set; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.ReverbReflectionsGain"/>.
    /// </summary>
    public float ReflectionsGain { get; set; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.ReverbReflectionsDelay"/>.
    /// </summary>
    public float ReflectionsDelay { get; set; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectVector3.EaxReverbReflectionsPan"/>.
    /// </summary>
    public Vector3 ReflectionsPan { get; set; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.ReverbLateReverbGain"/>.
    /// </summary>
    public float LateReverbGain { get; set; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.ReverbLateReverbDelay"/>.
    /// </summary>
    public float LateReverbDelay { get; set; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectVector3.EaxReverbLateReverbPan"/>.
    /// </summary>
    public Vector3 LateReverbPan { get; set; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.EaxReverbEchoTime"/>.
    /// </summary>
    public float EchoTime { get; set; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.EaxReverbEchoDepth"/>.
    /// </summary>
    public float EchoDepth { get; set; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.EaxReverbModulationTime"/>.
    /// </summary>
    public float ModulationTime { get; set; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.EaxReverbModulationDepth"/>.
    /// </summary>
    public float ModulationDepth { get; set; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.ReverbAirAbsorptionGainHF"/>.
    /// </summary>
    public float AirAbsorptionGainHF { get; set; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.EaxReverbHFReference"/>.
    /// </summary>
    public float HFReference { get; set; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.EaxReverbLFReference"/>.
    /// </summary>
    public float LFReference { get; set; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.ReverbRoomRolloffFactor"/>.
    /// </summary>
    public float RoomRolloffFactor { get; set; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectInteger.ReverbDecayHFLimit"/>.
    /// </summary>
    public int DecayHFLimit { get; set; }
}
