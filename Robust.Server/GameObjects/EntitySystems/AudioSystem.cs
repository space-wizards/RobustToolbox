using System;
using System.Collections.Generic;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Players;

namespace Robust.Server.GameObjects
{
    public class AudioSystem : EntitySystem, IAudioSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public const int AudioDistanceRange = 25;

        private uint _streamIndex;

        public class AudioSourceServer : IPlayingAudioStream
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

        /// <inheritdoc />
        public override void Initialize()
        {
            SubscribeLocalEvent<SoundSystem.QueryAudioSystem>((ev => ev.Audio = this));
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
        /// <param name="recipients"></param>
        public IPlayingAudioStream PlayGlobal(string filename, AudioParams? audioParams = null, Func<IPlayerSession, bool>? predicate = null, IPlayerSession? excludedSession = null)
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

            IList<IPlayerSession> players = predicate != null ? _playerManager.GetPlayersBy(predicate) : _playerManager.GetAllPlayers();

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
        public IPlayingAudioStream PlayFromEntity(string filename, IEntity entity, AudioParams? audioParams = null, int range = AudioDistanceRange, IPlayerSession? excludedSession = null)
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

            // send to every player
            if (range <= 0 && excludedSession == null)
            {
                RaiseNetworkEvent(msg);
                return new AudioSourceServer(this, id);
            }

            List<IPlayerSession> players;

            if (range > 0.0f)
                players = _playerManager.GetPlayersInRange(entity.Transform.Coordinates, range);
            else
                players = _playerManager.GetAllPlayers();

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
        public IPlayingAudioStream PlayAtCoords(string filename, EntityCoordinates coordinates, AudioParams? audioParams = null, int range = AudioDistanceRange, IPlayerSession? excludedSession = null)
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

            List<IPlayerSession> players;

            if (range > 0.0f)
                players = _playerManager.GetPlayersInRange(coordinates, range);
            else
                players = _playerManager.GetAllPlayers();

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

        /// <inheritdoc />
        public int DefaultSoundRange => AudioDistanceRange;

        /// <inheritdoc />
        public int OcclusionCollisionMask { get; set; }

        /// <inheritdoc />
        public IPlayingAudioStream? Play(Filter playerFilter, string filename, AudioParams? audioParams = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public IPlayingAudioStream? Play(Filter playerFilter, string filename, IEntity entity, AudioParams? audioParams = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public IPlayingAudioStream? Play(Filter playerFilter, string filename, EntityCoordinates coordinates, AudioParams? audioParams = null)
        {
            throw new NotImplementedException();
        }
    }
}
