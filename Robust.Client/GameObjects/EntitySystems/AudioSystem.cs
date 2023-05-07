using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared;
using Robust.Shared.Audio;
using Robust.Shared.Exceptions;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Random;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects;

[UsedImplicitly]
public sealed class AudioSystem : SharedAudioSystem
{
    [Dependency] private readonly SharedPhysicsSystem _broadPhaseSystem = default!;
    [Dependency] private readonly IClydeAudio _clyde = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IParallelManager _parMan = default!;
    [Dependency] private readonly SharedTransformSystem _xformSys = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IRuntimeLog _runtimeLog = default!;

    private readonly List<PlayingStream> _playingClydeStreams = new();

    private ISawmill _sawmill = default!;

    private float _maxRayLength;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<PlayAudioEntityMessage>(PlayAudioEntityHandler);
        SubscribeNetworkEvent<PlayAudioGlobalMessage>(PlayAudioGlobalHandler);
        SubscribeNetworkEvent<PlayAudioPositionalMessage>(PlayAudioPositionalHandler);
        SubscribeNetworkEvent<StopAudioMessageClient>(StopAudioMessageHandler);

        _sawmill = Logger.GetSawmill("audio");

        CfgManager.OnValueChanged(CVars.AudioRaycastLength, OnRaycastLengthChanged, true);
    }

    public override void Shutdown()
    {
        CfgManager.UnsubValueChanged(CVars.AudioRaycastLength, OnRaycastLengthChanged);
        foreach (var stream in _playingClydeStreams)
        {
            stream.Source.Dispose();
        }
        _playingClydeStreams.Clear();

        base.Shutdown();
    }

    private void OnRaycastLengthChanged(float value)
    {
        _maxRayLength = value;
    }

    #region Event Handlers
    private void PlayAudioEntityHandler(PlayAudioEntityMessage ev)
    {
        var stream = EntityManager.EntityExists(ev.EntityUid)
            ? (PlayingStream?) Play(ev.FileName, ev.EntityUid, ev.FallbackCoordinates, ev.AudioParams)
            : (PlayingStream?) Play(ev.FileName, ev.Coordinates, ev.FallbackCoordinates, ev.AudioParams);

        if (stream != null)
            stream.NetIdentifier = ev.Identifier;
    }

    private void PlayAudioGlobalHandler(PlayAudioGlobalMessage ev)
    {
        var stream = (PlayingStream?) Play(ev.FileName, ev.AudioParams);
        if (stream != null)
            stream.NetIdentifier = ev.Identifier;
    }

    private void PlayAudioPositionalHandler(PlayAudioPositionalMessage ev)
    {
        var stream = (PlayingStream?) Play(ev.FileName, ev.Coordinates, ev.FallbackCoordinates, ev.AudioParams);
        if (stream != null)
            stream.NetIdentifier = ev.Identifier;
    }

    private void StopAudioMessageHandler(StopAudioMessageClient ev)
    {
        var stream = _playingClydeStreams.Find(p => p.NetIdentifier == ev.Identifier);
        if (stream == null)
            return;

        stream.Done = true;
        stream.Source.Dispose();
        _playingClydeStreams.Remove(stream);
    }
    #endregion

    public override void FrameUpdate(float frameTime)
    {
        var xforms = GetEntityQuery<TransformComponent>();
        var physics = GetEntityQuery<PhysicsComponent>();
        var ourPos = _eyeManager.CurrentEye.Position;
        var opts = new ParallelOptions { MaxDegreeOfParallelism = _parMan.ParallelProcessCount };

        try
        {
            Parallel.ForEach(_playingClydeStreams, opts, (stream) => ProcessStream(stream, ourPos, xforms, physics));
        }
        catch (Exception e)
        {
            _sawmill.Error($"Caught exception while processing entity streams.");
            _runtimeLog.LogException(e, $"{nameof(AudioSystem)}.{nameof(FrameUpdate)}");
        }
        finally
        {

            for (var i = _playingClydeStreams.Count - 1; i >= 0; i--)
            {
                var stream = _playingClydeStreams[i];
                if (stream.Done)
                {
                    stream.Source.Dispose();
                    _playingClydeStreams.RemoveSwap(i);
                }
            }
        }
    }

    private void ProcessStream(PlayingStream stream,
        MapCoordinates listener,
        EntityQuery<TransformComponent> xforms,
        EntityQuery<PhysicsComponent> physics)
    {
        if (!stream.Source.IsPlaying)
        {
            stream.Done = true;
            return;
        }

        if (stream.Source.IsGlobal)
        {
            DebugTools.Assert(stream.TrackingCoordinates == null
                && stream.TrackingEntity == null
                && stream.TrackingFallbackCoordinates == null);

            return;
        }

        DebugTools.Assert(stream.TrackingCoordinates != null
            || stream.TrackingEntity != null
            || stream.TrackingFallbackCoordinates != null);

        // Get audio Position
        if (!TryGetStreamPosition(stream, xforms, out var mapPos)
            || mapPos == MapCoordinates.Nullspace
            || mapPos.Value.MapId != listener.MapId)
        {
            stream.Done = true;
            return;
        }

        // Max distance check
        var delta = mapPos.Value.Position - listener.Position;
        var distance = delta.Length;
        if (distance > stream.MaxDistance)
        {
            stream.Source.SetVolumeDirect(0);
            return;
        }

        // Update audio occlusion
        float occlusion = 0;
        if (distance > 0.1)
        {
            var rayLength = MathF.Min(distance, _maxRayLength);
            var ray = new CollisionRay(listener.Position, delta/distance, OcclusionCollisionMask);
            occlusion = _broadPhaseSystem.IntersectRayPenetration(listener.MapId, ray, rayLength, stream.TrackingEntity);
        }
        stream.Source.SetOcclusion(occlusion);

        // Update attenuation dependent volume.
        UpdatePositionalVolume(stream, distance);

        // Update audio positions.
        var audioPos = stream.Attenuation != Attenuation.NoAttenuation ? mapPos.Value : listener;
        if (!stream.Source.SetPosition(audioPos.Position))
        {
            _sawmill.Warning("Interrupting positional audio, can't set position.");
            stream.Source.StopPlaying();
            return;
        }

        // Make race cars go NYYEEOOOOOMMMMM
        if (stream.TrackingEntity != null && physics.TryGetComponent(stream.TrackingEntity, out var physicsComp))
        {
            // This actually gets the tracked entity's xform & iterates up though the parents for the second time. Bit
            // inefficient.
            var velocity = _physics.GetMapLinearVelocity(stream.TrackingEntity.Value, physicsComp, null, xforms, physics);
            stream.Source.SetVelocity(velocity);
        }
    }

    private void UpdatePositionalVolume(PlayingStream stream, float distance)
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
        stream.Source.SetVolumeDirect(actualGain);
    }

    private bool TryGetStreamPosition(PlayingStream stream, EntityQuery<TransformComponent> xformQuery, [NotNullWhen(true)] out MapCoordinates? mapPos)
    {
        if (stream.TrackingCoordinates != null)
        {
            mapPos = stream.TrackingCoordinates.Value.ToMap(EntityManager);
            if (mapPos != MapCoordinates.Nullspace)
                return true;
        }

        if (xformQuery.TryGetComponent(stream.TrackingEntity, out var xform)
            && xform.MapID != MapId.Nullspace)
        {
            mapPos = new MapCoordinates(_xformSys.GetWorldPosition(xform, xformQuery), xform.MapID);
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
        if (_resourceCache.TryGetResource<AudioResource>(new ResPath(filename), out audio))
            return true;

        _sawmill.Error($"Server tried to play audio file {filename} which does not exist.");
        return false;
    }

    private bool TryCreateAudioSource(AudioStream stream, [NotNullWhen(true)] out IClydeAudioSource? source)
    {
        if (!_timing.IsFirstTimePredicted)
        {
            source = null;
            _sawmill.Error($"Tried to create audio source outside of prediction!");
            DebugTools.Assert(false);
            return false;
        }

        source = _clyde.CreateAudioSource(stream);
        return source != null;
    }

    private PlayingStream CreateAndStartPlayingStream(IClydeAudioSource source, AudioParams? audioParams)
    {
        ApplyAudioParams(audioParams, source);
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
    private IPlayingAudioStream? Play(string filename, AudioParams? audioParams = null)
    {
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
            _sawmill.Error($"Error setting up global audio for {stream.Name}: {0}", Environment.StackTrace);
            return null;
        }

        source.SetGlobal();

        return CreateAndStartPlayingStream(source, audioParams);
    }

    /// <summary>
    ///     Play an audio file following an entity.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="entity">The entity "emitting" the audio.</param>
    /// <param name="fallbackCoordinates">The map or grid coordinates at which to play the audio when entity is invalid.</param>
    /// <param name="audioParams"></param>
    private IPlayingAudioStream? Play(string filename, EntityUid entity, EntityCoordinates fallbackCoordinates,
        AudioParams? audioParams = null)
    {
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
            _sawmill.Error($"Error setting up entity audio for {stream.Name} / {ToPrettyString(entity)}: {0}", Environment.StackTrace);
            return null;
        }

        var query = GetEntityQuery<TransformComponent>();
        var xform = query.GetComponent(entity);
        var worldPos = _xformSys.GetWorldPosition(xform, query);
        fallbackCoordinates ??= GetFallbackCoordinates(new MapCoordinates(worldPos, xform.MapID));

        if (!source.SetPosition(worldPos))
            return Play(stream, fallbackCoordinates.Value, fallbackCoordinates.Value, audioParams);

        var playing = CreateAndStartPlayingStream(source, audioParams);
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
        EntityCoordinates fallbackCoordinates, AudioParams? audioParams = null)
    {
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
            _sawmill.Error($"Error setting up coordinates audio for {stream.Name} / {coordinates}: {0}", Environment.StackTrace);
            return null;
        }

        if (!source.SetPosition(fallbackCoordinates.Position))
        {
            source.Dispose();
            _sawmill.Warning($"Can't play positional audio \"{stream.Name}\", can't set position.");
            return null;
        }

        var playing = CreateAndStartPlayingStream(source, audioParams);
        playing.TrackingCoordinates = coordinates;
        playing.TrackingFallbackCoordinates = fallbackCoordinates != EntityCoordinates.Invalid ? fallbackCoordinates : null;
        return playing;
    }
    #endregion

    /// <inheritdoc />
    public override IPlayingAudioStream? PlayPredicted(SoundSpecifier? sound, EntityUid source, EntityUid? user,
        AudioParams? audioParams = null)
    {
        if (_timing.IsFirstTimePredicted || sound == null)
            return Play(sound, Filter.Local(), source, false, audioParams);
        return null; // uhh Lets hope predicted audio never needs to somehow store the playing audio....
    }

    public override IPlayingAudioStream? PlayPredicted(SoundSpecifier? sound, EntityCoordinates coordinates, EntityUid? user,
        AudioParams? audioParams = null)
    {
        if (_timing.IsFirstTimePredicted || sound == null)
            return Play(sound, Filter.Local(), coordinates, false, audioParams);
        return null;
    }

    private void ApplyAudioParams(AudioParams? audioParams, IClydeAudioSource source)
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
        source.SetPlaybackPosition(audioParams.Value.PlayOffsetSeconds);
        source.IsLooping = audioParams.Value.Loop;
    }

    public sealed class PlayingStream : IPlayingAudioStream
    {
        public uint? NetIdentifier;
        public IClydeAudioSource Source = default!;
        public EntityUid? TrackingEntity;
        public EntityCoordinates? TrackingCoordinates;
        public EntityCoordinates? TrackingFallbackCoordinates;
        public bool Done;
        public float Volume;

        public float MaxDistance;
        public float ReferenceDistance;
        public float RolloffFactor;

        public Attenuation Attenuation
        {
            get => _attenuation;
            set
            {
                if (value == _attenuation) return;
                _attenuation = value;
                if (_attenuation != Attenuation.Default)
                {
                    // Need to disable default attenuation when using a custom one
                    // Damn Sloth wanting linear ambience sounds so they smoothly cut-off and are short-range
                    Source.SetRolloffFactor(0f);
                }
            }
        }
        private Attenuation _attenuation = Attenuation.Default;

        public void Stop()
        {
            Source.StopPlaying();
        }
    }

    /// <inheritdoc />
    public override IPlayingAudioStream? PlayGlobal(string filename, Filter playerFilter, bool recordReplay, AudioParams? audioParams = null)
    {
        return Play(filename, audioParams);
    }

    /// <inheritdoc />
    public override IPlayingAudioStream? Play(string filename, Filter playerFilter, EntityUid entity, bool recordReplay, AudioParams? audioParams = null)
    {
        if (_resourceCache.TryGetResource<AudioResource>(new ResPath(filename), out var audio))
        {
            return Play(audio, entity, null, audioParams);
        }

        _sawmill.Error($"Server tried to play audio file {filename} which does not exist.");
        return default;
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
        if (_resourceCache.TryGetResource<AudioResource>(new ResPath(filename), out var audio))
        {
            return Play(audio, uid, null, audioParams);
        }
        return null;
    }

    /// <inheritdoc />
    public override IPlayingAudioStream? PlayEntity(string filename, EntityUid recipient, EntityUid uid, AudioParams? audioParams = null)
    {
        if (_resourceCache.TryGetResource<AudioResource>(new ResPath(filename), out var audio))
        {
            return Play(audio, uid, null, audioParams);
        }
        return null;
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
