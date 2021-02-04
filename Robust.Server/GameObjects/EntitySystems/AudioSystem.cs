using System;
using System.Collections.Generic;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using static Robust.Server.GameObjects.EntitySystems.AudioSystem;

namespace Robust.Server.GameObjects.EntitySystems
{
    public class AudioSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public const int AudioDistanceRange = 25;

        private uint _streamIndex;

        public class AudioSourceServer
        {
            private readonly uint _id;
            private readonly AudioSystem _audioSystem;
            private readonly IEnumerable<IPlayerSession>? _sessions;

            internal AudioSourceServer(AudioSystem parent, uint identifier, IEnumerable<IPlayerSession>? sessions = null)
            {
                _audioSystem = parent;
                _id = identifier;
                _sessions = sessions;
            }
            public void Stop()
            {
                _audioSystem.InternalStop(_id, _sessions);
            }
        }

        private void InternalStop(uint id, IEnumerable<IPlayerSession>? sessions = null)
        {
            var msg = new StopAudioMessageClient
            {
                Identifier = id
            };

            if (sessions == null)
                RaiseNetworkEvent(msg);
            else
            {
                foreach (var session in sessions)
                {
                    RaiseNetworkEvent(msg, session.ConnectedClient);
                }
            }
        }

        private uint CacheIdentifier()
        {
            return unchecked(_streamIndex++);
        }

        /// <summary>
        ///     Play an audio file globally, without position.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="audioParams"></param>
        /// <param name="predicate">The predicate that will be used to send the audio to players, or null to send to everyone.</param>
        /// <param name="excludedSession">Session that won't receive the audio message.</param>
        public AudioSourceServer PlayGlobal(string filename, AudioParams? audioParams = null, Func<IPlayerSession, bool>? predicate = null, IPlayerSession? excludedSession = null)
        {
            var id = CacheIdentifier();
            var msg = new PlayAudioGlobalMessage
            {
                FileName = filename,
                AudioParams = audioParams ?? AudioParams.Default,
                Identifier = id
            };

            if (predicate == null && excludedSession == null)
            {
                RaiseNetworkEvent(msg);
                return new AudioSourceServer(this, id);
            }

            var players = predicate != null ? _playerManager.GetPlayersBy(predicate) : _playerManager.GetAllPlayers();

            for (var i = players.Count - 1; i >= 0; i--)
            {
                var player = players[i];
                if (player == excludedSession)
                {
                    players.RemoveAt(i);
                    continue;
                }

                RaiseNetworkEvent(msg, player.ConnectedClient);
            }

            return new AudioSourceServer(this, id, players);

        }

        /// <summary>
        ///     Play an audio file following an entity.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="entity">The entity "emitting" the audio.</param>
        /// <param name="audioParams"></param>
        /// <param name="range">The max range at which the audio will be heard. Less than or equal to 0 to send to every player.</param>
        /// <param name="excludedSession">Sessions that won't receive the audio message.</param>
        public AudioSourceServer PlayFromEntity(string filename, IEntity entity, AudioParams? audioParams = null, int range = AudioDistanceRange, IPlayerSession? excludedSession = null)
        {
            var id = CacheIdentifier();

            var msg = new PlayAudioEntityMessage
            {
                FileName = filename,
                Coordinates = entity.Transform.Coordinates,
                EntityUid = entity.Uid,
                AudioParams = audioParams ?? AudioParams.Default,
                Identifier = id,
            };

            if (range <= 0 && excludedSession == null)
            {
                RaiseNetworkEvent(msg);
                return new AudioSourceServer(this, id);
            }

            var players = range > 0.0f ? _playerManager.GetPlayersInRange(entity.Transform.Coordinates, range) : _playerManager.GetAllPlayers();

            for (var i = players.Count - 1; i >= 0; i--)
            {
                var player = players[i];
                if (player == excludedSession)
                {
                    players.RemoveAt(i);
                    continue;
                }

                RaiseNetworkEvent(msg, player.ConnectedClient);
            }

            return new AudioSourceServer(this, id, players);
        }

        /// <summary>
        ///     Play an audio file at a static position.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="coordinates">The coordinates at which to play the audio.</param>
        /// <param name="audioParams"></param>
        /// <param name="range">The max range at which the audio will be heard. Less than or equal to 0 to send to every player.</param>
        /// <param name="excludedSession">Session that won't receive the audio message.</param>
        public AudioSourceServer PlayAtCoords(string filename, EntityCoordinates coordinates, AudioParams? audioParams = null, int range = AudioDistanceRange, IPlayerSession? excludedSession = null)
        {
            var id = CacheIdentifier();
            var msg = new PlayAudioPositionalMessage
            {
                FileName = filename,
                Coordinates = coordinates,
                AudioParams = audioParams ?? AudioParams.Default,
                Identifier = id
            };

            if (range <= 0 && excludedSession == null)
            {
                RaiseNetworkEvent(msg);
                return new AudioSourceServer(this, id);
            }

            var players = range > 0.0f ? _playerManager.GetPlayersInRange(coordinates, range) : _playerManager.GetAllPlayers();

            for (var i = players.Count - 1; i >= 0; i--)
            {
                var player = players[i];
                if (player == excludedSession)
                {
                    players.RemoveAt(i);
                    continue;
                }

                RaiseNetworkEvent(msg, player.ConnectedClient);
            }

            return new AudioSourceServer(this, id, players);
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
            PlayGlobal(filename, audioParams);
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
            PlayFromEntity(filename, entity, audioParams);
        }

        /// <summary>
        ///     Play an audio file at a static position.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="coordinates">The coordinates at which to play the audio.</param>
        /// <param name="audioParams"></param>
        [Obsolete("Deprecated. Use PlayAtCoords instead.")]
        public void Play(string filename, EntityCoordinates coordinates, AudioParams? audioParams = null)
        {
            PlayAtCoords(filename, coordinates, audioParams);
        }

        #endregion
    }

    public static class AudioSystemExtensions
    {
        /// <summary>
        ///     Play an audio file following an entity.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="entity">The entity "emitting" the audio.</param>
        /// <param name="audioParams"></param>
        /// <param name="range">The max range at which the audio will be heard. Less than or equal to 0 to send to every player.</param>
        /// <param name="excludedSession">Sessions that won't receive the audio message.</param>
        /// <param name="audioSystem">A pre-fetched instance of <see cref="AudioSystem"/> to use, can be null.</param>
        public static void PlaySoundFrom(
            this IEntity entity,
            string filename,
            AudioParams? audioParams = null,
            int range = AudioDistanceRange,
            IPlayerSession? excludedSession = null,
            AudioSystem? audioSystem = null)
        {
            audioSystem ??= EntitySystem.Get<AudioSystem>();
            audioSystem.PlayFromEntity(filename, entity, audioParams, range, excludedSession);
        }

        /// <summary>
        ///     Play an audio file at a static position.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="coordinates">The coordinates at which to play the audio.</param>
        /// <param name="audioParams"></param>
        /// <param name="range">The max range at which the audio will be heard. Less than or equal to 0 to send to every player.</param>
        /// <param name="excludedSession">Session that won't receive the audio message.</param>
        /// <param name="audioSystem">A pre-fetched instance of <see cref="AudioSystem"/> to use, can be null.</param>
        public static void PlaySoundFrom(
            this EntityCoordinates coordinates,
            string filename,
            AudioParams? audioParams = null,
            int range = AudioDistanceRange,
            IPlayerSession? excludedSession = null,
            AudioSystem? audioSystem = null)
        {
            audioSystem ??= EntitySystem.Get<AudioSystem>();
            audioSystem.PlayAtCoords(filename, coordinates, audioParams, range, excludedSession);
        }
    }
}
