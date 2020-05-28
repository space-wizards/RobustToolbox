using System;
using System.Collections.Generic;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Robust.Server.GameObjects.EntitySystems
{
    public class AudioSystem : EntitySystem
    {

        private static List<ushort> _streams = new List<ushort>(ushort.MaxValue);
        private RobustRandom _random = new RobustRandom();
        private static ushort _previousRandom = 0;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<StopAudioMessageServer>(StopAudioMessageHandler);

        }

        private void StopAudioMessageHandler(StopAudioMessageServer msg, EntitySessionEventArgs args)
        {
            _streams.RemoveAll(s => s == msg.Identifier);
        }

        private ushort CacheIdentifier()
        {
            var streamID = (ushort)_random.Next(ushort.MaxValue);
            while (streamID == _previousRandom)
            {
                streamID = (ushort)_random.Next(ushort.MaxValue);
                break;
            }
            _streams.Add(streamID);
            _previousRandom = streamID;
            return streamID;
        }

        /// <summary>
        ///     Play an audio file globally, without position.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="audioParams"></param>
        public ushort PlayGlobal(string filename, AudioParams? audioParams = null)
        {
            var id = CacheIdentifier();
            var msg = new PlayAudioGlobalMessage
            {
                FileName = filename,
                AudioParams = audioParams ?? AudioParams.Default,
                Identifier = id
            };
            RaiseNetworkEvent(msg);
            return id;
        }

        /// <summary>
        ///     Play an audio file following an entity.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="entity">The entity "emitting" the audio.</param>
        /// <param name="audioParams"></param>
        public ushort PlayFromEntity(string filename, IEntity entity, AudioParams? audioParams = null)
        {
            var id = CacheIdentifier();
            var msg = new PlayAudioEntityMessage
            {
                FileName = filename,
                EntityUid = entity.Uid,
                AudioParams = audioParams ?? AudioParams.Default,
                Identifier = id
            };
            RaiseNetworkEvent(msg);
            return id;
        }

        /// <summary>
        ///     Play an audio file at a static position.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="coordinates">The coordinates at which to play the audio.</param>
        /// <param name="audioParams"></param>
        public ushort PlayAtCoords(string filename, GridCoordinates coordinates, AudioParams? audioParams = null)
        {
            var id = CacheIdentifier();
            var msg = new PlayAudioPositionalMessage
            {
                FileName = filename,
                Coordinates = coordinates,
                AudioParams = audioParams ?? AudioParams.Default,
                Identifier = id
            };
            RaiseNetworkEvent(msg);
            return id;
        }

        /// <summary>
        ///     Stop an audio event.
        /// </summary>
        /// <param name="identifier">The ushort returned by the Play function used to start playing a sound.</param>
        public void Stop(ushort identifier)
        {
            var msg = new StopAudioMessageClient
            {
                Identifier = identifier
            };

            RaiseNetworkEvent(msg);
            _streams.RemoveAll(s => s == identifier);
        }

        #region DEPRECATED
        /// <summary>
        ///     Play an audio file globally, without position.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="audioParams"></param>
        [Obsolete("Deprecated. Use PlayGlobal instead.")]
        public void Play(string filename, AudioParams? audioParams = null)
        {
            var msg = new PlayAudioGlobalMessage
            {
                FileName = filename,
                AudioParams = audioParams ?? AudioParams.Default
            };
            RaiseNetworkEvent(msg);
        }


        /// <summary>
        ///     Play an audio file following an entity.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="entity">The entity "emitting" the audio.</param>
        /// <param name="audioParams"></param>
        [Obsolete("Deprecated. Use PlayFromEntity instead.")]
        public void Play(string filename, IEntity entity, AudioParams? audioParams = null)
        {
            var msg = new PlayAudioEntityMessage
            {
                FileName = filename,
                EntityUid = entity.Uid,
                AudioParams = audioParams ?? AudioParams.Default
            };
            RaiseNetworkEvent(msg);
        }

        /// <summary>
        ///     Play an audio file at a static position.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="coordinates">The coordinates at which to play the audio.</param>
        /// <param name="audioParams"></param>
        [Obsolete("Deprecated. Use PlayAtCoords instead.")]
        public void Play(string filename, GridCoordinates coordinates, AudioParams? audioParams = null)
        {
            var msg = new PlayAudioPositionalMessage
            {
                FileName = filename,
                Coordinates = coordinates,
                AudioParams = audioParams ?? AudioParams.Default
            };
            RaiseNetworkEvent(msg);
        }

        #endregion
    }
}
