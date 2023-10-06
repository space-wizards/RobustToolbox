using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Threading.Tasks;
using OpenTK.Audio.OpenAL;
using OpenTK.Audio.OpenAL.Extensions.Creative.EFX;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Exceptions;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Random;
using Robust.Shared.Replays;
using Robust.Shared.ResourceManagement.ResourceTypes;
using Robust.Shared.Spawners;
using Robust.Shared.Threading;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects;

public sealed class AudioSystem : SharedAudioSystem
{
    [Dependency] private readonly IReplayRecordingManager _replayRecording = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IClientResourceCache _resourceCache = default!;
    [Dependency] private readonly IParallelManager _parMan = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRuntimeLog _runtimeLog = default!;
    [Dependency] private readonly IAudioInternal _audio = default!;
    [Dependency] private readonly SharedTransformSystem _xformSys = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    /// <summary>
    /// Per-tick cache of relevant streams.
    /// </summary>
    private readonly List<(EntityUid Entity, AudioComponent Component, TransformComponent Xform)> _streams = new();

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<TimedDespawnComponent> _despawnQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    private float _maxRayLength;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;
        // Need to run after Eye updates so we have an accurate listener position.
        UpdatesAfter.Add(typeof(EyeSystem));

        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _despawnQuery = GetEntityQuery<TimedDespawnComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<AudioComponent, ComponentStartup>(OnAudioStartup);
        SubscribeLocalEvent<AudioComponent, ComponentShutdown>(OnAudioShutdown);
        SubscribeLocalEvent<AudioComponent, EntityPausedEvent>(OnAudioPaused);
        SubscribeLocalEvent<AudioComponent, EntityUnpausedEvent>(OnAudioUnpaused);
        SubscribeLocalEvent<AudioComponent, AfterAutoHandleStateEvent>(OnAudioState);

        CfgManager.OnValueChanged(CVars.AudioAttenuation, OnAudioAttenuation, true);
        CfgManager.OnValueChanged(CVars.AudioRaycastLength, OnRaycastLengthChanged, true);
    }

    private void OnAudioState(EntityUid uid, AudioComponent component, ref AfterAutoHandleStateEvent args)
    {
        ApplyAudioParams(component.Params, component);
        component.Source.Global = component.Global;
    }

    /// <summary>
    /// Sets the volume for the entire game.
    /// </summary>
    public void SetMasterVolume(float value)
    {
        _audio.SetMasterVolume(value);
    }

    protected override void SetZOffset(float value)
    {
        base.SetZOffset(value);
        _audio.SetZOffset(value);
    }

    public override void Shutdown()
    {
        CfgManager.UnsubValueChanged(CVars.AudioAttenuation, OnAudioAttenuation);
        CfgManager.UnsubValueChanged(CVars.AudioRaycastLength, OnRaycastLengthChanged);
        base.Shutdown();
    }

    public void CreateEffect()
    {
        var slot = EFX.GenEffect();
        var aux = EFX.GenAuxiliaryEffectSlot();
    }

    private void OnAudioPaused(EntityUid uid, AudioComponent component, ref EntityPausedEvent args)
    {
        component.Pause();
    }

    private void OnAudioUnpaused(EntityUid uid, AudioComponent component, ref EntityUnpausedEvent args)
    {
        component.StartPlaying();
    }

    private void OnAudioStartup(EntityUid uid, AudioComponent component, ComponentStartup args)
    {
        if ((!Timing.ApplyingState && !Timing.IsFirstTimePredicted) || !TryGetAudio(component.FileName, out var audioResource))
            return;

        var source = _audio.CreateAudioSource(audioResource);

        if (source == null)
        {
            Log.Error($"Error creating audio source for {audioResource}");
            DebugTools.Assert(false);
            source = new DummyAudioSource();
        }

        // Need to set all initial data for first frame.
        component.Source = source;
        ApplyAudioParams(component.Params, component);
        component.Source.Global = component.Global;
        // Start playing it first frame as that handles audio properly.
    }

    private void OnAudioShutdown(EntityUid uid, AudioComponent component, ComponentShutdown args)
    {
        // Breaks with prediction?
        component.Source.Dispose();
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
        var eye = _eyeManager.CurrentEye;
        _audio.SetRotation(eye.Rotation);
        _audio.SetPosition(eye.Position.Position);

        var ourPos = eye.Position;
        var opts = new ParallelOptions { MaxDegreeOfParallelism = _parMan.ParallelProcessCount };

        var query = AllEntityQuery<AudioComponent, TransformComponent>();
        _streams.Clear();

        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            _streams.Add((uid, comp, xform));
        }

        try
        {
            Parallel.ForEach(_streams, opts, comp => ProcessStream(comp.Entity, comp.Component, comp.Xform, ourPos));
        }
        catch (Exception e)
        {
            Log.Error($"Caught exception while processing entity streams.");
            _runtimeLog.LogException(e, $"{nameof(AudioSystem)}.{nameof(FrameUpdate)}");
        }
    }

    private bool ResumeStream(EntityUid entity, AudioComponent component, float timeRemaining)
    {
        // This exists so if we turn-off / turn-on audio we don't just accidentally replay it on the final tick and clip it.
        // We should be able to go between maps and just have audio resume where it began cleanly ideally.

        if (component.Playing)
            return true;

        if (timeRemaining < 0.1f)
            return false;

        component.Playing = true;

        if (!timeRemaining.Equals(float.MaxValue))
            component.Source.PlaybackPosition = (float) (GetAudioLength(component.FileName).TotalSeconds - timeRemaining);

        return true;
    }

    private void ProcessStream(EntityUid entity, AudioComponent component, TransformComponent xform, MapCoordinates listener)
    {
        float timeRemaining = float.MaxValue;
        if (_despawnQuery.TryGetComponent(entity, out var despawn))
        {
            timeRemaining = despawn.Lifetime;
        }

        // If it's global but on another map (that isn't nullspace) then stop playing it.
        if (component.Global)
        {
            if (xform.MapID != MapId.Nullspace && listener.MapId != xform.MapID)
            {
                component.StopPlaying();
                return;
            }

            // Resume playing.
            ResumeStream(entity, component, timeRemaining);
            return;
        }

        // Non-global sounds, stop playing if on another map.
        // Not relevant to us.
        if (listener.MapId != xform.MapID)
        {
            component.StopPlaying();
            return;
        }

        if (!ResumeStream(entity, component, timeRemaining))
            return;

        var mapPos = xform.MapPosition;

        // Max distance check
        var delta = mapPos.Position - listener.Position;
        var distance = delta.Length();

        // Out of range so just clip it for us.
        if (distance > component.MaxDistance)
        {
            // Still keeps the source playing, just with no volume.
            component.Source.Gain = 0f;
            return;
        }

        // Update audio occlusion
        var occlusion = GetOcclusion(entity, listener, delta, distance);
        component.Occlusion = occlusion;

        // Update audio positions.
        component.Position = mapPos.Position;

        // Make race cars go NYYEEOOOOOMMMMM
        if (_physicsQuery.TryGetComponent(entity, out var physicsComp))
        {
            // This actually gets the tracked entity's xform & iterates up though the parents for the second time. Bit
            // inefficient.
            var velocity = _physics.GetMapLinearVelocity(entity, physicsComp, xform, _xformQuery, _physicsQuery);
            component.Velocity = velocity;
        }
    }

    internal float GetOcclusion(EntityUid entity, MapCoordinates listener, Vector2 delta, float distance)
    {
        float occlusion = 0;

        if (distance > 0.1)
        {
            var rayLength = MathF.Min(distance, _maxRayLength);
            var ray = new CollisionRay(listener.Position, delta / distance, OcclusionCollisionMask);
            occlusion = _physics.IntersectRayPenetration(listener.MapId, ray, rayLength, entity);
        }

        return occlusion;
    }

    private bool TryGetAudio(string filename, [NotNullWhen(true)] out AudioResource? audio)
    {
        if (_resourceCache.TryGetResource(new ResPath(filename), out audio))
            return true;

        Log.Error($"Server tried to play audio file {filename} which does not exist.");
        return false;
    }

    private bool TryCreateAudioSource(AudioStream stream, [NotNullWhen(true)] out IAudioSource? source)
    {
        if (!Timing.IsFirstTimePredicted)
        {
            source = null;
            Log.Error($"Tried to create audio source outside of prediction!");
            DebugTools.Assert(false);
            return false;
        }

        source = _audio.CreateAudioSource(stream);
        return source != null;
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayPredicted(SoundSpecifier? sound, EntityUid source, EntityUid? user, AudioParams? audioParams = null)
    {
        if (Timing.IsFirstTimePredicted || sound == null)
            return PlayEntity(sound, Filter.Local(), source, false, audioParams);

        return null; // uhh Lets hope predicted audio never needs to somehow store the playing audio....
    }

    public override (EntityUid Entity, AudioComponent Component)? PlayPredicted(SoundSpecifier? sound, EntityCoordinates coordinates, EntityUid? user, AudioParams? audioParams = null)
    {
        if (Timing.IsFirstTimePredicted || sound == null)
            return PlayStatic(sound, Filter.Local(), coordinates, false, audioParams);

        return null;
    }

    /// <summary>
    ///     Play an audio file globally, without position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="audioParams"></param>
    private (EntityUid Entity, AudioComponent Component)? PlayGlobal(string filename, AudioParams? audioParams = null, bool recordReplay = true)
    {
        /* left here just in case uhh yeah idk how replays handle clientside entity spawns.
        if (recordReplay && _replayRecording.IsRecording)
        {
            _replayRecording.RecordReplayMessage(new PlayAudioGlobalMessage
            {
                FileName = filename,
                AudioParams = audioParams ?? AudioParams.Default
            });
        }
        */

        return TryGetAudio(filename, out var audio) ? PlayGlobal(audio, audioParams) : default;
    }

    /// <summary>
    ///     Play an audio stream globally, without position.
    /// </summary>
    /// <param name="stream">The audio stream to play.</param>
    /// <param name="audioParams"></param>
    private (EntityUid Entity, AudioComponent Component)? PlayGlobal(AudioStream stream, AudioParams? audioParams = null)
    {
        var (entity, component) = CreateAndStartPlayingStream(audioParams, stream);
        component.Global = true;
        component.Source.Global = true;
        Dirty(entity, component);
        return (entity, component);
    }

    /// <summary>
    ///     Play an audio file following an entity.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="entity">The entity "emitting" the audio.</param>
    private (EntityUid Entity, AudioComponent Component)? PlayEntity(string filename, EntityUid entity, AudioParams? audioParams = null, bool recordReplay = true)
    {
        /*
        if (recordReplay && _replayRecording.IsRecording)
        {
            _replayRecording.RecordReplayMessage(new PlayAudioEntityMessage
            {
                FileName = filename,
                NetEntity = GetNetEntity(entity),
                AudioParams = audioParams ?? AudioParams.Default
            });
        }
        */

        return TryGetAudio(filename, out var audio) ? PlayEntity(audio, entity, audioParams) : default;
    }

    /// <summary>
    ///     Play an audio stream following an entity.
    /// </summary>
    /// <param name="stream">The audio stream to play.</param>
    /// <param name="entity">The entity "emitting" the audio.</param>
    /// <param name="audioParams"></param>
    private (EntityUid Entity, AudioComponent Component)? PlayEntity(AudioStream stream, EntityUid entity, AudioParams? audioParams = null)
    {
        var playing = CreateAndStartPlayingStream(audioParams, stream);
        _xformSys.SetCoordinates(playing.Entity, new EntityCoordinates(entity, Vector2.Zero));

        return playing;
    }

    /// <summary>
    ///     Play an audio file at a static position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    /// <param name="audioParams"></param>
    private (EntityUid Entity, AudioComponent Component)? PlayStatic(string filename, EntityCoordinates coordinates, AudioParams? audioParams = null, bool recordReplay = true)
    {
        /*
        if (recordReplay && _replayRecording.IsRecording)
        {
            _replayRecording.RecordReplayMessage(new PlayAudioPositionalMessage
            {
                FileName = filename,
                Coordinates = GetNetCoordinates(coordinates),
                AudioParams = audioParams ?? AudioParams.Default
            });
        }
        */

        return TryGetAudio(filename, out var audio) ? PlayStatic(audio, coordinates, audioParams) : default;
    }

    /// <summary>
    ///     Play an audio stream at a static position.
    /// </summary>
    /// <param name="stream">The audio stream to play.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    /// <param name="audioParams"></param>
    private (EntityUid Entity, AudioComponent Component)? PlayStatic(AudioStream stream, EntityCoordinates coordinates, AudioParams? audioParams = null)
    {
        var playing = CreateAndStartPlayingStream(audioParams, stream);
        _xformSys.SetCoordinates(playing.Entity, coordinates);
        return playing;
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayGlobal(string filename, Filter playerFilter, bool recordReplay, AudioParams? audioParams = null)
    {
        return PlayGlobal(filename, audioParams);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayEntity(string filename, Filter playerFilter, EntityUid entity, bool recordReplay, AudioParams? audioParams = null)
    {
        return PlayEntity(filename, entity, audioParams);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayStatic(string filename, Filter playerFilter, EntityCoordinates coordinates, bool recordReplay, AudioParams? audioParams = null)
    {
        return PlayStatic(filename, coordinates, audioParams);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayGlobal(string filename, ICommonSession recipient, AudioParams? audioParams = null)
    {
        return PlayGlobal(filename, audioParams);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayGlobal(string filename, EntityUid recipient, AudioParams? audioParams = null)
    {
        return PlayGlobal(filename, audioParams);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayEntity(string filename, ICommonSession recipient, EntityUid uid, AudioParams? audioParams = null)
    {
        return PlayEntity(filename, uid, audioParams);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayEntity(string filename, EntityUid recipient, EntityUid uid, AudioParams? audioParams = null)
    {
        return PlayEntity(filename, uid, audioParams);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayStatic(string filename, ICommonSession recipient, EntityCoordinates coordinates, AudioParams? audioParams = null)
    {
        return PlayStatic(filename, coordinates, audioParams);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayStatic(string filename, EntityUid recipient, EntityCoordinates coordinates, AudioParams? audioParams = null)
    {
        return PlayStatic(filename, coordinates, audioParams);
    }

    private (EntityUid Entity, AudioComponent Component) CreateAndStartPlayingStream(AudioParams? audioParams, AudioStream stream)
    {
        var audioP = audioParams ?? AudioParams.Default;
        var entity = EntityManager.CreateEntityUninitialized("Audio", MapCoordinates.Nullspace);
        var comp = SetupAudio(entity, stream.Name!, audioP);
        EntityManager.InitializeAndStartEntity(entity);
        var source = comp.Source;

        // TODO clamp the offset inside of SetPlaybackPosition() itself.
        var offset = audioP.PlayOffsetSeconds;
        offset = Math.Clamp(offset, 0f, (float) stream.Length.TotalSeconds);
        source.PlaybackPosition = offset;

        ApplyAudioParams(audioP, comp);
        comp.Params = audioP;
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
        source.MaxDistance = audioParams.MaxDistance;
        source.ReferenceDistance = audioParams.ReferenceDistance;
        source.Looping = audioParams.Loop;
    }
}
