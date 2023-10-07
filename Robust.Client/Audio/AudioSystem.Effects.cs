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

    /*
    public void SetEffect(AuxiliaryEffect auxiliary)
    {
        EFX.Effect();

        EFX.Source(sourceHandle, EFXSourceInteger3.AuxiliarySendFilter, auxiliarySlot, 0, 0);
    }

    public void SetAuxiliary(EntityUid entity, AudioComponent component, AuxiliaryEffect auxiliary)
    {
        // value2 is for send slot
        // value3 is for optional EFX.Filter
        EFX.Source(component.Source.Handle, EFXSourceInteger3.AuxiliarySendFilter, auxiliary.Handle, 0, 0);
    }
    */
}
