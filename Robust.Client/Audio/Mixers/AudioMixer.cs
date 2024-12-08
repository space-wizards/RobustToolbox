using System;
using System.Collections.Generic;
using Robust.Shared.Audio.Mixers;
using Robust.Shared.Prototypes;

namespace Robust.Client.Audio.Mixers;

public sealed class AudioMixer : IAudioMixer
{
    public IAudioMixer? Out => _out;

    public float SelfGain
    {
        get => _selfGain;
        set
        {
            if (_isDisposed) return;
            _selfGain = value;
            _selfGain = Math.Max(_selfGain, 0);
            RecalculateGain();
        }
    }
    public float Gain { get; private set; }

    public ProtoId<AudioMixerPrototype>? ProtoId { get; }
    string? IAudioMixer.GainCVar
    {
        get => _gainCVar;
        set => _gainCVar = _isDisposed ? null : value;
    }

    private readonly AudioMixersManager _manager;
    private readonly HashSet<IAudioMixerSubscriber> _subscribers = new();
    private IAudioMixer? _out;
    private float _selfGain = 1f;
    private string? _gainCVar;
    private bool _isDisposed = false;
    private bool _isNotifyingSubscribers = false;

    internal AudioMixer(ProtoId<AudioMixerPrototype>? protoId, AudioMixersManager manager)
    {
        ProtoId = protoId;
        _manager = manager;
        Recalculate();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        SetDefaults();
        SetOut(null);
        _subscribers.Clear();
        _manager.SetMixerGainCVar(this, null);
        _isDisposed = true;
    }

    public void Subscribe(IAudioMixerSubscriber subscriber)
    {
        if (_isDisposed) return;
        _subscribers.Add(subscriber);
    }

    public void Unsubscribe(IAudioMixerSubscriber subscriber)
    {
        if (_isDisposed) return;
        _subscribers.Remove(subscriber);
    }

    public void SetOut(IAudioMixer? outMixer)
    {
        if (_out == outMixer || outMixer == this || _isDisposed) return;
        if (_out is { })
        {
            _out.Unsubscribe(this);
        }
        _out = outMixer;
        if (outMixer is { })
        {
            outMixer.Subscribe(this);
        }
        Recalculate();
    }

    void IAudioMixerSubscriber.OnMixerGainChanged(float mixerGain)
    {
        RecalculateGain();
    }

    void IAudioMixer.OnGainCVarChanged(float value)
    {
        SelfGain = value;
    }

    private void Recalculate()
    {
        RecalculateGain();
    }

    private void RecalculateGain()
    {
        if (_isNotifyingSubscribers)
        {
            _manager.Sawmill.Error($"Audio mixer {ToString()} has a circular output.");
            return;
        }
        var gain = (_out?.Gain ?? 1f) * _selfGain;
        Gain = gain;
        _isNotifyingSubscribers = true;
        foreach (var subscriber in _subscribers)
        {
            subscriber.OnMixerGainChanged(gain);
        }
        _isNotifyingSubscribers = false;
    }

    private void SetDefaults()
    {
        _selfGain = 1f;
    }

    public override string ToString()
    {
        return $"{{ Proto: {ProtoId} GainCVar: {_gainCVar} }}";
    }
}
