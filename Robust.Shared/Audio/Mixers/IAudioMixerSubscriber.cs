namespace Robust.Shared.Audio.Mixers;

/// <summary>
/// Implement this to be able to subscribe to <see cref="IAudioMixer"/>.
/// </summary>
public interface IAudioMixerSubscriber
{
    /// <summary>
    /// This is called from subscribed mixer when its gain is changed.
    /// </summary>
    void OnMixerGainChanged(float mixerGain);
}
