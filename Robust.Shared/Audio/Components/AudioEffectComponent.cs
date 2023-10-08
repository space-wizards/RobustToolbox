using System;
using Robust.Shared.Audio.Effects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Audio.Components;

/// <summary>
/// Stores OpenAL audio effect data that can be bound to an <see cref="AudioAuxiliaryComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedAudioSystem)), AutoGenerateComponentState]
public sealed partial class AudioEffectComponent : Component, IAudioEffect
{
    [ViewVariables]
    internal IAudioEffect Effect = new DummyAudioEffect();

    /// <inheritdoc />
    [DataField, AutoNetworkedField]
    public float Density
    {
        get => Effect.Density;
        set => Effect.Density = value;
    }

    /// <inheritdoc />
    [DataField, AutoNetworkedField]
    public float Diffusion
    {
        get => Effect.Diffusion;
        set => Effect.Diffusion = value;
    }

    /// <inheritdoc />
    [DataField, AutoNetworkedField]
    public float Gain
    {
        get => Effect.Gain;
        set => Effect.Gain = value;
    }

    /// <inheritdoc />
    [DataField, AutoNetworkedField]
    public float GainHF
    {
        get => Effect.GainHF;
        set => Effect.GainHF = value;
    }

    /// <inheritdoc />
    [DataField, AutoNetworkedField]
    public float GainLF
    {
        get => Effect.GainLF;
        set => Effect.GainLF = value;
    }

    /// <inheritdoc />
    [DataField, AutoNetworkedField]
    public float DecayTime
    {
        get => Effect.DecayTime;
        set => Effect.DecayTime = value;
    }

    /// <inheritdoc />
    [DataField, AutoNetworkedField]
    public float DecayHFRatio
    {
        get => Effect.DecayHFRatio;
        set => Effect.DecayHFRatio = value;
    }

    /// <inheritdoc />
    [DataField, AutoNetworkedField]
    public float DecayLFRatio
    {
        get => Effect.DecayLFRatio;
        set => Effect.DecayLFRatio = value;
    }

    /// <inheritdoc />
    [DataField, AutoNetworkedField]
    public float ReflectionsGain
    {
        get => Effect.ReflectionsGain;
        set => Effect.ReflectionsGain = value;
    }

    /// <inheritdoc />
    [DataField, AutoNetworkedField]
    public float ReflectionsDelay
    {
        get => Effect.ReflectionsDelay;
        set => Effect.ReflectionsDelay = value;
    }

    /// <inheritdoc />
    [DataField, AutoNetworkedField]
    public Vector3 ReflectionsPan
    {
        get => Effect.ReflectionsPan;
        set => Effect.ReflectionsPan = value;
    }

    /// <inheritdoc />
    [DataField, AutoNetworkedField]
    public float LateReverbGain
    {
        get => Effect.LateReverbGain;
        set => Effect.LateReverbGain = value;
    }

    /// <inheritdoc />
    [DataField, AutoNetworkedField]
    public float LateReverbDelay
    {
        get => Effect.LateReverbDelay;
        set => Effect.LateReverbDelay = value;
    }

    /// <inheritdoc />
    [DataField, AutoNetworkedField]
    public Vector3 LateReverbPan
    {
        get => Effect.LateReverbPan;
        set => Effect.LateReverbPan = value;
    }

    /// <inheritdoc />
    [DataField, AutoNetworkedField]
    public float EchoTime
    {
        get => Effect.EchoTime;
        set => Effect.EchoTime = value;
    }

    /// <inheritdoc />
    [DataField, AutoNetworkedField]
    public float EchoDepth
    {
        get => Effect.EchoDepth;
        set => Effect.EchoDepth = value;
    }

    /// <inheritdoc />
    [DataField, AutoNetworkedField]
    public float ModulationTime
    {
        get => Effect.ModulationTime;
        set => Effect.ModulationTime = value;
    }

    /// <inheritdoc />
    [DataField, AutoNetworkedField]
    public float ModulationDepth
    {
        get => Effect.ModulationDepth;
        set => Effect.ModulationDepth = value;
    }

    /// <inheritdoc />
    [DataField, AutoNetworkedField]
    public float AirAbsorptionGainHF
    {
        get => Effect.AirAbsorptionGainHF;
        set => Effect.AirAbsorptionGainHF = value;
    }

    /// <inheritdoc />
    [DataField, AutoNetworkedField]
    public float HFReference
    {
        get => Effect.HFReference;
        set => Effect.HFReference = value;
    }

    /// <inheritdoc />
    [DataField, AutoNetworkedField]
    public float LFReference
    {
        get => Effect.LFReference;
        set => Effect.LFReference = value;
    }

    /// <inheritdoc />
    [DataField, AutoNetworkedField]
    public float RoomRolloffFactor
    {
        get => Effect.RoomRolloffFactor;
        set => Effect.RoomRolloffFactor = value;
    }

    /// <inheritdoc />
    [DataField, AutoNetworkedField]
    public int DecayHFLimit
    {
        get => Effect.DecayHFLimit;
        set => Effect.DecayHFLimit = value;
    }
}
