using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Effects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Audio.Systems;

public abstract partial class SharedAudioSystem
{
    /// <summary>
    /// Creates an auxiliary audio slot that can have an audio source or audio effect applied to it.
    /// </summary>
    public virtual (EntityUid Entity, AudioAuxiliaryComponent Component) CreateAuxiliary()
    {
        var ent = Spawn(null, MapCoordinates.Nullspace);
        var comp = AddComp<AudioAuxiliaryComponent>(ent);
        return (ent, comp);
    }

    /// <summary>
    /// Creates an audio effect that can be used with an auxiliary audio slot.
    /// </summary>
    public virtual (EntityUid Entity, AudioEffectComponent Component) CreateEffect()
    {
        var ent = Spawn(null, MapCoordinates.Nullspace);
        var comp = AddComp<AudioEffectComponent>(ent);
        return (ent, comp);
    }

    /// <summary>
    /// Sets the auxiliary effect slot for a specified audio source.
    /// </summary>
    public virtual void SetAuxiliary(EntityUid uid, AudioComponent audio, EntityUid? auxUid)
    {
        DebugTools.Assert(auxUid == null || HasComp<AudioAuxiliaryComponent>(auxUid));
        audio.Auxiliary = auxUid;
        Dirty(uid, audio);
    }

    /// <summary>
    /// Sets the audio effect for a specified auxiliary effect slot.
    /// </summary>
    public virtual void SetEffect(EntityUid auxUid, AudioAuxiliaryComponent aux, EntityUid? effectUid)
    {
        DebugTools.Assert(effectUid == null || HasComp<AudioEffectComponent>(effectUid));
        aux.Effect = effectUid;
        Dirty(auxUid, aux);
    }

    /// <summary>
    /// Applies an EAX reverb effect preset to an audio effect.
    /// </summary>
    public void SetEffectPreset(EntityUid effectUid, AudioEffectComponent effectComp, ReverbProperties preset)
    {
        effectComp.Density = preset.Density;
        effectComp.Diffusion = preset.Diffusion;
        effectComp.Gain = preset.Gain;
        effectComp.GainHF = preset.GainHF;
        effectComp.GainLF = preset.GainLF;
        effectComp.DecayTime = preset.DecayTime;
        effectComp.DecayHFRatio = preset.DecayHFRatio;
        effectComp.DecayLFRatio = preset.DecayLFRatio;
        effectComp.ReflectionsGain = preset.ReflectionsGain;
        effectComp.ReflectionsDelay = preset.ReflectionsDelay;
        effectComp.ReflectionsPan = preset.ReflectionsPan;
        effectComp.LateReverbGain = preset.LateReverbGain;
        effectComp.LateReverbDelay = preset.LateReverbDelay;
        effectComp.LateReverbPan = preset.LateReverbPan;
        effectComp.EchoTime = preset.EchoTime;
        effectComp.EchoDepth = preset.EchoDepth;
        effectComp.ModulationTime = preset.ModulationTime;
        effectComp.ModulationDepth = preset.ModulationDepth;
        effectComp.AirAbsorptionGainHF = preset.AirAbsorptionGainHF;
        effectComp.HFReference = preset.HFReference;
        effectComp.LFReference = preset.LFReference;
        effectComp.RoomRolloffFactor = preset.RoomRolloffFactor;
        effectComp.DecayHFLimit = preset.DecayHFLimit;

        Dirty(effectUid, effectComp);
    }
}
