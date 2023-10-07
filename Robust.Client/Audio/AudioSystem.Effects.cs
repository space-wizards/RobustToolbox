using OpenTK.Audio.OpenAL.Extensions.Creative.EFX;
using Robust.Client.Audio.Effects;
using Robust.Shared.Audio.Components;
using Robust.Shared.GameObjects;

namespace Robust.Client.Audio;

public sealed partial class AudioSystem
{
    private void InitializeEffect()
    {
        SubscribeLocalEvent<AudioEffectComponent, ComponentStartup>(OnEffectStartup);
        SubscribeLocalEvent<AudioEffectComponent, ComponentShutdown>(OnEffectShutdown);

        SubscribeLocalEvent<AudioAuxiliaryComponent, ComponentStartup>(OnAuxiliaryStartup);
        SubscribeLocalEvent<AudioAuxiliaryComponent, AfterAutoHandleStateEvent>(OnAuxiliaryAuto);
    }

    private void OnEffectStartup(EntityUid uid, AudioEffectComponent component, ComponentStartup args)
    {
        var effect = new AudioEffect(_audio);
        EFX.Effect(effect.Handle, EffectInteger.EffectType, (int) EffectType.EaxReverb);
        component.Effect = effect;
    }

    private void OnEffectShutdown(EntityUid uid, AudioEffectComponent component, ComponentShutdown args)
    {
        component.Effect.Dispose();
    }

    private void OnAuxiliaryStartup(EntityUid uid, AudioAuxiliaryComponent component, ComponentStartup args)
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
}
