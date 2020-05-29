using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace Robust.Server.GameObjects.EntitySystems
{
    public class AudioSystem : EntitySystem
    {
        private static byte _streamIndex = byte.MaxValue - 1;

        public class AudioSource
        {
            private readonly byte Id;
            private readonly AudioSystem _audioSystem;

            public AudioSource(AudioSystem parent, byte identifier)
            {
                _audioSystem = parent;
                Id = identifier;
            }
            public void Stop()
            {
                _audioSystem.InternalStop(Id);
            }
        }

        private void InternalStop(byte id)
        {
            var msg = new StopAudioMessageClient
            {
                Identifier = id
            };

            RaiseNetworkEvent(msg);
        }

        private byte CacheIdentifier()
        {
            if (_streamIndex >= byte.MaxValue)
            {
                _streamIndex = 0;
            }
            return _streamIndex++;
        }

        /// <summary>
        ///     Play an audio file globally, without position.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="audioParams"></param>
        public AudioSource PlayGlobal(string filename, AudioParams? audioParams = null)
        {
            var id = CacheIdentifier();
            var msg = new PlayAudioGlobalMessage
            {
                FileName = filename,
                AudioParams = audioParams ?? AudioParams.Default,
                Identifier = id
            };
            RaiseNetworkEvent(msg);
            var src = new AudioSource(this, id);
            return src;

        }

        /// <summary>
        ///     Play an audio file following an entity.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="entity">The entity "emitting" the audio.</param>
        /// <param name="audioParams"></param>
        public AudioSource PlayFromEntity(string filename, IEntity entity, AudioParams? audioParams = null)
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
            var src = new AudioSource(this, id);
            return src;
        }

        /// <summary>
        ///     Play an audio file at a static position.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="coordinates">The coordinates at which to play the audio.</param>
        /// <param name="audioParams"></param>
        public AudioSource PlayAtCoords(string filename, GridCoordinates coordinates, AudioParams? audioParams = null)
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
            var src = new AudioSource(this, id);
            return src;
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
