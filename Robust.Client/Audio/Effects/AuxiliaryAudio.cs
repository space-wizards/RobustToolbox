using OpenTK.Audio.OpenAL;
using Robust.Shared.Audio.Effects;

namespace Robust.Client.Audio.Effects;

/// <inheritdoc />
internal sealed class AuxiliaryAudio : IAuxiliaryAudio
{
    internal int Handle = ALC.EFX.GenAuxiliaryEffectSlot();

    public void Dispose()
    {
        if (Handle != -1)
        {
            ALC.EFX.DeleteAuxiliaryEffectSlot(Handle);
            Handle = -1;
        }
    }

    /// <inheritdoc />
    public void SetEffect(IAudioEffect? effect)
    {
        if (effect is AudioEffect audEffect)
        {
            ALC.EFX.AuxiliaryEffectSlot(Handle, EffectSlotInteger.Effect, audEffect.Handle);
        }
        else
        {
            ALC.EFX.AuxiliaryEffectSlot(Handle, EffectSlotInteger.Effect, 0);
        }
    }
}
