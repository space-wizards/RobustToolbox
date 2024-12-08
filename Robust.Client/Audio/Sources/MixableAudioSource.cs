using System.Numerics;
using Robust.Shared.Audio.Effects;
using Robust.Shared.Audio.Mixers;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Audio.Systems;
using Robust.Shared.ViewVariables;

namespace Robust.Client.Audio.Sources;

public sealed class MixableAudioSource : IMixableAudioSource, IAudioMixerSubscriber
{
    [ViewVariables(VVAccess.ReadOnly)]
    private readonly IAudioSource _innerSource;
    [ViewVariables(VVAccess.ReadOnly)]
    private IAudioMixer? _mixer;
    [ViewVariables]
    private float _selfGain = 1f;

    private bool _isDisposed = false;

    public MixableAudioSource(IAudioSource innerSource)
    {
        _innerSource = innerSource;
        _selfGain = innerSource.Gain;
    }

    public bool Playing
    {
        get => _innerSource.Playing;
        set => _innerSource.Playing = value;
    }

    public bool Looping
    {
        get => _innerSource.Looping;
        set => _innerSource.Looping = value;
    }

    public bool Global
    {
        get => _innerSource.Global;
        set => _innerSource.Global = value;
    }

    public Vector2 Position
    {
        get => _innerSource.Position;
        set => _innerSource.Position = value;
    }

    public float Pitch
    {
        get => _innerSource.Pitch;
        set => _innerSource.Pitch = value;
    }
    
    public float Volume
    {
        get
        {
            var gain = Gain;
            var volume = SharedAudioSystem.GainToVolume(gain);
            return volume;
        }
        set => Gain = SharedAudioSystem.VolumeToGain(value);
    }

    public float Gain
    {
        get => _selfGain;
        set
        {
            _selfGain = value;
            RecalculateGain();
        }
    }

    public float MaxDistance
    {
        get => _innerSource.MaxDistance;
        set => _innerSource.MaxDistance = value;
    }

    public float RolloffFactor
    {
        get => _innerSource.RolloffFactor;
        set => _innerSource.RolloffFactor = value;
    }

    public float ReferenceDistance
    {
        get => _innerSource.ReferenceDistance;
        set => _innerSource.ReferenceDistance = value;
    }

    public float Occlusion
    {
        get => _innerSource.Occlusion;
        set => _innerSource.Occlusion = value;
    }

    public float PlaybackPosition
    {
        get => _innerSource.PlaybackPosition;
        set => _innerSource.PlaybackPosition = value;
    }

    public Vector2 Velocity
    {
        get => _innerSource.Velocity;
        set => _innerSource.Velocity = value;
    }

    public void Pause()
    {
        _innerSource.Pause();
    }

    public void StartPlaying()
    {
        _innerSource.StartPlaying();
    }

    public void StopPlaying()
    {
        _innerSource.StopPlaying();
    }

    public void Restart()
    {
        _innerSource.Restart();
    }

    public void Dispose()
    {
        _isDisposed = true;
        _mixer?.Unsubscribe(this);
        _innerSource.Dispose();
    }

    public void SetAuxiliary(IAuxiliaryAudio? audio)
    {
        _innerSource.SetAuxiliary(audio);
    }

    public void SetMixer(IAudioMixer? mixer)
    {
        if (_mixer == mixer || _isDisposed)
        {
            return;
        }
        _mixer?.Unsubscribe(this);
        _mixer = mixer;
        _mixer?.Subscribe(this);
        Recalculate();
    }

    public void OnMixerGainChanged(float mixerGain)
    {
        RecalculateGain();
    }

    private void Recalculate()
    {
        RecalculateGain();
    }

    private void RecalculateGain()
    {
        _innerSource.Gain = _selfGain * (_mixer?.Gain ?? 1f);
    }
}
