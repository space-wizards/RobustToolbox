using Robust.Client.Audio.Mixers;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Mixers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Robust.Client.Audio;

public sealed partial class AudioSystem
{
    [Dependency] private readonly IAudioMixersManager _audioMixersManager = default!;

    protected override void InitializeMixers()
    {
        base.InitializeMixers();

        SubscribeLocalEvent<AudioMixerComponent, ComponentAdd>(OnMixerAdd);
    }

    public override Entity<AudioMixerComponent> CreateMixer(Entity<AudioMixerComponent>? outMixer)
    {
        var mixerEntity = base.CreateMixer(outMixer);
        if (outMixer is { } outMixerValue)
        {
            mixerEntity.Comp.Mixer.SetOut(outMixerValue.Comp.Mixer);
        }
        return mixerEntity;
    }

    public override void SetMixerGain(Entity<AudioMixerComponent> mixer, float gain)
    {
        base.SetMixerGain(mixer, gain);
        if (mixer.Comp.Mixer.GainCVar is { } cvar)
        {
            CfgManager.SetCVar(cvar, gain);
        }
        else
        {
            mixer.Comp.Mixer.SelfGain = gain;
        }
    }

    public override void SetMixerGainCVar(Entity<AudioMixerComponent> mixer, string? name)
    {
        base.SetMixerGainCVar(mixer, name);
        _audioMixersManager.SetMixerGainCVar(mixer.Comp.Mixer, name);
    }

    protected override Entity<AudioMixerComponent> SpawnMixerForPrototype(ProtoId<AudioMixerPrototype> mixerProtoId)
    {
        var mixer = base.SpawnMixerForPrototype(mixerProtoId);
        mixer.Comp.Mixer = _audioMixersManager.GetMixer(mixerProtoId) ?? mixer.Comp.Mixer;
        return mixer;
    }

    private void OnMixerAdd(Entity<AudioMixerComponent> mixer, ref ComponentAdd args)
    {
        mixer.Comp.Mixer = _audioMixersManager.CreateMixer();
    }

    protected override void OnMixerShutdown(Entity<AudioMixerComponent> mixer, ref ComponentShutdown args)
    {
        base.OnMixerShutdown(mixer, ref args);
        DisposeMixer(mixer.Comp.Mixer);
    }

    protected override void OnHandleState(Entity<AudioMixerComponent> mixer, ref ComponentHandleState args)
    {
        base.OnHandleState(mixer, ref args);

        if (mixer.Comp.ProtoId is { } protoId
            && protoId != mixer.Comp.Mixer.ProtoId
            && _audioMixersManager.GetMixer(protoId) is { } newMixer)
        {
            DisposeMixer(mixer.Comp.Mixer);
            mixer.Comp.Mixer = newMixer;
        }
        SetMixerGainCVar(mixer, mixer.Comp.GainCVar);
        if (mixer.Comp.ProtoId is null)
        {
            Entity<AudioMixerComponent>? outMixer = mixer.Comp.OutEntity is { } outMixerOwner
                && TryComp<AudioMixerComponent>(outMixerOwner, out var outMixerComponent)
                ? new Entity<AudioMixerComponent>(outMixerOwner, outMixerComponent) : null;
            SetMixerOut(mixer, outMixer);
        }
    }

    private void DisposeMixer(IAudioMixer mixer)
    {
        // We don't want to dispose mixers from prototypes cos they are supposed to be re-used.
        if (mixer.ProtoId is { })
            return;
        mixer.Dispose();
    }
}
