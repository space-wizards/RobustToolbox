using Robust.Shared.Audio.Mixers;

namespace Robust.Shared.Audio.Sources;

/// <summary>
/// <see cref="IAudioSource"/> with support for <see cref="IAudioMixer"/>.
/// </summary>
public interface IMixableAudioSource : IAudioSource
{
    void SetMixer(IAudioMixer? mixer);
}
