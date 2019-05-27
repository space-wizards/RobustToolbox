using Robust.Client.Audio;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using System.Collections.Generic;
using System.IO;

namespace Robust.Client.GameObjects.EntitySystems
{
    public class AudioSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IResourceCache resourceCache;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IClyde _clyde;
#pragma warning restore 649

        private readonly List<PlayingStream> PlayingClydeStreams = new List<PlayingStream>();

        public override void RegisterMessageTypes()
        {
            base.RegisterMessageTypes();

            RegisterMessageType<PlayAudioEntityMessage>();
            RegisterMessageType<PlayAudioGlobalMessage>();
            RegisterMessageType<PlayAudioPositionalMessage>();
        }

        public override void FrameUpdate(float frameTime)
        {
            // Update positions of streams every frame.
            foreach (var stream in PlayingClydeStreams)
            {
                if (!stream.Source.IsPlaying)
                {
                    stream.Source.Dispose();
                    stream.Done = true;
                    continue;
                }

                if (stream.TrackingCoordinates != null)
                {
                    stream.Source.SetPosition(stream.TrackingCoordinates.Value.ToWorld(_mapManager).Position);
                }
                else if (stream.TrackingEntity != null)
                {
                    stream.Source.SetPosition(stream.TrackingEntity.Transform.WorldPosition);
                }
            }

            PlayingClydeStreams.RemoveAll(p => p.Done);
        }

        /// <summary>
        ///     Play an audio file globally, without position.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="audioParams"></param>
        public void Play(string filename, AudioParams? audioParams = null)
        {
            Play(resourceCache.GetResource<AudioResource>(new ResourcePath(filename)), audioParams);
        }

        /// <summary>
        ///     Play an audio stream globally, without position.
        /// </summary>
        /// <param name="stream">The audio stream to play.</param>
        /// <param name="audioParams"></param>
        public void Play(AudioStream stream, AudioParams? audioParams = null)
        {
            var source = _clyde.CreateAudioSource(stream);
            if (audioParams.HasValue)
            {
                source.SetPitch(audioParams.Value.PitchScale);
                source.SetVolume(audioParams.Value.Volume);
            }

            source.SetGlobal();
            source.StartPlaying();
            var playing = new PlayingStream
            {
                Source = source,
            };
            PlayingClydeStreams.Add(playing);
        }

        /// <summary>
        ///     Play an audio file following an entity.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="entity">The entity "emitting" the audio.</param>
        /// <param name="audioParams"></param>
        public void Play(string filename, IEntity entity, AudioParams? audioParams = null)
        {
            Play(resourceCache.GetResource<AudioResource>(new ResourcePath(filename)), entity, audioParams);
        }

        /// <summary>
        ///     Play an audio stream following an entity.
        /// </summary>
        /// <param name="stream">The audio stream to play.</param>
        /// <param name="entity">The entity "emitting" the audio.</param>
        /// <param name="audioParams"></param>
        public void Play(AudioStream stream, IEntity entity, AudioParams? audioParams = null)
        {
            var source = _clyde.CreateAudioSource(stream);
            source.SetPosition(entity.Transform.WorldPosition);
            if (audioParams.HasValue)
            {
                source.SetPitch(audioParams.Value.PitchScale);
                source.SetVolume(audioParams.Value.Volume);
            }

            source.StartPlaying();
            var playing = new PlayingStream
            {
                Source = source,
                TrackingEntity = entity,
            };
            PlayingClydeStreams.Add(playing);
        }

        /// <summary>
        ///     Play an audio file at a static position.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="coordinates">The coordinates at which to play the audio.</param>
        /// <param name="audioParams"></param>
        public void Play(string filename, GridCoordinates coordinates, AudioParams? audioParams = null)
        {
            Play(resourceCache.GetResource<AudioResource>(new ResourcePath(filename)), coordinates, audioParams);
        }

        /// <summary>
        ///     Play an audio stream at a static position.
        /// </summary>
        /// <param name="stream">The audio stream to play.</param>
        /// <param name="coordinates">The coordinates at which to play the audio.</param>
        /// <param name="audioParams"></param>
        public void Play(AudioStream stream, GridCoordinates coordinates, AudioParams? audioParams = null)
        {
            var source = _clyde.CreateAudioSource(stream);
            source.SetPosition(coordinates.ToWorld(_mapManager).Position);
            if (audioParams.HasValue)
            {
                source.SetPitch(audioParams.Value.PitchScale);
                source.SetVolume(audioParams.Value.Volume);
            }

            source.StartPlaying();
            var playing = new PlayingStream
            {
                Source = source,
                TrackingCoordinates = coordinates
            };
            PlayingClydeStreams.Add(playing);
        }

        public override void HandleNetMessage(INetChannel channel, EntitySystemMessage message)
        {
            base.HandleNetMessage(channel, message);

            if (!(message is AudioMessage msg))
            {
                return;
            }

            try
            {
                switch (message)
                {
                    case PlayAudioGlobalMessage globalmsg:
                        Play(globalmsg.FileName, globalmsg.AudioParams);
                        break;

                    case PlayAudioEntityMessage entitymsg:
                        if (!EntityManager.TryGetEntity(entitymsg.EntityUid, out var entity))
                        {
                            Logger.Error(
                                $"Server tried to play audio file {entitymsg.FileName} on entity {entitymsg.EntityUid} which does not exist.");
                            break;
                        }

                        Play(entitymsg.FileName, entity, entitymsg.AudioParams);
                        break;

                    case PlayAudioPositionalMessage posmsg:
                        Play(posmsg.FileName, posmsg.Coordinates, posmsg.AudioParams);
                        break;
                }
            }
            catch (FileNotFoundException)
            {
                Logger.Error($"Server tried to play audio file {msg.FileName} which does not exist.");
            }
        }

        private class PlayingStream
        {
            public IClydeAudioSource Source;
            public IEntity TrackingEntity;
            public GridCoordinates? TrackingCoordinates;
            public bool Done;
        }
    }
}
