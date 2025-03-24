using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using OpenTK.Audio.OpenAL;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Exceptions;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Replays;
using Robust.Shared.Threading;
using Robust.Shared.Utility;

namespace Robust.Client.Audio;

public sealed partial class AudioSystem : SharedAudioSystem
{
    /*
     * There's still a lot more OpenAL can do in terms of filters, auxiliary slots, etc.
     * but exposing the whole thing in an easy way is a lot of effort.
     */

    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IReplayRecordingManager _replayRecording = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IParallelManager _parMan = default!;
    [Dependency] private readonly IRuntimeLog _runtimeLog = default!;
    [Dependency] private readonly IAudioInternal _audio = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly SharedTransformSystem _xformSys = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    /// <summary>
    /// Per-tick cache of relevant streams.
    /// </summary>
    private readonly List<(EntityUid Entity, AudioComponent Component, TransformComponent Xform)> _streams = new();
    private UpdateAudioJob _updateAudioJob;

    private float _audioFrameTime;
    private float _audioFrameTimeRemaining;

    private EntityQuery<PhysicsComponent> _physicsQuery;

    private float _maxRayLength;
    private float _zOffset;
    private float _audioEndBuffer;

    public override float ZOffset
    {
        get => _zOffset;
        protected set
        {
            _zOffset = value;
            _audio.SetZOffset(value);

            var query = AllEntityQuery<AudioComponent>();

            while (query.MoveNext(out var audio))
            {
                // Pythagoras back to normal then adjust.
                var maxDistance = GetAudioDistance(audio.Params.MaxDistance);
                var refDistance = GetAudioDistance(audio.Params.ReferenceDistance);

                audio.MaxDistance = maxDistance;
                audio.ReferenceDistance = refDistance;
            }
        }
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        _updateAudioJob = new UpdateAudioJob
        {
            System = this,
            Streams = _streams,
        };

        UpdatesOutsidePrediction = true;
        // Need to run after Eye updates so we have an accurate listener position.
        UpdatesAfter.Add(typeof(EyeSystem));

        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        SubscribeLocalEvent<AudioComponent, ComponentStartup>(OnAudioStartup);
        SubscribeLocalEvent<AudioComponent, ComponentShutdown>(OnAudioShutdown);
        SubscribeLocalEvent<AudioComponent, EntityPausedEvent>(OnAudioPaused);
        SubscribeLocalEvent<AudioComponent, AfterAutoHandleStateEvent>(OnAudioState);

        // Replay stuff
        SubscribeNetworkEvent<PlayAudioGlobalMessage>(OnGlobalAudio);
        SubscribeNetworkEvent<PlayAudioEntityMessage>(OnEntityAudio);
        SubscribeNetworkEvent<PlayAudioPositionalMessage>(OnEntityCoordinates);

        Subs.CVar(CfgManager, CVars.AudioEndBuffer, OnAudioBuffer, true);
        Subs.CVar(CfgManager, CVars.AudioAttenuation, OnAudioAttenuation, true);
        Subs.CVar(CfgManager, CVars.AudioRaycastLength, OnRaycastLengthChanged, true);
        Subs.CVar(CfgManager, CVars.AudioTickRate, OnAudioTickRate, true);
        InitializeLimit();
    }

    private void OnAudioBuffer(float value)
    {
        _audioEndBuffer = value;
    }

    private void OnAudioTickRate(int obj)
    {
        _audioFrameTime = 1f / obj;
        _audioFrameTimeRemaining = MathF.Min(_audioFrameTimeRemaining, _audioFrameTime);
    }

    private void OnAudioState(Entity<AudioComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        var component = entity.Comp;

        if (component.LifeStage < ComponentLifeStage.Initialized)
            return;

        ApplyAudioParams(component.Params, component);
        component.Source.Global = component.Global;

        if (TryComp<AudioAuxiliaryComponent>(component.Auxiliary, out var auxComp))
        {
            component.Source.SetAuxiliary(auxComp.Auxiliary);
        }
        else
        {
            component.Source.SetAuxiliary(null);
        }

        switch (component.State)
        {
            case AudioState.Playing:
                component.StartPlaying();
                break;
            case AudioState.Paused:
                component.Pause();
                break;
            case AudioState.Stopped:
                component.StopPlaying();
                component.PlaybackPosition = 0f;
                return;
        }

        // If playback position changed then update it.
        var position = (float) ((entity.Comp.PauseTime ?? Timing.CurTime) - entity.Comp.AudioStart).TotalSeconds;
        var currentPosition = entity.Comp.Source.PlaybackPosition;
        var diff = Math.Abs(position - currentPosition);

        // Don't try to set the audio too far ahead.
        if (!string.IsNullOrEmpty(entity.Comp.FileName))
        {
            if (position > GetAudioLengthImpl(entity.Comp.FileName).TotalSeconds - _audioEndBuffer)
            {
                entity.Comp.StopPlaying();
                return;
            }
        }

        // If the difference is minor then we'll just keep playing it.
        if (diff > 0.1f)
        {
            entity.Comp.PlaybackPosition = position;
        }
    }

    /// <summary>
    /// Sets the volume for the entire game.
    /// </summary>
    public void SetMasterVolume(float value)
    {
        _audio.SetMasterGain(value);
    }

    private void OnAudioPaused(EntityUid uid, AudioComponent component, ref EntityPausedEvent args)
    {
        component.Pause();
    }

    protected override void OnAudioUnpaused(EntityUid uid, AudioComponent component, ref EntityUnpausedEvent args)
    {
        base.OnAudioUnpaused(uid, component, ref args);
        component.StartPlaying();
    }

    private void OnAudioStartup(EntityUid uid, AudioComponent component, ComponentStartup args)
    {
        if (!Timing.ApplyingState && !Timing.IsFirstTimePredicted)
        {
            return;
        }

        // Source has already been set
        if (component.Loaded)
        {
            return;
        }

        if (!TryGetAudio(component.FileName, out var audioResource))
        {
            Log.Error($"Error creating audio source for {audioResource}, can't find file {component.FileName}");
            return;
        }

        SetupSource((uid, component), audioResource);
        component.Loaded = true;
    }

    private void SetupSource(Entity<AudioComponent> entity, AudioResource audioResource, TimeSpan? length = null)
    {
        var component = entity.Comp;
        length ??= GetAudioLength(component.FileName);

        // If audio came into range then start playback at the correct position.
        var offset = ((entity.Comp.PauseTime ?? Timing.CurTime) - component.AudioStart).TotalSeconds;

        if (TryAudioLimit(component.FileName))
        {
            var newSource = _audio.CreateAudioSource(audioResource);

            if (newSource == null)
            {
                Log.Error($"Error creating audio source for {audioResource}");
                DebugTools.Assert(false);
            }
            else
            {
                component.Source = newSource;
            }
        }

        // Need to set all initial data for first frame.
        ApplyAudioParams(component.Params, component);
        component.Source.Global = component.Global;

        // Don't play until first frame so occlusion etc. are correct.
        component.Gain = 0f;

        // If the offset < buffer than just play it from the start.
        if (offset < AudioDespawnBuffer)
        {
            offset = 0;
        }
        // Not enough audio to play
        else if (offset > length.Value.TotalSeconds - _audioEndBuffer)
        {
            component.StopPlaying();
            return;
        }

        if (offset > 0)
        {
            component.PlaybackPosition = (float) offset;
        }
    }

    private void OnAudioShutdown(EntityUid uid, AudioComponent component, ComponentShutdown args)
    {
        // Breaks with prediction?
        component.Source.Dispose();

        RemoveAudioLimit(component.FileName);
    }

    private void OnAudioAttenuation(int obj)
    {
        _audio.SetAttenuation((Attenuation) obj);
    }

    private void OnRaycastLengthChanged(float value)
    {
        _maxRayLength = value;
    }

    public override void FrameUpdate(float frameTime)
    {
        _audioFrameTimeRemaining -= frameTime;

        if (_audioFrameTimeRemaining > 0f)
            return;

        // Clamp to 0 in case we have a really long frame.
        _audioFrameTimeRemaining = MathF.Max(0f, _audioFrameTime + _audioFrameTimeRemaining);
        var eye = _eyeManager.CurrentEye;
        var localEntity = _playerManager.LocalEntity;
        Vector2 listenerVelocity;

        if (localEntity != null)
            listenerVelocity = _physics.GetMapLinearVelocity(localEntity.Value);
        else
            listenerVelocity = Vector2.Zero;

        _audio.SetVelocity(listenerVelocity);
        _audio.SetRotation(eye.Rotation);
        _audio.SetPosition(eye.Position.Position);

        var ourPos = GetListenerCoordinates();

        var query = AllEntityQuery<AudioComponent, TransformComponent>();
        _streams.Clear();

        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            _streams.Add((uid, comp, xform));
        }

        try
        {
            _updateAudioJob.OurPosition = ourPos;
            _parMan.ProcessNow(_updateAudioJob, _streams.Count);
        }
        catch (Exception e)
        {
            Log.Error($"Caught exception while processing entity streams.");
            _runtimeLog.LogException(e, $"{nameof(AudioSystem)}.{nameof(FrameUpdate)}");
        }
    }

    public MapCoordinates GetListenerCoordinates()
    {
        return _eyeManager.CurrentEye.Position;
    }

    private void ProcessStream(EntityUid entity, AudioComponent component, TransformComponent xform, MapCoordinates listener)
    {
        // TODO:
        // I Originally tried to be fancier here but it caused audio issues so just trying
        // to replicate the old behaviour for now.
        if (!component.Started)
        {
            component.Started = true;
            component.StartPlaying();
        }

        // If it's global but on another map (that isn't nullspace) then stop playing it.
        if (component.Global)
        {
            if (xform.MapID != MapId.Nullspace && listener.MapId != xform.MapID)
            {
                component.Gain = 0f;
                return;
            }

            // Resume playing.
            component.Volume = component.Params.Volume;
            return;
        }

        // Non-global sounds, stop playing if on another map.
        // Not relevant to us.
        if (listener.MapId != xform.MapID)
        {
            component.Gain = 0f;
            return;
        }

        Vector2 worldPos;
        component.Volume = component.Params.Volume;

        // Handle grid audio differently by using grid position.
        if ((component.Flags & AudioFlags.GridAudio) != 0x0)
        {
            var parentUid = xform.ParentUid;
            worldPos = _maps.GetGridPosition(parentUid);
        }
        else
        {
            worldPos = _xformSys.GetWorldPosition(entity);
        }

        // Max distance check
        var delta = worldPos - listener.Position;
        var distance = delta.Length();

        // Out of range so just clip it for us.
        if (GetAudioDistance(distance) > component.MaxDistance)
        {
            // Still keeps the source playing, just with no volume.
            component.Gain = 0f;
            return;
        }

        if (distance > 0f && distance < 0.01f)
        {
            worldPos = listener.Position;
            delta = Vector2.Zero;
            distance = 0f;
        }

        // Update audio occlusion
        if ((component.Flags & AudioFlags.NoOcclusion) == AudioFlags.NoOcclusion)
        {
            component.Occlusion = 0f;
        }
        else
        {
            var occlusion = GetOcclusion(listener, delta, distance, entity);
            component.Occlusion = occlusion;
        }

        // Update audio positions.
        component.Position = worldPos;

        // Make race cars go NYYEEOOOOOMMMMM
        if (_physicsQuery.TryGetComponent(entity, out var physicsComp))
        {
            // This actually gets the tracked entity's xform & iterates up though the parents for the second time. Bit
            // inefficient.
            var velocity = _physics.GetMapLinearVelocity(entity, physicsComp, xform);
            component.Velocity = velocity;
        }
    }

    /// <summary>
    /// Gets the audio occlusion from the target audio entity to the listener's position.
    /// </summary>
    public float GetOcclusion(MapCoordinates listener, Vector2 delta, float distance, EntityUid? ignoredEnt = null)
    {
        float occlusion = 0;

        if (distance > 0.1)
        {
            var rayLength = MathF.Min(distance, _maxRayLength);
            var ray = new CollisionRay(listener.Position, delta / distance, OcclusionCollisionMask);
            occlusion = _physics.IntersectRayPenetration(listener.MapId, ray, rayLength, ignoredEnt);
        }

        return occlusion;
    }

    private bool TryGetAudio(ResolvedSoundSpecifier specifier, [NotNullWhen(true)] out AudioResource? audio)
    {
        var filename = GetAudioPath(specifier);
        if (_resourceCache.TryGetResource(new ResPath(filename), out audio))
            return true;

        Log.Error($"Server tried to play audio file {filename} which does not exist.");
        return false;
    }

    private bool TryGetAudio(string filename, [NotNullWhen(true)] out AudioResource? audio)
    {
        if (_resourceCache.TryGetResource(new ResPath(filename), out audio))
            return true;

        Log.Error($"Server tried to play audio file {filename} which does not exist.");
        return false;
    }

    private bool TryGetAudio(AudioStream stream, [NotNullWhen(true)] out AudioResource? audio)
    {
        if (_resourceCache.TryGetResource(stream, out audio))
            return true;

        Log.Error($"Server failed to play audio stream {stream.Title}.");
        return false;
    }

    public override (EntityUid Entity, AudioComponent Component)? PlayPvs(ResolvedSoundSpecifier? specifier, EntityCoordinates coordinates,
        AudioParams? audioParams = null)
    {
        return PlayStatic(specifier, Filter.Local(), coordinates, true, audioParams);
    }

    public override (EntityUid Entity, AudioComponent Component)? PlayPvs(ResolvedSoundSpecifier? specifier, EntityUid uid, AudioParams? audioParams = null)
    {
        return PlayEntity(specifier, Filter.Local(), uid, true, audioParams);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayPredicted(SoundSpecifier? sound, EntityUid source, EntityUid? user, AudioParams? audioParams = null)
    {
        if (Timing.IsFirstTimePredicted && sound != null)
            return PlayEntity(sound, Filter.Local(), source, false, audioParams);

        return null; // uhh Lets hope predicted audio never needs to somehow store the playing audio....
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayLocal(
        SoundSpecifier? sound,
        EntityUid source,
        EntityUid? soundInitiator,
        AudioParams? audioParams = null
    )
    {
        return PlayPredicted(sound, source, soundInitiator, audioParams);
    }

    public override (EntityUid Entity, AudioComponent Component)? PlayPredicted(SoundSpecifier? sound, EntityCoordinates coordinates, EntityUid? user, AudioParams? audioParams = null)
    {
        if (Timing.IsFirstTimePredicted && sound != null)
            return PlayStatic(sound, Filter.Local(), coordinates, false, audioParams);

        return null;
    }

    /// <summary>
    ///     Play an audio file globally, without position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="audioParams"></param>
    private (EntityUid Entity, AudioComponent Component)? PlayGlobal(ResolvedSoundSpecifier? specifier, AudioParams? audioParams = null, bool recordReplay = true)
    {
        if (specifier is null)
            return null;

        if (recordReplay && _replayRecording.IsRecording)
        {
            _replayRecording.RecordReplayMessage(new PlayAudioGlobalMessage
            {
                Specifier = specifier,
                AudioParams = audioParams ?? AudioParams.Default
            });
        }

        return TryGetAudio(specifier, out var audio) ? PlayGlobal(audio, specifier, audioParams) : default;
    }

    /// <summary>
    ///     Play an audio stream globally, without position.
    /// </summary>
    /// <param name="stream">The audio stream to play.</param>
    /// <param name="audioParams"></param>
    public (EntityUid Entity, AudioComponent Component)? PlayGlobal(AudioStream stream, ResolvedSoundSpecifier? specifier, AudioParams? audioParams = null)
    {
        var (entity, component) = CreateAndStartPlayingStream(audioParams, specifier, stream);
        component.Global = true;
        component.Source.Global = true;
        DirtyField(entity, component, nameof(AudioComponent.Global));
        return (entity, component);
    }

    /// <summary>
    ///     Play an audio file following an entity.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="entity">The entity "emitting" the audio.</param>
    private (EntityUid Entity, AudioComponent Component)? PlayEntity(ResolvedSoundSpecifier? specifier, EntityUid entity, AudioParams? audioParams = null, bool recordReplay = true)
    {
        if (specifier is null)
            return null;

        if (recordReplay && _replayRecording.IsRecording)
        {
            _replayRecording.RecordReplayMessage(new PlayAudioEntityMessage
            {
                Specifier = specifier,
                NetEntity = GetNetEntity(entity),
                AudioParams = audioParams ?? AudioParams.Default
            });
        }

        return TryGetAudio(specifier, out var audio) ? PlayEntity(audio, entity, specifier, audioParams) : default;
    }

    /// <summary>
    ///     Play an audio stream following an entity.
    /// </summary>
    /// <param name="stream">The audio stream to play.</param>
    /// <param name="entity">The entity "emitting" the audio.</param>
    /// <param name="audioParams"></param>
    public (EntityUid Entity, AudioComponent Component)? PlayEntity(AudioStream stream, EntityUid entity, ResolvedSoundSpecifier? specifier, AudioParams? audioParams = null)
    {
        if (TerminatingOrDeleted(entity))
        {
            Log.Error($"Tried to play coordinates audio on a terminating / deleted entity {ToPrettyString(entity)}");
            return null;
        }

        var playing = CreateAndStartPlayingStream(audioParams, specifier, stream);
        _xformSys.SetCoordinates(playing.Entity, new EntityCoordinates(entity, Vector2.Zero));

        return playing;
    }

    /// <summary>
    ///     Play an audio file at a static position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    /// <param name="audioParams"></param>
    private (EntityUid Entity, AudioComponent Component)? PlayStatic(ResolvedSoundSpecifier? specifier, EntityCoordinates coordinates, AudioParams? audioParams = null, bool recordReplay = true)
    {
        if (specifier is null)
            return null;

        if (recordReplay && _replayRecording.IsRecording)
        {
            _replayRecording.RecordReplayMessage(new PlayAudioPositionalMessage
            {
                Specifier = specifier,
                Coordinates = GetNetCoordinates(coordinates),
                AudioParams = audioParams ?? AudioParams.Default
            });
        }

        return TryGetAudio(specifier, out var audio) ? PlayStatic(audio, coordinates, specifier, audioParams) : default;
    }

    /// <summary>
    ///     Play an audio stream at a static position.
    /// </summary>
    /// <param name="stream">The audio stream to play.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    /// <param name="audioParams"></param>
    public (EntityUid Entity, AudioComponent Component)? PlayStatic(AudioStream stream, EntityCoordinates coordinates, ResolvedSoundSpecifier? specifier, AudioParams? audioParams = null)
    {
        if (TerminatingOrDeleted(coordinates.EntityId))
        {
            Log.Error($"Tried to play coordinates audio on a terminating / deleted entity {ToPrettyString(coordinates.EntityId)}");
            return null;
        }

        var playing = CreateAndStartPlayingStream(audioParams, specifier, stream);
        _xformSys.SetCoordinates(playing.Entity, coordinates);
        return playing;
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayGlobal(ResolvedSoundSpecifier? specifier, Filter playerFilter, bool recordReplay, AudioParams? audioParams = null)
    {
        return PlayGlobal(specifier, audioParams);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayEntity(ResolvedSoundSpecifier? specifier, Filter playerFilter, EntityUid entity, bool recordReplay, AudioParams? audioParams = null)
    {
        return PlayEntity(specifier, entity, audioParams);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayStatic(ResolvedSoundSpecifier? specifier, Filter playerFilter, EntityCoordinates coordinates, bool recordReplay, AudioParams? audioParams = null)
    {
        return PlayStatic(specifier, coordinates, audioParams);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayGlobal(ResolvedSoundSpecifier? specifier, ICommonSession recipient, AudioParams? audioParams = null)
    {
        return PlayGlobal(specifier, audioParams);
    }

    public override void LoadStream<T>(Entity<AudioComponent> entity, T stream)
    {
        if (stream is AudioStream audioStream)
        {
            TryGetAudio(audioStream, out var audio);
            SetupSource(entity, audio!, audioStream.Length);
            entity.Comp.Loaded = true;
        }
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayGlobal(ResolvedSoundSpecifier? specifier, EntityUid recipient, AudioParams? audioParams = null)
    {
        return PlayGlobal(specifier, audioParams);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayEntity(ResolvedSoundSpecifier? specifier, ICommonSession recipient, EntityUid uid, AudioParams? audioParams = null)
    {
        return PlayEntity(specifier, uid, audioParams);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayEntity(ResolvedSoundSpecifier? specifier, EntityUid recipient, EntityUid uid, AudioParams? audioParams = null)
    {
        return PlayEntity(specifier, uid, audioParams);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayStatic(ResolvedSoundSpecifier? specifier, ICommonSession recipient, EntityCoordinates coordinates, AudioParams? audioParams = null)
    {
        return PlayStatic(specifier, coordinates, audioParams);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayStatic(ResolvedSoundSpecifier? specifier, EntityUid recipient, EntityCoordinates coordinates, AudioParams? audioParams = null)
    {
        return PlayStatic(specifier, coordinates, audioParams);
    }

    private (EntityUid Entity, AudioComponent Component) CreateAndStartPlayingStream(AudioParams? audioParams, ResolvedSoundSpecifier? specifier, AudioStream stream)
    {
        var audioP = audioParams ?? AudioParams.Default;
        var entity = SetupAudio(specifier, audioP, initialize: false, length: stream.Length);
        LoadStream(entity, stream);
        EntityManager.InitializeAndStartEntity(entity);
        var comp = entity.Comp;
        var source = comp.Source;

        // TODO clamp the offset inside of SetPlaybackPosition() itself.
        var offset = audioP.PlayOffsetSeconds;
        var maxOffset = Math.Max((float) stream.Length.TotalSeconds - 0.01f, 0f);
        offset = Math.Clamp(offset, 0f, maxOffset);
        source.PlaybackPosition = offset;

        // For server we will rely on the adjusted one but locally we will have to adjust it ourselves.
        ApplyAudioParams(comp.Params, comp);
        source.StartPlaying();
        return (entity, comp);
    }

    /// <summary>
    /// Applies the audioparams to the underlying audio source.
    /// </summary>
    private void ApplyAudioParams(AudioParams audioParams, IAudioSource source)
    {
        source.Pitch = audioParams.Pitch;
        source.Volume = audioParams.Volume;
        source.RolloffFactor = audioParams.RolloffFactor;
        source.MaxDistance = GetAudioDistance(audioParams.MaxDistance);
        source.ReferenceDistance = GetAudioDistance(audioParams.ReferenceDistance);
        source.Looping = audioParams.Loop;
    }

    private void OnEntityCoordinates(PlayAudioPositionalMessage ev)
    {
        PlayStatic(ev.Specifier, GetCoordinates(ev.Coordinates), ev.AudioParams, false);
    }

    private void OnEntityAudio(PlayAudioEntityMessage ev)
    {
        PlayEntity(ev.Specifier, GetEntity(ev.NetEntity), ev.AudioParams, false);
    }

    private void OnGlobalAudio(PlayAudioGlobalMessage ev)
    {
        PlayGlobal(ev.Specifier, ev.AudioParams, false);
    }

    protected override TimeSpan GetAudioLengthImpl(string filename)
    {
        return _resourceCache.GetResource<AudioResource>(filename).AudioStream.Length;
    }

    #region Jobs

    private record struct UpdateAudioJob : IParallelRobustJob
    {
        public int BatchSize => 2;

        public AudioSystem System;

        public MapCoordinates OurPosition;
        public List<(EntityUid Entity, AudioComponent Component, TransformComponent Xform)> Streams;

        public void Execute(int index)
        {
            var comp = Streams[index];

            System.ProcessStream(comp.Entity, comp.Component, comp.Xform, OurPosition);
        }
    }

    #endregion
}
