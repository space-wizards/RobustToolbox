using Robust.Shared.Audio.Mixers;
using Robust.Shared.Prototypes;

namespace Robust.Client.Audio.Mixers;

/// <summary>
/// Public API to manipulate on raw <see cref="IAudioMixer"/> objects.
/// </summary>
public interface IAudioMixersManager
{
    IAudioMixer CreateMixer();
    IAudioMixer? GetMixer(ProtoId<AudioMixerPrototype>? mixerProtoIdMaybe);
    void SetMixerGainCVar(IAudioMixer mixer, string? name);
}
