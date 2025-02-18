using Robust.Shared.Prototypes;

namespace Robust.Shared.Audio.Mixers;

internal sealed class DummyAudioMixer : IAudioMixer
{
    public float SelfGain { get; set; } = 1f;
    public float Gain => SelfGain;
    public IAudioMixer? Out { get; }
    public ProtoId<AudioMixerPrototype>? ProtoId { get; }
    string? IAudioMixer.GainCVar { get; set; }

    public void Subscribe(IAudioMixerSubscriber subscriber)
    {
    }

    public void Unsubscribe(IAudioMixerSubscriber subscriber)
    {
    }

    public void Dispose()
    {
    }

    public void SetOut(IAudioMixer? outMixer)
    {
    }

    public void OnMixerGainChanged(float mixerGain)
    {
    }

    void IAudioMixer.OnGainCVarChanged(float value)
    {
    }
}
