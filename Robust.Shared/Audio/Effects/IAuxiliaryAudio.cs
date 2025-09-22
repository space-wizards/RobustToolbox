using System;

namespace Robust.Shared.Audio.Effects;

public interface IAuxiliaryAudio : IDisposable
{
    /// <summary>
    /// Sets the audio effect for this auxiliary audio slot.
    /// </summary>
    void SetEffect(IAudioEffect? effect);
}
