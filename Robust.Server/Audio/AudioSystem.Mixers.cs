using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Mixers;
using Robust.Shared.GameObjects;

namespace Robust.Server.Audio;

public partial class AudioSystem
{
    public override Entity<AudioMixerComponent> CreateMixerEntity(IAudioMixer mixer)
    {
        var mixerEntity = base.CreateMixerEntity(mixer);
        _pvs.AddGlobalOverride(mixerEntity);
        return mixerEntity;
    }

    public override Entity<AudioMixerComponent> CreateMixer(Entity<AudioMixerComponent>? outMixer)
    {
        var mixerEntity = base.CreateMixer(outMixer);
        _pvs.AddGlobalOverride(mixerEntity);
        return mixerEntity;
    }
}
