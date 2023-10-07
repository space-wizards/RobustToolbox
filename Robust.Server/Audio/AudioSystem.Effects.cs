using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Effects;
using Robust.Shared.GameObjects;

namespace Robust.Server.Audio;

public sealed partial class AudioSystem
{
    private void InitializeEffect()
    {
        SubscribeLocalEvent<AudioEffectComponent, ComponentStartup>(OnEffectStartup);
        SubscribeLocalEvent<AudioAuxiliaryComponent, ComponentStartup>(OnAuxiliaryStartup);
    }

    private void OnEffectStartup(EntityUid uid, AudioEffectComponent component, ComponentStartup args)
    {
        component.Effect = new DummyAudioEffect();
    }

    private void OnAuxiliaryStartup(EntityUid uid, AudioAuxiliaryComponent component, ComponentStartup args)
    {
        component.Auxiliary = new DummyAuxiliaryAudio();
    }

    public override (EntityUid Entity, AudioAuxiliaryComponent Component) CreateAuxiliary()
    {
        var (ent, comp) = base.CreateAuxiliary();
        _pvs.AddGlobalOverride(GetNetEntity(ent));
        return (ent, comp);
    }

    public override (EntityUid Entity, AudioEffectComponent Component) CreateEffect()
    {
        var (ent, comp) = base.CreateEffect();
        _pvs.AddGlobalOverride(GetNetEntity(ent));
        return (ent, comp);
    }
}
