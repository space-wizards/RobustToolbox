using OpenTK.Audio.OpenAL.Extensions.Creative.EFX;
using Robust.Client.Audio.Effects;
using Robust.Shared.Audio.Components;
using Robust.Shared.GameObjects;

namespace Robust.Client.Audio;

public sealed partial class AudioSystem
{
    private void InitializeEffect()
    {
        SubscribeLocalEvent<AudioEffectComponent, ComponentInit>(OnEffectInit);
        SubscribeLocalEvent<AudioEffectComponent, ComponentShutdown>(OnEffectShutdown);

        SubscribeLocalEvent<AudioAuxiliaryComponent, ComponentInit>(OnAuxiliaryInit);
        SubscribeLocalEvent<AudioAuxiliaryComponent, AfterAutoHandleStateEvent>(OnAuxiliaryAuto);
    }

    private void OnEffectInit(EntityUid uid, AudioEffectComponent component, ComponentInit args)
    {
        var effect = new AudioEffect(_audio);
        EFX.Effect(effect.Handle, EffectInteger.EffectType, (int) EffectType.EaxReverb);
        component.Effect = effect;
    }

    private void OnEffectShutdown(EntityUid uid, AudioEffectComponent component, ComponentShutdown args)
    {
        component.Effect.Dispose();
    }

    private void OnAuxiliaryInit(EntityUid uid, AudioAuxiliaryComponent component, ComponentInit args)
    {
        component.Auxiliary = new AuxiliaryAudio();
    }

    private void OnAuxiliaryAuto(EntityUid uid, AudioAuxiliaryComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (TryComp<AudioEffectComponent>(component.Effect, out var effectComp))
        {
            component.Auxiliary.SetEffect(effectComp.Effect);
        }
        else
        {
            component.Auxiliary.SetEffect(null);
        }
    }

    public override void SetAuxiliary(EntityUid uid, AudioComponent audio, EntityUid? auxUid)
    {
        base.SetAuxiliary(uid, audio, auxUid);
        if (TryComp<AudioAuxiliaryComponent>(audio.Auxiliary, out var auxComp))
        {
            audio.Source.SetAuxiliary(auxComp.Auxiliary);
        }
        else
        {
            audio.Source.SetAuxiliary(null);
        }
    }

    public override void SetEffect(EntityUid auxUid, AudioAuxiliaryComponent aux, EntityUid? effectUid)
    {
        base.SetEffect(auxUid, aux, effectUid);
        if (TryComp<AudioEffectComponent>(aux.Effect, out var effectComp))
        {
            aux.Auxiliary.SetEffect(effectComp.Effect);
        }
        else
        {
            aux.Auxiliary.SetEffect(null);
        }
    }
}
