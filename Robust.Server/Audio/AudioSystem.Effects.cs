using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Effects;
using Robust.Shared.Collections;
using Robust.Shared.GameObjects;

namespace Robust.Server.Audio;

public sealed partial class AudioSystem
{
    protected override void InitializeEffect()
    {
        base.InitializeEffect();
        SubscribeLocalEvent<AudioEffectComponent, ComponentAdd>(OnEffectAdd);
        SubscribeLocalEvent<AudioAuxiliaryComponent, ComponentAdd>(OnAuxiliaryAdd);
    }

    private void ShutdownEffect()
    {
    }

    /// <summary>
    /// Reloads all <see cref="AudioPresetPrototype"/> entities.
    /// </summary>
    public void ReloadPresets()
    {
        var query = AllEntityQuery<AudioPresetComponent>();
        var toDelete = new ValueList<EntityUid>();

        while (query.MoveNext(out var uid, out _))
        {
            toDelete.Add(uid);
        }

        foreach (var ent in toDelete)
        {
            Del(ent);
        }

        foreach (var proto in ProtoMan.EnumeratePrototypes<AudioPresetPrototype>())
        {
            if (!proto.CreateAuxiliary)
                continue;

            var effect = CreateEffect();
            var aux = CreateAuxiliary();
            SetEffectPreset(effect.Entity, effect.Component, proto);
            SetEffect(aux.Entity, aux.Component, effect.Entity);
            var preset = AddComp<AudioPresetComponent>(aux.Entity);
            _auxiliaries.Remove(preset.Preset);
            preset.Preset = proto.ID;
            _auxiliaries[preset.Preset] = aux.Entity;
        }
    }

    private void OnEffectAdd(EntityUid uid, AudioEffectComponent component, ComponentAdd args)
    {
        component.Effect = new DummyAudioEffect();
    }

    private void OnAuxiliaryAdd(EntityUid uid, AudioAuxiliaryComponent component, ComponentAdd args)
    {
        component.Auxiliary = new DummyAuxiliaryAudio();
    }

    public override (EntityUid Entity, AudioAuxiliaryComponent Component) CreateAuxiliary()
    {
        var (ent, comp) = base.CreateAuxiliary();
        _pvs.AddGlobalOverride(ent);
        return (ent, comp);
    }

    public override (EntityUid Entity, AudioEffectComponent Component) CreateEffect()
    {
        var (ent, comp) = base.CreateEffect();
        _pvs.AddGlobalOverride(ent);
        return (ent, comp);
    }
}
