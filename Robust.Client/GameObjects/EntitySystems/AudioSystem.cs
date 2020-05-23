using System;
using Robust.Client.Audio;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using System.Collections.Generic;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Maths;

namespace Robust.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class AudioSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IResourceCache _resourceCache;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IClydeAudio _clyde;
        [Dependency] private readonly IEyeManager _eyeManager;
#pragma warning restore 649

        private readonly List<PlayingStream> _playingClydeStreams = new List<PlayingStream>();

        /// <inheritdoc />
        public override void Initialize()
        {
            SubscribeNetworkEvent<PlayAudioEntityMessage>(PlayAudioEntityHandler);
            SubscribeNetworkEvent<PlayAudioGlobalMessage>(PlayAudioGlobalHandler);
            SubscribeNetworkEvent<PlayAudioPositionalMessage>(PlayAudioPositionalHandler);
        }

        private void PlayAudioPositionalHandler(PlayAudioPositionalMessage ev)
        {
            if (!_mapManager.GridExists(ev.Coordinates.GridID))
            {
                Logger.Error($"Server tried to play sound on grid {ev.Coordinates.GridID.Value}, which does not exist. Ignoring.");
                return;
            }

            Play(ev.FileName, ev.Coordinates, ev.AudioParams);
        }

        private void PlayAudioGlobalHandler(PlayAudioGlobalMessage ev)
        {
            Play(ev.FileName, ev.AudioParams);
        }

        private void PlayAudioEntityHandler(PlayAudioEntityMessage ev)
        {
            if (!EntityManager.TryGetEntity(ev.EntityUid, out var entity))
            {
                Logger.Error($"Server tried to play audio file {ev.FileName} on entity {ev.EntityUid} which does not exist.");
                return;
            }

            Play(ev.FileName, entity, ev.AudioParams);
        }

        public override void FrameUpdate(float frameTime)
        {
            var currentMap = _eyeManager.CurrentMap;

            // Update positions of streams every frame.
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
                    mapPos = stream.TrackingCoordinates.Value.ToMap(_mapManager);
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
                    if (pos.MapId != currentMap)
                    {
                        stream.Source.SetVolume(-10000000);
                    }
                    else
                    {
                        var occlusion = 0;
                        if ((_eyeManager.CurrentEye.Position.Position - pos.Position).Length > 0)
                        {
                            IoCManager.Resolve<IPhysicsManager>().IntersectRayWithPredicate(
                                pos.MapId,
                                new CollisionRay(
                                    pos.Position,
                                    (_eyeManager.CurrentEye.Position.Position - pos.Position).Normalized,
                                    (int) (CollisionGroup.Impassable)),
                                (pos.Position - _eyeManager.CurrentEye.Position.Position).Length,
                                (e) => { occlusion++; return false; }, false);
                        }
                        stream.Source.SetVolume(stream.Volume);
                        stream.Source.SetOcclusion(occlusion);
                    }

                    if (!stream.Source.SetPosition(pos.Position))
                    {
                        Logger.Warning("Interrupting positional audio, can't set position.");
                        stream.Source.StopPlaying();
                    }
                }
            }

            _playingClydeStreams.RemoveAll(p => p.Done);
        }

        private static void StreamDone(PlayingStream stream)
        {
            stream.Source.Dispose();
            stream.Done = true;
            stream.DoPlaybackDone();
        }

        /// <summary>
        ///     Play an audio file globally, without position.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="audioParams"></param>
        public IPlayingAudioStream Play(string filename, AudioParams? audioParams = null)
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
        public IPlayingAudioStream Play(AudioStream stream, AudioParams? audioParams = null)
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
        public IPlayingAudioStream Play(string filename, IEntity entity, AudioParams? audioParams = null)
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
        [CanBeNull]
        public IPlayingAudioStream Play(AudioStream stream, IEntity entity, AudioParams? audioParams = null)
        {
            var source = _clyde.CreateAudioSource(stream);
            if (!source.SetPosition(entity.Transform.WorldPosition))
            {
                Logger.Warning("Can't play positional audio, can't set position.");
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
        [CanBeNull]
        public IPlayingAudioStream Play(string filename, GridCoordinates coordinates, AudioParams? audioParams = null)
        {
            if(_resourceCache.TryGetResource<AudioResource>(new ResourcePath(filename), out var audio))
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
        [CanBeNull]
        public IPlayingAudioStream Play(AudioStream stream, GridCoordinates coordinates,
            AudioParams? audioParams = null)
        {
            var source = _clyde.CreateAudioSource(stream);
            if (!source.SetPosition(coordinates.ToMapPos(_mapManager)))
            {
                Logger.Warning("Can't play positional audio, can't set position.");
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
            public IClydeAudioSource Source;
            public IEntity TrackingEntity;
            public GridCoordinates? TrackingCoordinates;
            public bool Done;
            public float Volume;

            public void Stop()
            {
                Source.StopPlaying();
            }

            public event Action PlaybackDone;

            public void DoPlaybackDone()
            {
                PlaybackDone?.Invoke();
            }
        }
    }

    public interface IPlayingAudioStream
    {
        void Stop();

        event Action PlaybackDone;
    }
}
