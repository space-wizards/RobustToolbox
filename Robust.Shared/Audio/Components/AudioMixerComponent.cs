using System;
using Robust.Shared.Audio.Mixers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Audio.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedAudioSystem))]
public sealed partial class AudioMixerComponent : Component, IAudioMixer
{
    public IAudioMixer? Out => Mixer.Out;

    public EntityUid? OutEntity { get; set; }

    public ProtoId<AudioMixerPrototype>? ProtoId { get; set; }

    public float SelfGain
    {
        get => Mixer.SelfGain;
        set => Mixer.SelfGain = value;
    }
    public float Gain => Mixer.Gain;

    /// <summary>
    /// Set <see langword="true"/> if you want to control gain of the mixer from the server side.
    /// </summary>
    [Access(Other = AccessPermissions.ReadWrite)]
    public bool IsGainSynced { get; set; } = false;

    public string? GainCVar
    {
        get => Mixer.GainCVar;
        set => Mixer.GainCVar = value;
    }

    [ViewVariables]
    internal IAudioMixer Mixer = new DummyAudioMixer();

    internal bool IsInitiallySynced { get; set; } = false;

    public void Dispose() { }

    public void Subscribe(IAudioMixerSubscriber subscriber)
    {
        Mixer.Subscribe(subscriber);
    }

    public void Unsubscribe(IAudioMixerSubscriber subscriber)
    {
        Mixer.Unsubscribe(subscriber);
    }

    public void SetOut(IAudioMixer? outMixer)
    {
        Mixer.SetOut(outMixer);
    }

    public void OnMixerGainChanged(float mixerGain)
    {
        Mixer.OnMixerGainChanged(mixerGain);
    }

    void IAudioMixer.OnGainCVarChanged(float value)
    {
        Mixer.OnGainCVarChanged(value);
    }
}

[Serializable, NetSerializable]
public sealed class AudioMixerComponentState : IComponentState
{
    public NetEntity? OutEntity;
    public ProtoId<AudioMixerPrototype>? ProtoId;
    public float SelfGain;
    public bool IsGainSynced;
    public string? GainCVar;
}
