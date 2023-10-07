using OpenTK.Audio.OpenAL.Extensions.Creative.EFX;
using Robust.Shared.Audio.Effects;

namespace Robust.Client.Audio.Effects;

/// <inheritdoc />
internal sealed class AuxiliaryAudio : IAuxiliaryAudio
{
    internal int Handle = EFX.GenAuxiliaryEffectSlot();

    public void Dispose()
    {
        if (Handle != -1)
        {
            EFX.DeleteAuxiliaryEffectSlot(Handle);
            Handle = -1;
        }
    }

    /// <inheritdoc />
    public void SetEffect(IAudioEffect? effectCompEffect)
    {
        if (effectCompEffect is AudioEffect effect)
        {
            EFX.AuxiliaryEffectSlot(Handle, EffectSlotInteger.Effect, effect.Handle);
        }
        else
        {
            EFX.AuxiliaryEffectSlot(Handle, EffectSlotInteger.Effect, 0);
        }
    }
}
