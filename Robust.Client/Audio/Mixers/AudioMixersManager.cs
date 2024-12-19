using System.Collections.Generic;
using Robust.Shared.Audio.Mixers;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Robust.Client.Audio.Mixers;

public sealed class AudioMixersManager : IAudioMixersManager
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    internal ISawmill Sawmill => _sawmill ??= _logManager.GetSawmill("audiomixers"); 

    private readonly Dictionary<ProtoId<AudioMixerPrototype>, IAudioMixer> _audioMixers = new();
    private ISawmill? _sawmill;

    public IAudioMixer CreateMixer()
    {
        return CreateMixer(null);
    }

    public IAudioMixer? GetMixer(ProtoId<AudioMixerPrototype>? mixerProtoIdMaybe)
    {
        return GetMixer(mixerProtoIdMaybe, mixerProtoIdMaybe);
    }

    public void SetMixerGainCVar(IAudioMixer mixer, string? name)
    {
        if (mixer.GainCVar is { })
        {
            _configurationManager.UnsubValueChanged<float>(mixer.GainCVar, mixer.OnGainCVarChanged);
        }
        mixer.GainCVar = name;
        if (mixer.GainCVar is { })
        {
            _configurationManager.OnValueChanged<float>(mixer.GainCVar, mixer.OnGainCVarChanged, true);
        }
    }

    private IAudioMixer CreateMixer(ProtoId<AudioMixerPrototype>? mixerProtoIdMaybe)
    {
        return new AudioMixer(mixerProtoIdMaybe, this);
    }

    private IAudioMixer? GetMixer(ProtoId<AudioMixerPrototype>? mixerProtoIdMaybe, ProtoId<AudioMixerPrototype>? originProtoId)
    {
        if (mixerProtoIdMaybe is not { } protoId)
        {
            return null;
        }
        if (_audioMixers.TryGetValue(protoId, out var mixer))
        {
            return mixer;
        }
        if (!_prototypeManager.TryIndex(protoId, out var proto))
        {
            return null;
        }
        mixer = CreateMixer(mixerProtoIdMaybe);
        _audioMixers[protoId] = mixer;
        if (proto.Out == originProtoId)
        {
            Sawmill.Error($"Audio mixer prototype {originProtoId} has a circular output.");
        }
        else if (GetMixer(proto.Out, originProtoId) is { } outMixer)
        {
            mixer.SetOut(outMixer);
        }
        mixer.SelfGain = proto.Gain;
        SetMixerGainCVar(mixer, proto.GainCVar);
        return mixer;
    }
}
