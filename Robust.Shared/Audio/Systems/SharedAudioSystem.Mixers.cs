using System;
using System.Collections.Generic;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Mixers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Robust.Shared.Audio.Systems;

public abstract partial class SharedAudioSystem
{
    /// <summary>
    /// Mixer prototype id that will be used on audio sources without specified mixer.
    /// </summary>
    public ProtoId<AudioMixerPrototype>? DefaultMixer { get; set; }

    private readonly Dictionary<ProtoId<AudioMixerPrototype>, Entity<AudioMixerComponent>> _audioMixers = new();
    private bool _isMixersStarted;

    protected virtual void InitializeMixers()
    {
        SubscribeLocalEvent<AudioMixerComponent, ComponentShutdown>(OnMixerShutdown);
        SubscribeLocalEvent<AudioMixerComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<AudioMixerComponent, ComponentHandleState>(OnHandleState);
        EntityManager.AfterEntityFlush += LoadPrototypedMixers;
    }

    protected virtual void UpdateMixers()
    {
        // Ahhhh, I find this the best way to know when game is ready to spawn entities on startup.
        if (!_isMixersStarted)
        {
            StartMixers();
        }
    }

    private void StartMixers()
    {
        _isMixersStarted = true;
        LoadPrototypedMixers();
    }

    /// <summary>
    /// Creates audio mixer entity wrapper from raw <see cref="IAudioMixer"/>.
    /// </summary>
    public virtual Entity<AudioMixerComponent> CreateMixerEntity(IAudioMixer mixer)
    {
        var ent = Spawn(null, MapCoordinates.Nullspace);
        var comp = AddComp<AudioMixerComponent>(ent);
        comp.Mixer = mixer;
        return (ent, comp);
    }

    /// <summary>
    /// Creates audio mixer.
    /// </summary>
    /// <param name="outMixer">Mixer to set as out for created mixer.</param>
    public virtual Entity<AudioMixerComponent> CreateMixer(Entity<AudioMixerComponent>? outMixer)
    {
        var ent = Spawn(null, MapCoordinates.Nullspace);
        var comp = AddComp<AudioMixerComponent>(ent);
        SetMixerOut((ent, comp), outMixer);
        return (ent, comp);
    }

    /// <summary>
    /// Assigns audio mixer to specified audio source.
    /// </summary>
    public void SetMixer(Entity<AudioComponent> audio, Entity<AudioMixerComponent>? mixerOrNone)
    {
        if (mixerOrNone is { } mixer)
        {
            SetMixer(audio, mixer);
        }
        else
        {
            ClearMixer(audio);
        }
    }

    /// <summary>
    /// Assigns audio mixer to specified audio source.
    /// </summary>
    public virtual void SetMixer(Entity<AudioComponent> audio, Entity<AudioMixerComponent> mixer)
    {
        audio.Comp.Mixer = mixer;
        audio.Comp.Params.MixerProto = mixer.Comp.ProtoId;
        audio.Comp.Source.SetMixer(mixer.Comp.Mixer);
        Dirty(audio);
    }

    /// <summary>
    /// Clears audio mixer from specified audio source.
    /// </summary>
    public virtual void ClearMixer(Entity<AudioComponent> audio)
    {
        audio.Comp.Mixer = null;
        audio.Comp.Params.MixerProto = null;
        audio.Comp.Source.SetMixer(null);
        Dirty(audio);
    }

    /// <summary>
    /// Returns audio mixer associated with provided mixer prototype id.
    /// </summary>
    public Entity<AudioMixerComponent>? GetMixer(ProtoId<AudioMixerPrototype>? mixerProtoId)
    {
        return mixerProtoId.HasValue ? GetMixer(mixerProtoId.Value) : null;
    }

    /// <summary>
    /// Returns audio mixer associated with provided mixer prototype id.
    /// </summary>
    public virtual Entity<AudioMixerComponent>? GetMixer(ProtoId<AudioMixerPrototype> mixerProtoId)
    {
        return _audioMixers.TryGetValue(mixerProtoId, out var mixer) ? mixer : null;
    }

    /// <summary>
    /// Set gain value to the audio mixer.
    /// </summary>
    public virtual void SetMixerGain(Entity<AudioMixerComponent> mixer, float gain)
    {
        gain = Math.Max(gain, 0);
        mixer.Comp.SelfGain = gain;
        if (mixer.Comp.IsGainSynced)
            Dirty(mixer);
    }

    /// <summary>
    /// Assigns CVar to store mixer gain value. Pass <see langword="null"/> to clear.
    /// </summary>
    public virtual void SetMixerGainCVar(Entity<AudioMixerComponent> mixer, string? name)
    {
        mixer.Comp.GainCVar = name;
        Dirty(mixer);
    }

    /// <summary>
    /// Set specified mixer as an output for this mixer, pass <see langword="null"/> to set as root mixer.
    /// </summary>
    public virtual void SetMixerOut(Entity<AudioMixerComponent> mixer, Entity<AudioMixerComponent>? outMixerOrNone)
    {
        if (outMixerOrNone is { } outMixer && !TerminatingOrDeleted(outMixer))
        {
            mixer.Comp.OutEntity = outMixer;
            mixer.Comp.Mixer.SetOut(outMixer.Comp.Mixer);
        }
        else
        {
            mixer.Comp.OutEntity = null;
            mixer.Comp.Mixer.SetOut(null);
        }
        Dirty(mixer);
    }

    protected virtual Entity<AudioMixerComponent> SpawnMixerForPrototype(ProtoId<AudioMixerPrototype> mixerProtoId)
    {
        var mixer = CreateMixer(null);
        mixer.Comp.ProtoId = mixerProtoId;
        return mixer;
    }

    protected void ApplyAudioParamsMixer(Entity<AudioComponent> audio, AudioParams audioParams)
    {
        SetMixer(audio, GetMixer(audioParams.MixerProto));
    }

    protected virtual void OnMixerShutdown(Entity<AudioMixerComponent> mixer, ref ComponentShutdown args)
    {
        // It is too hard to store all the subscribers to unsubscribe here, so we do this
        var query = AllEntityQuery<AudioComponent>();
        while (query.MoveNext(out var audio))
        {
            if (audio.Mixer != mixer.Owner)
                continue;
            audio.Mixer = null;
            audio.Params.MixerProto = null;
        }
    }

    private void OnGetState(Entity<AudioMixerComponent> mixer, ref ComponentGetState args)
    {
        args.State = new AudioMixerComponentState
        {
            OutEntity = GetNetEntity(mixer.Comp.OutEntity),
            ProtoId = mixer.Comp.ProtoId,
            IsGainSynced = mixer.Comp.IsGainSynced,
            SelfGain = mixer.Comp.SelfGain,
            GainCVar = mixer.Comp.GainCVar,
        };
    }

    protected virtual void OnHandleState(Entity<AudioMixerComponent> mixer, ref ComponentHandleState args)
    {
        if (args.Current is not AudioMixerComponentState state)
            return;

        mixer.Comp.OutEntity = EnsureEntity<AudioMixerComponent>(state.OutEntity, mixer);
        mixer.Comp.ProtoId = state.ProtoId;
        mixer.Comp.IsGainSynced = state.IsGainSynced;
        if (mixer.Comp.IsGainSynced || !mixer.Comp.IsInitiallySynced)
            mixer.Comp.SelfGain = state.SelfGain;
        mixer.Comp.GainCVar = state.GainCVar;

        mixer.Comp.IsInitiallySynced = true;
    }

    private void LoadPrototypedMixers()
    {
        if (EntityManager.ShuttingDown)
        {
            return;
        }
        // Initialization
        foreach (var proto in ProtoMan.EnumeratePrototypes<AudioMixerPrototype>())
        {
            var mixer = SpawnMixerForPrototype(proto.ID);
            _audioMixers[proto.ID] = mixer;
            SetMixerGain(mixer, proto.Gain);
            SetMixerGainCVar(mixer, proto.GainCVar);
        }
        // Out setup
        foreach (var proto in ProtoMan.EnumeratePrototypes<AudioMixerPrototype>())
        {
            if (proto.Out is { } outId)
            {
                SetMixerOut(_audioMixers[proto.ID], _audioMixers.TryGetValue(outId, out var mixer) ? mixer : null);
            }
        }
    }
}
