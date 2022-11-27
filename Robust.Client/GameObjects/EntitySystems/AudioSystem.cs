using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects;

[UsedImplicitly]
public sealed class AudioSystem : SharedAudioSystem
{
    [Dependency] private readonly SharedPhysicsSystem _broadPhaseSystem = default!;
    [Dependency] private readonly IClydeAudio _clyde = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _xformSys = default!;

    private readonly List<PlayingStream> _playingClydeStreams = new();

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<PlayAudioEntityMessage>(PlayAudioEntityHandler);
        SubscribeNetworkEvent<PlayAudioGlobalMessage>(PlayAudioGlobalHandler);
        SubscribeNetworkEvent<PlayAudioPositionalMessage>(PlayAudioPositionalHandler);
        SubscribeNetworkEvent<StopAudioMessageClient>(StopAudioMessageHandler);
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
        var mapId = ev.Coordinates.GetMapId(EntityManager);

        if (!_mapManager.MapExists(mapId))
        {
            Logger.Error($"Server tried to play sound on map {mapId}, which does not exist. Ignoring.");
            return;
        }

        var stream = (PlayingStream?) Play(ev.FileName, ev.Coordinates, ev.FallbackCoordinates, ev.AudioParams);
        if (stream != null)
            stream.NetIdentifier = ev.Identifier;
    }

    private void StopAudioMessageHandler(StopAudioMessageClient ev)
    {
        var stream = _playingClydeStreams.Find(p => p.NetIdentifier == ev.Identifier);
        if (stream == null)
            return;

        StreamDone(stream);
        _playingClydeStreams.Remove(stream);
    }
    #endregion

    public override void FrameUpdate(float frameTime)
    {
        // Update positions of streams every frame.
        // Start with an initial pass to cull streams that need to be removed, and sort stuff out.
        Span<int> validIndices = stackalloc int[_playingClydeStreams.Count];
        var validCount = 0;

        // Initial clearing pass
        try
        {
            var metaQuery = GetEntityQuery<MetaDataComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();
            int streamIndexOut = 0;
            foreach (var stream in _playingClydeStreams)
            {
                // Note: continue; in here is expected to have one of two outcomes:
                // + StreamDone
                // + streamIndexOut++

                // Occlusion recalculation parallel needs a way to know which targets to actually recalculate for.
                // That in mind start by setting this to false (it's set to true later when relevant)
                stream.OcclusionValidTemporary = false;

                if (!stream.Source.IsPlaying)
                {
                    StreamDone(stream);
                    continue;
                }

                MapCoordinates? mapPos = null;

                if (stream.TrackingCoordinates != null)
                {
                    var coords = stream.TrackingCoordinates.Value;
                    mapPos = coords.ToMap(EntityManager);

                    if (!_mapManager.MapExists(mapPos.Value.MapId))
                    {
                        // Map no longer exists, delete stream.
                        StreamDone(stream);
                        continue;
                    }
                }
                else if (stream.TrackingEntity.IsValid())
                {
                    if (!metaQuery.TryGetComponent(stream.TrackingEntity, out var meta) ||
                        Deleted(stream.TrackingEntity, meta) ||
                        !xformQuery.TryGetComponent(stream.TrackingEntity, out var xform))
                    {
                        StreamDone(stream);
                        continue;
                    }

                    mapPos = xform.MapPosition;
                }

                if (mapPos == null || mapPos.Value.MapId == MapId.Nullspace)
                {
                    // Positionless audio
                    if (stream.TrackingFallbackCoordinates == null)
                    {
                        validIndices[validCount] = streamIndexOut;
                        validCount++;
                    }
                    else
                    {
                        mapPos = stream.TrackingFallbackCoordinates?.ToMap(EntityManager);
                    }
                }

                if (mapPos != null && mapPos.Value.MapId != MapId.Nullspace)
                {
                    stream.MapCoordinatesTemporary = mapPos.Value;
                    // this has a map position so it's good to go to the other processes
                    validIndices[validCount] = streamIndexOut;
                    // check for occlusion recalc
                    stream.OcclusionValidTemporary = mapPos.Value.MapId == _eyeManager.CurrentMap;
                    validCount++;
                }

                // This stream gets to live!
                streamIndexOut++;
            }
        }
        finally
        {
            // if this doesn't get ran (exception...) then the list can fill up with disposed garbage.
            // that will then throw on IsPlaying.
            // meaning it'll break the entire audio system.
            _playingClydeStreams.RemoveAll(p => p.Done);
        }

        var ourPos = _eyeManager.CurrentEye.Position.Position;

        // Occlusion calculation pass

        Parallel.For(0, _playingClydeStreams.Count, i =>
        {
            var stream = _playingClydeStreams[i];
            // As set earlier.
            if (stream.OcclusionValidTemporary)
            {
                var pos = stream.MapCoordinatesTemporary;
                var sourceRelative = ourPos - pos.Position;
                var occlusion = 0f;
                if (sourceRelative.Length > 0)
                {
                    occlusion = _broadPhaseSystem.IntersectRayPenetration(pos.MapId,
                        new CollisionRay(pos.Position, sourceRelative.Normalized, OcclusionCollisionMask),
                        sourceRelative.Length, stream.TrackingEntity);
                }

                stream.OcclusionTemporary = occlusion;
            }
        });

        // Occlusion apply / Attenuation / position / velocity pass
        // Note that for streams for which MapCoordinatesTemporary isn't updated, they don't get here
        for (var i = 0; i < validCount; i++)
        {
            var stream = _playingClydeStreams[validIndices[i]];
            var pos = stream.MapCoordinatesTemporary;

            if (stream.OcclusionValidTemporary)
                stream.Source.SetOcclusion(stream.OcclusionTemporary);

            if (stream.Source.IsGlobal)
            {
                stream.Source.SetVolume(stream.Volume);
            }
            else if (pos.MapId != _eyeManager.CurrentMap)
                stream.Source.SetVolumeDirect(0f);
            else
            {
                var sourceRelative = ourPos - pos.Position;
                // OpenAL uses MaxDistance to limit how much attenuation can *reduce* the gain,
                // and doesn't do any culling. We however cull based on MaxDistance, because
                // this is what all current code that uses MaxDistance expects and because
                // we don't need the OpenAL behaviour.
                if (sourceRelative.Length > stream.MaxDistance)
                    stream.Source.SetVolumeDirect(0f);
                else
                {
                    // OpenAL also limits the distance to <= AL_MAX_DISTANCE, but since we cull
                    // sources that are further away than stream.MaxDistance, we don't do that.
                    var distance = MathF.Max(stream.ReferenceDistance, sourceRelative.Length);
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
                                $"No implemented attenuation for {stream.Attenuation.ToString()}");
                    }

                    var volume = MathF.Pow(10, stream.Volume / 10);
                    var actualGain = MathF.Max(0f, volume * gain);

                    stream.Source.SetVolumeDirect(actualGain);
                    var audioPos = stream.Attenuation != Attenuation.NoAttenuation ? pos.Position : ourPos;

                    if (!stream.Source.SetPosition(audioPos))
                    {
                        Logger.Warning("Interrupting positional audio, can't set position.");
                        stream.Source.StopPlaying();
                    }

                    if (stream.TrackingEntity != default)
                        stream.Source.SetVelocity(stream.TrackingEntity.GlobalLinearVelocity());
                }
            }
        }
    }

    private static void StreamDone(PlayingStream stream)
    {
        stream.Source.Dispose();
        stream.Done = true;
    }

    #region Play AudioStream
    private bool TryGetAudio(string filename, [NotNullWhen(true)] out AudioResource? audio)
    {
        if (_resourceCache.TryGetResource<AudioResource>(new ResourcePath(filename), out audio))
            return true;

        Logger.Error($"Server tried to play audio file {filename} which does not exist.");
        return false;
    }

    private bool TryCreateAudioSource(AudioStream stream, [NotNullWhen(true)] out IClydeAudioSource? source)
    {
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
            return null;

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
            return null;

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
            return null;

        if (!source.SetPosition(fallbackCoordinates.Position))
        {
            source.Dispose();
            Logger.Warning($"Can't play positional audio \"{stream.Name}\", can't set position.");
            return null;
        }

        if (!coordinates.IsValid(EntityManager))
            coordinates = fallbackCoordinates;

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
        public EntityUid TrackingEntity = default!;
        public EntityCoordinates? TrackingCoordinates;
        public EntityCoordinates? TrackingFallbackCoordinates;
        public bool Done;
        public float Volume;

        /// <summary>
        /// Temporary holding value to determine if calculating occlusion for this stream is a good idea.
        /// Because some of this stuff is parallelized for performance, these can't be stackalloc'd arrays.
        /// </summary>
        public bool OcclusionValidTemporary;
        /// <summary>
        /// Temporary holding value containing the occlusion value of the stream.
        /// Because some of this stuff is parallelized for performance, these can't be stackalloc'd arrays.
        /// </summary>
        public float OcclusionTemporary;
        /// <summary>
        /// Temporary holding value containing the map coordinates of the stream.
        /// Because some of this stuff is parallelized for performance, these can't be stackalloc'd arrays.
        /// Note that if the map coordinates aren't available, this isn't updated.
        /// Only streams for which map coordinates are available go into the "valid" stackalloc'd array.
        /// (Occlusion uses the OcclusionValidTemporary field as it can't access stackalloc'd arrays.)
        /// </summary>
        public MapCoordinates MapCoordinatesTemporary;

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
        if (_resourceCache.TryGetResource<AudioResource>(new ResourcePath(filename), out var audio))
        {
            return Play(audio, entity, null, audioParams);
        }

        Logger.Error($"Server tried to play audio file {filename} which does not exist.");
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
        if (_resourceCache.TryGetResource<AudioResource>(new ResourcePath(filename), out var audio))
        {
            return Play(audio, uid, null, audioParams);
        }
        return null;
    }

    /// <inheritdoc />
    public override IPlayingAudioStream? PlayEntity(string filename, EntityUid recipient, EntityUid uid, AudioParams? audioParams = null)
    {
        if (_resourceCache.TryGetResource<AudioResource>(new ResourcePath(filename), out var audio))
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
