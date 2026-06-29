using System;

namespace Robust.Shared.Audio.Effects;

[NotContentImplementable]
public interface IAuxiliaryAudio : IDisposable
{
    /// <summary>
    /// Sets the audio effect for this auxiliary audio slot.
    /// </summary>
    void SetEffect(IAudioEffect? effect);
}
