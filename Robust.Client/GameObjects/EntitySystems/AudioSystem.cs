using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Threading.Tasks;
using Robust.Client.Audio;
using Robust.Client.Audio.Sources;
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
using Robust.Shared.Threading;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects;

public sealed class AudioSystem : SharedAudioSystem
{
    [Dependency] private readonly IReplayRecordingManager _replayRecording = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IClientResourceCache _resourceCache = default!;
    [Dependency] private readonly IOverlayManager _overlays = default!;
    [Dependency] private readonly IParallelManager _parMan = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRuntimeLog _runtimeLog = default!;
    [Dependency] private readonly IAudioInternal _audio = default!;
    [Dependency] private readonly SharedTransformSystem _xformSys = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    /// <summary>
    /// Per-tick cache of relevant streams.
    /// </summary>
    private readonly List<(EntityUid Entity, AudioComponent Component)> _streams = new();

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    private float _maxRayLength;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<AudioComponent, ComponentStartup>(OnAudioStartup);
        SubscribeLocalEvent<AudioComponent, ComponentShutdown>(OnAudioShutdown);
        SubscribeLocalEvent<AudioComponent, EntityPausedEvent>(OnAudioPaused);
        SubscribeLocalEvent<AudioComponent, EntityUnpausedEvent>(OnAudioUnpaused);

        CfgManager.OnValueChanged(CVars.AudioRaycastLength, OnRaycastLengthChanged, true);

        _overlays.AddOverlay(new AudioOverlay(EntityManager, _playerManager, IoCManager.Resolve<IClientResourceCache>(), this, _xformSys));
    }

    /// <summary>
    /// Sets the volume for the entire game.
    /// </summary>
    public void SetMasterVolume(float value)
    {
        throw new NotImplementedException();
    }

    public override void Shutdown()
    {
        CfgManager.UnsubValueChanged(CVars.AudioRaycastLength, OnRaycastLengthChanged);
        base.Shutdown();
    }

    private void OnAudioPaused(EntityUid uid, AudioComponent component, ref EntityPausedEvent args)
    {
        // TODO: OpenAL scrubbing through audio.
        throw new NotImplementedException();
    }

    private void OnAudioUnpaused(EntityUid uid, AudioComponent component, ref EntityUnpausedEvent args)
    {
        // TODO: OpenAL scrubbing through audio.
        throw new NotImplementedException();
    }

    private void OnAudioStartup(EntityUid uid, AudioComponent component, ComponentStartup args)
    {
        if (!Timing.IsFirstTimePredicted || !TryGetAudio(component.FileName, out var audioResource))
            return;

        var source = _audio.CreateAudioSource(audioResource);

        if (source == null)
            return;

        component.Stream = new PlayingStream()
        {
            Source = source,
        };
    }

    private void OnAudioShutdown(EntityUid uid, AudioComponent component, ComponentShutdown args)
    {
        var pStream = component.Stream;
        pStream.Dispose();
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

        var query = AllEntityQuery<AudioComponent>();
        _streams.Clear();

        while (query.MoveNext(out var uid, out var comp))
        {
            _streams.Add((uid, comp));
        }

        try
        {
            Parallel.ForEach(_streams, opts, comp => ProcessStream(comp.Entity, comp.Component, comp.Component.Stream, ourPos));
        }
        catch (Exception e)
        {
            Log.Error($"Caught exception while processing entity streams.");
            _runtimeLog.LogException(e, $"{nameof(AudioSystem)}.{nameof(FrameUpdate)}");
        }
        finally
        {
            for (var i = _streams.Count - 1; i >= 0; i--)
            {
                var comp = _streams[i];
                var stream = comp.Component.Stream;

                if (stream.Done)
                {
                    QueueDel(comp.Entity);
                }
            }
        }
    }

    private void ProcessStream(EntityUid entity, AudioComponent component, IPlayingAudioStream stream, MapCoordinates listener)
    {
        if (!stream.IsPlaying)
        {
            stream.Done = true;
            return;
        }

        // TODO:
        // - If it's global always play
        // If it's attached to OUR map always play
        //
        if (component.AudioType == AudioType.Global)
            return;

        // Get audio Position
        if (!TryGetStreamPosition(stream, out var mapPos)
            || mapPos == MapCoordinates.Nullspace
            || mapPos.Value.MapId != listener.MapId)
        {
            stream.Done = true;
            return;
        }

        // Max distance check
        var delta = mapPos.Value.Position - listener.Position;
        var distance = delta.Length();

        if (distance > stream.MaxDistance)
        {
            stream.Source.SetVolumeDirect(0);
            return;
        }

        // Update audio occlusion
        var occlusion = GetOcclusion(stream, listener, delta, distance);
        stream.Source.SetOcclusion(occlusion);

        // Update attenuation dependent volume.
        stream.Source.SetVolumeDirect(GetPositionalVolume(stream, distance));

        // Update audio positions.
        var audioPos = stream.Attenuation != Attenuation.NoAttenuation ? mapPos.Value : listener;
        if (!stream.Source.SetPosition(audioPos.Position))
        {
            Log.Warning("Interrupting positional audio, can't set position.");
            stream.Source.StopPlaying();
            return;
        }

        // Make race cars go NYYEEOOOOOMMMMM
        if (stream.TrackingEntity != null && _physicsQuery.TryGetComponent(stream.TrackingEntity, out var physicsComp))
        {
            // This actually gets the tracked entity's xform & iterates up though the parents for the second time. Bit
            // inefficient.
            var velocity = _physics.GetMapLinearVelocity(stream.TrackingEntity.Value, physicsComp, null, _xformQuery, _physicsQuery);
            stream.Source.SetVelocity(velocity);
        }
    }

    internal float GetOcclusion(PlayingStream stream, MapCoordinates listener, Vector2 delta, float distance)
    {
        float occlusion = 0;

        if (distance > 0.1)
        {
            var rayLength = MathF.Min(distance, _maxRayLength);
            var ray = new CollisionRay(listener.Position, delta / distance, OcclusionCollisionMask);
            occlusion = _physics.IntersectRayPenetration(listener.MapId, ray, rayLength, stream.TrackingEntity);
        }

        return occlusion;
    }

    internal float GetPositionalVolume(PlayingStream stream, float distance)
    {
        // OpenAL also limits the distance to <= AL_MAX_DISTANCE, but since we cull
        // sources that are further away than stream.MaxDistance, we don't do that.
        distance = MathF.Max(stream.ReferenceDistance, distance);
        float gain;

        // Technically these are formulas for gain not decibels but EHHHHHHHH.
        switch (stream.Attenuation)
        {
            case Attenuation.Default:
                gain = 1f;
                break;
            // You thought I'd implement clamping per source? Hell no that's just for the overall OpenAL setting
            // I didn't even wanna implement this much for linear but figured it'd be cleaner.
            case Attenuation.InverseDistanceClamped:
            case Attenuation.InverseDistance:
                gain = stream.ReferenceDistance
                        / (stream.ReferenceDistance
                            + stream.RolloffFactor * (distance - stream.ReferenceDistance));

                break;
            case Attenuation.LinearDistanceClamped:
            case Attenuation.LinearDistance:
                gain = 1f
                        - stream.RolloffFactor
                        * (distance - stream.ReferenceDistance)
                        / (stream.MaxDistance - stream.ReferenceDistance);

                break;
            case Attenuation.ExponentDistanceClamped:
            case Attenuation.ExponentDistance:
                gain = MathF.Pow(distance / stream.ReferenceDistance, -stream.RolloffFactor);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    $"No implemented attenuation for {stream.Attenuation}");
        }

        var volume = MathF.Pow(10, stream.Volume / 10);
        var actualGain = MathF.Max(0f, volume * gain);
        return actualGain;
    }

    private bool TryGetStreamPosition(PlayingStream stream, [NotNullWhen(true)] out MapCoordinates? mapPos)
    {
        if (stream.TrackingCoordinates != null)
        {
            mapPos = stream.TrackingCoordinates.Value.ToMap(EntityManager);
            if (mapPos != MapCoordinates.Nullspace)
                return true;
        }

        if (_xformQuery.TryGetComponent(stream.TrackingEntity, out var xform)
            && xform.MapID != MapId.Nullspace)
        {
            mapPos = new MapCoordinates(_xformSys.GetWorldPosition(xform), xform.MapID);
            return true;
        }

        if (stream.TrackingFallbackCoordinates != null)
        {
            mapPos = stream.TrackingFallbackCoordinates.Value.ToMap(EntityManager);
            return mapPos != MapCoordinates.Nullspace;
        }

        mapPos = MapCoordinates.Nullspace;
        return false;
    }

    #region Play AudioStream
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

        source = _clyde.CreateAudioSource(stream);
        return source != null;
    }

    private PlayingStream CreateAndStartPlayingStream(IAudioSource source, AudioParams? audioParams, AudioStream stream)
    {
        ApplyAudioParams(audioParams, source, stream);
        source.StartPlaying();
        var playing = new PlayingStream
        {
            Source = source,
            Attenuation = audioParams?.Attenuation ?? Attenuation.Default,
            MaxDistance = audioParams?.MaxDistance ?? float.MaxValue,
            ReferenceDistance = audioParams?.ReferenceDistance ?? 1f,
            RolloffFactor = audioParams?.RolloffFactor ?? 1f,
            Volume = audioParams?.Volume ?? 0
        };

        _playingClydeStreams.Add(playing);
        return playing;
    }

    /// <summary>
    ///     Play an audio file globally, without position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="audioParams"></param>
    private IPlayingAudioStream? Play(string filename, AudioParams? audioParams = null, bool recordReplay = true)
    {
        if (recordReplay && _replayRecording.IsRecording)
        {
            _replayRecording.RecordReplayMessage(new PlayAudioGlobalMessage
            {
                FileName = filename,
                AudioParams = audioParams ?? AudioParams.Default
            });
        }

        return TryGetAudio(filename, out var audio) ? Play(audio, audioParams) : default;
    }

    /// <summary>
    ///     Play an audio stream globally, without position.
    /// </summary>
    /// <param name="stream">The audio stream to play.</param>
    /// <param name="audioParams"></param>
    private IPlayingAudioStream? Play(AudioStream stream, AudioParams? audioParams = null)
    {
        if (!TryCreateAudioSource(stream, out var source))
        {
            Log.Error($"Error setting up global audio for {stream.Name}: {0}", Environment.StackTrace);
            return null;
        }

        source.SetGlobal();

        return CreateAndStartPlayingStream(source, audioParams, stream);
    }

    /// <summary>
    ///     Play an audio file following an entity.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="entity">The entity "emitting" the audio.</param>
    /// <param name="fallbackCoordinates">The map or grid coordinates at which to play the audio when entity is invalid.</param>
    /// <param name="audioParams"></param>
    private IPlayingAudioStream? Play(string filename, EntityUid entity, EntityCoordinates? fallbackCoordinates,
        AudioParams? audioParams = null, bool recordReplay = true)
    {
        if (recordReplay && _replayRecording.IsRecording)
        {
            _replayRecording.RecordReplayMessage(new PlayAudioEntityMessage
            {
                FileName = filename,
                NetEntity = GetNetEntity(entity),
                FallbackCoordinates = GetNetCoordinates(fallbackCoordinates) ?? default,
                AudioParams = audioParams ?? AudioParams.Default
            });
        }

        return TryGetAudio(filename, out var audio) ? Play(audio, entity, fallbackCoordinates, audioParams) : default;
    }

    /// <summary>
    ///     Play an audio stream following an entity.
    /// </summary>
    /// <param name="stream">The audio stream to play.</param>
    /// <param name="entity">The entity "emitting" the audio.</param>
    /// <param name="fallbackCoordinates">The map or grid coordinates at which to play the audio when entity is invalid.</param>
    /// <param name="audioParams"></param>
    private IPlayingAudioStream? Play(AudioStream stream, EntityUid entity, EntityCoordinates? fallbackCoordinates = null,
        AudioParams? audioParams = null)
    {
        if (!TryCreateAudioSource(stream, out var source))
        {
            Log.Error($"Error setting up entity audio for {stream.Name} / {ToPrettyString(entity)}: {0}", Environment.StackTrace);
            return null;
        }

        var query = GetEntityQuery<TransformComponent>();
        var xform = query.GetComponent(entity);
        var worldPos = _xformSys.GetWorldPosition(xform, query);
        fallbackCoordinates ??= GetFallbackCoordinates(new MapCoordinates(worldPos, xform.MapID));

        if (!source.SetPosition(worldPos))
            return Play(stream, fallbackCoordinates.Value, fallbackCoordinates.Value, audioParams);

        var playing = CreateAndStartPlayingStream(source, audioParams, stream);
        playing.TrackingEntity = entity;
        playing.TrackingFallbackCoordinates = fallbackCoordinates != EntityCoordinates.Invalid ? fallbackCoordinates : null;
        return playing;
    }

    /// <summary>
    ///     Play an audio file at a static position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    /// <param name="fallbackCoordinates">The map or grid coordinates at which to play the audio when coordinates are invalid.</param>
    /// <param name="audioParams"></param>
    private IPlayingAudioStream? Play(string filename, EntityCoordinates coordinates,
        EntityCoordinates fallbackCoordinates, AudioParams? audioParams = null, bool recordReplay = true)
    {
        if (recordReplay && _replayRecording.IsRecording)
        {
            _replayRecording.RecordReplayMessage(new PlayAudioPositionalMessage
            {
                FileName = filename,
                Coordinates = GetNetCoordinates(coordinates),
                FallbackCoordinates = GetNetCoordinates(fallbackCoordinates),
                AudioParams = audioParams ?? AudioParams.Default
            });
        }

        return TryGetAudio(filename, out var audio) ? Play(audio, coordinates, fallbackCoordinates, audioParams) : default;
    }

    /// <summary>
    ///     Play an audio stream at a static position.
    /// </summary>
    /// <param name="stream">The audio stream to play.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    /// <param name="fallbackCoordinates">The map or grid coordinates at which to play the audio when coordinates are invalid.</param>
    /// <param name="audioParams"></param>
    private IPlayingAudioStream? Play(AudioStream stream, EntityCoordinates coordinates,
        EntityCoordinates fallbackCoordinates, AudioParams? audioParams = null)
    {
        if (!TryCreateAudioSource(stream, out var source))
        {
            Log.Error($"Error setting up coordinates audio for {stream.Name} / {coordinates}: {0}", Environment.StackTrace);
            return null;
        }

        if (!source.SetPosition(fallbackCoordinates.Position))
        {
            source.Dispose();
            Log.Warning($"Can't play positional audio \"{stream.Name}\", can't set position.");
            return null;
        }

        var playing = CreateAndStartPlayingStream(source, audioParams, stream);
        playing.TrackingCoordinates = coordinates;
        playing.TrackingFallbackCoordinates = fallbackCoordinates != EntityCoordinates.Invalid ? fallbackCoordinates : null;
        return playing;
    }
    #endregion

    /// <inheritdoc />
    public override IPlayingAudioStream? PlayPredicted(SoundSpecifier? sound, EntityUid source, EntityUid? user,
        AudioParams? audioParams = null)
    {
        if (Timing.IsFirstTimePredicted || sound == null)
            return PlayEntity(sound, Filter.Local(), source, false, audioParams);
        return null; // uhh Lets hope predicted audio never needs to somehow store the playing audio....
    }

    public override IPlayingAudioStream? PlayPredicted(SoundSpecifier? sound, EntityCoordinates coordinates, EntityUid? user,
        AudioParams? audioParams = null)
    {
        if (Timing.IsFirstTimePredicted || sound == null)
            return Play(sound, Filter.Local(), coordinates, false, audioParams);
        return null;
    }

    private void ApplyAudioParams(AudioParams? audioParams, IAudioSource source, AudioStream audio)
    {
        if (!audioParams.HasValue)
            return;

        if (audioParams.Value.Variation.HasValue)
            source.SetPitch(audioParams.Value.PitchScale
                            * (float) RandMan.NextGaussian(1, audioParams.Value.Variation.Value));
        else
            source.SetPitch(audioParams.Value.PitchScale);

        source.SetVolume(audioParams.Value.Volume);
        source.SetRolloffFactor(audioParams.Value.RolloffFactor);
        source.SetMaxDistance(audioParams.Value.MaxDistance);
        source.SetReferenceDistance(audioParams.Value.ReferenceDistance);
        source.Looping = audioParams.Value.Loop;

        // TODO clamp the offset inside of SetPlaybackPosition() itself.
        var offset = audioParams.Value.PlayOffsetSeconds;
        offset = Math.Clamp(offset, 0f, (float) audio.Length.TotalSeconds);
        source.SetPlaybackPosition(offset);
    }

    /// <inheritdoc />
    public override IPlayingAudioStream? PlayGlobal(string filename, Filter playerFilter, bool recordReplay, AudioParams? audioParams = null)
    {
        return Play(filename, audioParams);
    }

    /// <inheritdoc />
    public override IPlayingAudioStream? Play(string filename, Filter playerFilter, EntityUid entity, bool recordReplay, AudioParams? audioParams = null)
    {
        return Play(filename, entity, null, audioParams);
    }

    /// <inheritdoc />
    public override IPlayingAudioStream? Play(string filename, Filter playerFilter, EntityCoordinates coordinates, bool recordReplay, AudioParams? audioParams = null)
    {
        return Play(filename, coordinates, GetFallbackCoordinates(coordinates.ToMap(EntityManager)), audioParams);
    }

    /// <inheritdoc />
    public override IPlayingAudioStream? PlayGlobal(string filename, ICommonSession recipient, AudioParams? audioParams = null)
    {
        return Play(filename, audioParams);
    }

    /// <inheritdoc />
    public override IPlayingAudioStream? PlayGlobal(string filename, EntityUid recipient, AudioParams? audioParams = null)
    {
        return Play(filename, audioParams);
    }

    /// <inheritdoc />
    public override IPlayingAudioStream? PlayEntity(string filename, ICommonSession recipient, EntityUid uid, AudioParams? audioParams = null)
    {
        return Play(filename, uid, null, audioParams);
    }

    /// <inheritdoc />
    public override IPlayingAudioStream? PlayEntity(string filename, EntityUid recipient, EntityUid uid, AudioParams? audioParams = null)
    {
        return Play(filename, uid, null, audioParams);
    }

    /// <inheritdoc />
    public override IPlayingAudioStream? PlayStatic(string filename, ICommonSession recipient, EntityCoordinates coordinates, AudioParams? audioParams = null)
    {
        return Play(filename, coordinates, GetFallbackCoordinates(coordinates.ToMap(EntityManager)), audioParams);
    }

    /// <inheritdoc />
    public override IPlayingAudioStream? PlayStatic(string filename, EntityUid recipient, EntityCoordinates coordinates, AudioParams? audioParams = null)
    {
        return Play(filename, coordinates, GetFallbackCoordinates(coordinates.ToMap(EntityManager)), audioParams);
    }
}
