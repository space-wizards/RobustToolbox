using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
{
    [UsedImplicitly]
    public class AudioSystem : SharedAudioSystem, IAudioSystem
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IClydeAudio _clyde = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly SharedPhysicsSystem _broadPhaseSystem = default!;

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

            var stream = (PlayingStream?) Play(ev.FileName, ev.Coordinates, ev.FallbackCoordinates, ev.AudioParams);
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
                (PlayingStream?) Play(ev.FileName, entity, ev.FallbackCoordinates, ev.AudioParams)
                : (PlayingStream?) Play(ev.FileName, ev.Coordinates, ev.FallbackCoordinates, ev.AudioParams);

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
                var ourPos = _eyeManager.CurrentEye.Position.Position;

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

                    // TODO Remove when coordinates can't be NaN
                    if (mapPos == null || !float.IsFinite(mapPos.Value.X) || !float.IsFinite(mapPos.Value.Y))
                        mapPos = stream.TrackingFallbackCoordinates?.ToMap(_entityManager);

                    if (mapPos != null)
                    {
                        var pos = mapPos.Value;
                        if (pos.MapId != _eyeManager.CurrentMap)
                        {
                            stream.Source.SetVolume(-10000000);
                        }
                        else
                        {
                            var sourceRelative = ourPos - pos.Position;
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

                            var distance = MathF.Max(stream.ReferenceDistance, MathF.Min(sourceRelative.Length, stream.MaxDistance));
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
                                    gain = stream.ReferenceDistance /
                                           (stream.ReferenceDistance + stream.RolloffFactor * (distance - stream.ReferenceDistance));

                                    break;
                                case Attenuation.LinearDistanceClamped:
                                case Attenuation.LinearDistance:
                                    gain = 1f - stream.RolloffFactor * (distance - stream.ReferenceDistance) /
                                        (stream.MaxDistance - stream.ReferenceDistance);

                                    break;
                                case Attenuation.ExponentDistanceClamped:
                                case Attenuation.ExponentDistance:
                                    gain = MathF.Pow((distance / stream.ReferenceDistance),
                                        (-stream.RolloffFactor));
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException($"No implemented attenuation for {stream.Attenuation.ToString()}");
                            }

                            var volume = MathF.Pow(10, stream.Volume / 10);
                            var actualGain = MathF.Max(0f, volume * gain);

                            stream.Source.SetVolumeDirect(actualGain);
                            stream.Source.SetOcclusion(occlusion);
                        }

                        SetAudioPos(stream, stream.Attenuation != Attenuation.NoAttenuation ? pos.Position : ourPos);

                        void SetAudioPos(PlayingStream stream, Vector2 pos)
                        {
                            if (!stream.Source.SetPosition(pos))
                            {
                                Logger.Warning("Interrupting positional audio, can't set position.");
                                stream.Source.StopPlaying();
                            }
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
        ///     Play an audio file following an entity.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="entity">The entity "emitting" the audio.</param>
        /// <param name="fallbackCoordinates">The map or grid coordinates at which to play the audio when entity is invalid.</param>
        /// <param name="audioParams"></param>
        private IPlayingAudioStream? Play(string filename, IEntity entity, EntityCoordinates fallbackCoordinates,
            AudioParams? audioParams = null, bool addInRange = true)
        {
            if (_resourceCache.TryGetResource<AudioResource>(new ResourcePath(filename), out var audio))
            {
                return Play(audio, entity, fallbackCoordinates, audioParams);
            }

            Logger.Error($"Server tried to play audio file {filename} which does not exist.");
            return default;
        }

        /// <summary>
        ///     Play an audio stream following an entity.
        /// </summary>
        /// <param name="stream">The audio stream to play.</param>
        /// <param name="entity">The entity "emitting" the audio.</param>
        /// <param name="fallbackCoordinates">The map or grid coordinates at which to play the audio when entity is invalid.</param>
        /// <param name="audioParams"></param>
        private IPlayingAudioStream? Play(AudioStream stream, IEntity entity, EntityCoordinates fallbackCoordinates,
            AudioParams? audioParams = null, bool addInRange = true)
        {
            var source = _clyde.CreateAudioSource(stream);
            if (!source.SetPosition(entity.Transform.WorldPosition))
            {
                return Play(stream, fallbackCoordinates, fallbackCoordinates, audioParams);
            }

            ApplyAudioParams(audioParams, source);

            source.StartPlaying();
            var playing = new PlayingStream
            {
                Source = source,
                TrackingEntity = entity,
                TrackingFallbackCoordinates = fallbackCoordinates != EntityCoordinates.Invalid ? fallbackCoordinates : null,
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
        ///     Play an audio file at a static position.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="coordinates">The coordinates at which to play the audio.</param>
        /// <param name="fallbackCoordinates">The map or grid coordinates at which to play the audio when coordinates are invalid.</param>
        /// <param name="audioParams"></param>
        private IPlayingAudioStream? Play(string filename, EntityCoordinates coordinates, EntityCoordinates fallbackCoordinates,
            AudioParams? audioParams = null, bool addInRange = true)
        {
            if (_resourceCache.TryGetResource<AudioResource>(new ResourcePath(filename), out var audio))
            {
                return Play(audio, coordinates, fallbackCoordinates, audioParams);
            }

            Logger.Error($"Server tried to play audio file {filename} which does not exist.");
            return default;
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
            var source = _clyde.CreateAudioSource(stream);
            if (!source.SetPosition(fallbackCoordinates.Position))
            {
                source.Dispose();
                Logger.Warning($"Can't play positional audio \"{stream.Name}\", can't set position.");
                return null;
            }

            if (!coordinates.IsValid(_entityManager))
            {
                coordinates = fallbackCoordinates;
            }

            ApplyAudioParams(audioParams, source);

            source.StartPlaying();
            var playing = new PlayingStream
            {
                Source = source,
                TrackingCoordinates = coordinates,
                TrackingFallbackCoordinates = fallbackCoordinates != EntityCoordinates.Invalid ? fallbackCoordinates : null,
                Attenuation = audioParams?.Attenuation ?? Attenuation.Default,
                MaxDistance = audioParams?.MaxDistance ?? float.MaxValue,
                ReferenceDistance = audioParams?.ReferenceDistance ?? 1f,
                RolloffFactor = audioParams?.RolloffFactor ?? 1f,
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
            source.SetRolloffFactor(audioParams.Value.RolloffFactor);
            source.SetMaxDistance(audioParams.Value.MaxDistance);
            source.SetReferenceDistance(audioParams.Value.ReferenceDistance);
            source.SetPlaybackPosition(audioParams.Value.PlayOffsetSeconds);
            source.IsLooping = audioParams.Value.Loop;
        }

        private class PlayingStream : IPlayingAudioStream
        {
            public uint? NetIdentifier;
            public IClydeAudioSource Source = default!;
            public IEntity TrackingEntity = default!;
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
        public int OcclusionCollisionMask { get; set; }

        /// <inheritdoc />
        public IPlayingAudioStream? Play(Filter playerFilter, string filename, AudioParams? audioParams = null)
        {
            return Play(filename, audioParams);
        }

        /// <inheritdoc />
        public IPlayingAudioStream? Play(Filter playerFilter, string filename, IEntity entity, AudioParams? audioParams = null,
            bool addInRange = true)
        {
            return Play(filename, entity, GetFallbackCoordinates(entity.Transform.MapPosition), audioParams);
        }

        public IPlayingAudioStream? Play(Filter playerFilter, string filename, EntityUid uid, AudioParams? audioParams = null,
            bool addInRange = true)
        {
            return EntityManager.TryGetEntity(uid, out var entity)
                ? Play(filename, entity, GetFallbackCoordinates(entity.Transform.MapPosition), audioParams) : null;
        }

        /// <inheritdoc />
        public IPlayingAudioStream? Play(Filter playerFilter, string filename, EntityCoordinates coordinates, AudioParams? audioParams = null,
            bool addInRange = true)
        {
            return Play(filename, coordinates, GetFallbackCoordinates(coordinates.ToMap(_entityManager)), audioParams);
        }
    }
}
