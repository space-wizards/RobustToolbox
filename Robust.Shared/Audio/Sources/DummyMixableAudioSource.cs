using Robust.Shared.Audio.Mixers;

namespace Robust.Shared.Audio.Sources;

internal sealed class DummyMixableAudioSource : DummyAudioSource, IMixableAudioSource
{
    public static new DummyMixableAudioSource Instance { get; } = new();

    public void SetMixer(IAudioMixer? mixer)
    {
    }
}
