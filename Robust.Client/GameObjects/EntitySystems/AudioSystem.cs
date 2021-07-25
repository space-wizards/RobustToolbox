using System.Collections.Generic;
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
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
{
    [UsedImplicitly]
    public class AudioSystem : EntitySystem, IAudioSystem
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IClydeAudio _clyde = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private SharedBroadphaseSystem _broadPhaseSystem = default!;

        private readonly List<PlayingStream> _playingClydeStreams = new();

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<PlayAudioEntityMessage>(PlayAudioEntityHandler);
            SubscribeNetworkEvent<PlayAudioGlobalMessage>(PlayAudioGlobalHandler);
            SubscribeNetworkEvent<PlayAudioPositionalMessage>(PlayAudioPositionalHandler);
            SubscribeNetworkEvent<StopAudioMessageClient>(StopAudioMessageHandler);

            SubscribeLocalEvent<SoundSystem.QueryAudioSystem>((ev => ev.Audio = this));
            _broadPhaseSystem = Get<SharedBroadphaseSystem>();
        }

        private void StopAudioMessageHandler(StopAudioMessageClient ev)
        {
            var stream = _playingClydeStreams.Find(p => p.NetIdentifier == ev.Identifier);
            if (stream == null)
            {
                return;
            }

            StreamDone(stream);
            _playingClydeStreams.Remove(stream);
        }

        private void PlayAudioPositionalHandler(PlayAudioPositionalMessage ev)
        {
            var mapId = ev.Coordinates.GetMapId(_entityManager);

            if (!_mapManager.MapExists(mapId))
            {
                Logger.Error(
                    $"Server tried to play sound on map {mapId}, which does not exist. Ignoring.");
                return;
            }

            var stream = (PlayingStream?) Play(ev.FileName, ev.Coordinates, ev.AudioParams);
            if (stream != null)
            {
                stream.NetIdentifier = ev.Identifier;
            }
        }

        private void PlayAudioGlobalHandler(PlayAudioGlobalMessage ev)
        {
            var stream = (PlayingStream?) Play(ev.FileName, ev.AudioParams);
            if (stream != null)
            {
                stream.NetIdentifier = ev.Identifier;
            }
        }

        private void PlayAudioEntityHandler(PlayAudioEntityMessage ev)
        {
            var stream = EntityManager.TryGetEntity(ev.EntityUid, out var entity) ?
                (PlayingStream?) Play(ev.FileName, entity, ev.AudioParams)
                : (PlayingStream?) Play(ev.FileName, ev.Coordinates, ev.AudioParams);

            if (stream != null)
            {
                stream.NetIdentifier = ev.Identifier;
            }

        }

        public override void FrameUpdate(float frameTime)
        {
            // Update positions of streams every frame.
            try
            {
                foreach (var stream in _playingClydeStreams)
                {
                    if (!stream.Source.IsPlaying)
                    {
                        StreamDone(stream);
                        continue;
                    }

                    MapCoordinates? mapPos = null;
                    if (stream.TrackingCoordinates != null)
                    {
                        var coords = stream.TrackingCoordinates.Value;
                        if (_mapManager.MapExists(coords.GetMapId(_entityManager)))
                        {
                            mapPos = stream.TrackingCoordinates.Value.ToMap(_entityManager);
                        }
                        else
                        {
                            // Map no longer exists, delete stream.
                            StreamDone(stream);
                            continue;
                        }
                    }
                    else if (stream.TrackingEntity != null)
                    {
                        if (stream.TrackingEntity.Deleted)
                        {
                            StreamDone(stream);
                            continue;
                        }

                        mapPos = stream.TrackingEntity.Transform.MapPosition;
                    }

                    if (mapPos != null)
                    {
                        var pos = mapPos.Value;
                        if (pos.MapId != _eyeManager.CurrentMap)
                        {
                            stream.Source.SetVolume(-10000000);
                        }
                        else
                        {
                            var sourceRelative = _eyeManager.CurrentEye.Position.Position - pos.Position;
                            var occlusion = 0f;
                            if (sourceRelative.Length > 0)
                            {
                                occlusion = _broadPhaseSystem.IntersectRayPenetration(
                                    pos.MapId,
                                    new CollisionRay(
                                        pos.Position,
                                        sourceRelative.Normalized,
                                        OcclusionCollisionMask),
                                    sourceRelative.Length,
                                    stream.TrackingEntity);
                            }

                            stream.Source.SetVolume(stream.Volume);
                            stream.Source.SetOcclusion(occlusion);
                        }

                        if (!stream.Source.SetPosition(pos.Position))
                        {
                            Logger.Warning("Interrupting positional audio, can't set position.");
                            stream.Source.StopPlaying();
                        }

                        if (stream.TrackingEntity != null)
                        {
                            stream.Source.SetVelocity(stream.TrackingEntity.GlobalLinearVelocity());
                        }

                    }
                }
            }
            finally
            {
                // if this doesn't get ran (exception...) then the list can fill up with disposed garbage.
                // that will then throw on IsPlaying.
                // meaning it'll break the entire audio system.
                _playingClydeStreams.RemoveAll(p => p.Done);
            }
        }

        private static void StreamDone(PlayingStream stream)
        {
            stream.Source.Dispose();
            stream.Done = true;
        }

        /// <summary>
        ///     Play an audio file globally, without position.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="audioParams"></param>
        private IPlayingAudioStream? Play(string filename, AudioParams? audioParams = null)
        {
            if (_resourceCache.TryGetResource<AudioResource>(new ResourcePath(filename), out var audio))
            {
                return Play(audio, audioParams);
            }

            Logger.Error($"Server tried to play audio file {filename} which does not exist.");
            return default;
        }

        /// <summary>
        ///     Play an audio stream globally, without position.
        /// </summary>
        /// <param name="stream">The audio stream to play.</param>
        /// <param name="audioParams"></param>
        private IPlayingAudioStream Play(AudioStream stream, AudioParams? audioParams = null)
        {
            var source = _clyde.CreateAudioSource(stream);
            ApplyAudioParams(audioParams, source);

            source.SetGlobal();
            source.StartPlaying();
            var playing = new PlayingStream
            {
                Source = source,
                Volume = audioParams?.Volume ?? 0
            };
            _playingClydeStreams.Add(playing);
            return playing;
        }

        /// <summary>
        ///     Play an audio file following an entity.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="entity">The entity "emitting" the audio.</param>
        /// <param name="audioParams"></param>
        private IPlayingAudioStream? Play(string filename, IEntity entity, AudioParams? audioParams = null)
        {
            if (_resourceCache.TryGetResource<AudioResource>(new ResourcePath(filename), out var audio))
            {
                return Play(audio, entity, audioParams);
            }

            Logger.Error($"Server tried to play audio file {filename} which does not exist.");
            return default;
        }

        /// <summary>
        ///     Play an audio stream following an entity.
        /// </summary>
        /// <param name="stream">The audio stream to play.</param>
        /// <param name="entity">The entity "emitting" the audio.</param>
        /// <param name="audioParams"></param>
        private IPlayingAudioStream? Play(AudioStream stream, IEntity entity, AudioParams? audioParams = null)
        {
            var source = _clyde.CreateAudioSource(stream);
            if (!source.SetPosition(entity.Transform.WorldPosition))
            {
                source.Dispose();
                Logger.Warning($"Can't play positional audio \"{stream.Name}\", can't set position.");
                return null;
            }

            ApplyAudioParams(audioParams, source);

            source.StartPlaying();
            var playing = new PlayingStream
            {
                Source = source,
                TrackingEntity = entity,
                Volume = audioParams?.Volume ?? 0
            };
            _playingClydeStreams.Add(playing);
            return playing;
        }

        /// <summary>
        ///     Play an audio file at a static position.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="coordinates">The coordinates at which to play the audio.</param>
        /// <param name="audioParams"></param>
        private IPlayingAudioStream? Play(string filename, EntityCoordinates coordinates, AudioParams? audioParams = null)
        {
            if (_resourceCache.TryGetResource<AudioResource>(new ResourcePath(filename), out var audio))
            {
                return Play(audio, coordinates, audioParams);
            }

            Logger.Error($"Server tried to play audio file {filename} which does not exist.");
            return default;
        }

        /// <summary>
        ///     Play an audio stream at a static position.
        /// </summary>
        /// <param name="stream">The audio stream to play.</param>
        /// <param name="coordinates">The coordinates at which to play the audio.</param>
        /// <param name="audioParams"></param>
        private IPlayingAudioStream? Play(AudioStream stream, EntityCoordinates coordinates,
            AudioParams? audioParams = null)
        {
            var source = _clyde.CreateAudioSource(stream);
            if (!source.SetPosition(coordinates.ToMapPos(EntityManager)))
            {
                source.Dispose();
                Logger.Warning($"Can't play positional audio \"{stream.Name}\", can't set position.");
                return null;
            }

            ApplyAudioParams(audioParams, source);

            source.StartPlaying();
            var playing = new PlayingStream
            {
                Source = source,
                TrackingCoordinates = coordinates,
                Volume = audioParams?.Volume ?? 0
            };
            _playingClydeStreams.Add(playing);
            return playing;
        }

        private static void ApplyAudioParams(AudioParams? audioParams, IClydeAudioSource source)
        {
            if (!audioParams.HasValue)
            {
                return;
            }

            source.SetPitch(audioParams.Value.PitchScale);
            source.SetVolume(audioParams.Value.Volume);
            source.SetPlaybackPosition(audioParams.Value.PlayOffsetSeconds);
            source.IsLooping = audioParams.Value.Loop;
        }

        private class PlayingStream : IPlayingAudioStream
        {
            public uint? NetIdentifier;
            public IClydeAudioSource Source = default!;
            public IEntity TrackingEntity = default!;
            public EntityCoordinates? TrackingCoordinates;
            public bool Done;
            public float Volume;

            public void Stop()
            {
                Source.StopPlaying();
            }
        }

        /// <inheritdoc />
        public int DefaultSoundRange => 25;

        /// <inheritdoc />
        public int OcclusionCollisionMask { get; set; }

        /// <inheritdoc />
        public IPlayingAudioStream? Play(Filter playerFilter, string filename, AudioParams? audioParams = null)
        {
            return Play(filename, audioParams);
        }

        /// <inheritdoc />
        public IPlayingAudioStream? Play(Filter playerFilter, string filename, IEntity entity, AudioParams? audioParams = null)
        {
            return Play(filename, entity, audioParams);
        }

        /// <inheritdoc />
        public IPlayingAudioStream? Play(Filter playerFilter, string filename, EntityCoordinates coordinates, AudioParams? audioParams = null)
        {
            return Play(filename, coordinates, audioParams);
        }
    }
}
