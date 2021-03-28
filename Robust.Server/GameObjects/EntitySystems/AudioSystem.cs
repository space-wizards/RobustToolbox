using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Players;

namespace Robust.Server.GameObjects
{
    [UsedImplicitly]
    public class AudioSystem : EntitySystem, IAudioSystem
    {
        private const int AudioDistanceRange = 25;

        private uint _streamIndex;

        private class AudioSourceServer : IPlayingAudioStream
        {
            private readonly uint _id;
            private readonly AudioSystem _audioSystem;
            private readonly IEnumerable<ICommonSession>? _sessions;

            internal AudioSourceServer(AudioSystem parent, uint identifier, IEnumerable<ICommonSession>? sessions = null)
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

        private void InternalStop(uint id, IEnumerable<ICommonSession>? sessions = null)
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

        /// <inheritdoc />
        public int DefaultSoundRange => AudioDistanceRange;

        /// <inheritdoc />
        public int OcclusionCollisionMask { get; set; }

        /// <inheritdoc />
        public IPlayingAudioStream Play(Filter playerFilter, string filename, AudioParams? audioParams = null)
        {
            var id = CacheIdentifier();
            var msg = new PlayAudioGlobalMessage
            {
                FileName = filename,
                AudioParams = audioParams ?? AudioParams.Default,
                Identifier = id
            };

            var players = (playerFilter as IFilter).Recipients;
            foreach (var player in players)
            {
                RaiseNetworkEvent(msg, player.ConnectedClient);
            }

            return new AudioSourceServer(this, id, players);
        }

        /// <inheritdoc />
        public IPlayingAudioStream Play(Filter playerFilter, string filename, IEntity entity, AudioParams? audioParams = null)
        {
            //TODO: Calculate this from PAS
            var range = audioParams is null || audioParams.Value.MaxDistance <= 0 ? AudioDistanceRange : audioParams.Value.MaxDistance;

            var id = CacheIdentifier();

            var msg = new PlayAudioEntityMessage
            {
                FileName = filename,
                Coordinates = entity.Transform.Coordinates,
                EntityUid = entity.Uid,
                AudioParams = audioParams ?? AudioParams.Default,
                Identifier = id,
            };
            
            IList<ICommonSession> players;
            var recipients = (playerFilter as IFilter).Recipients;

            if (range > 0.0f)
                players = PasInRange(recipients, entity.Transform.MapPosition, range);
            else
                players = recipients;

            foreach (var player in players)
            {
                RaiseNetworkEvent(msg, player.ConnectedClient);
            }
            
            return new AudioSourceServer(this, id, players);
        }

        /// <inheritdoc />
        public IPlayingAudioStream Play(Filter playerFilter, string filename, EntityCoordinates coordinates, AudioParams? audioParams = null)
        {
            //TODO: Calculate this from PAS
            var range = audioParams is null || audioParams.Value.MaxDistance <= 0 ? AudioDistanceRange : audioParams.Value.MaxDistance;

            var id = CacheIdentifier();
            var msg = new PlayAudioPositionalMessage
            {
                FileName = filename,
                Coordinates = coordinates,
                AudioParams = audioParams ?? AudioParams.Default,
                Identifier = id
            };
            
            IList<ICommonSession> players;
            var recipients = (playerFilter as IFilter).Recipients;

            if (range > 0.0f)
                players = PasInRange(recipients, coordinates.ToMap(EntityManager), range);
            else
                players = recipients;

            foreach (var player in players)
            {
                RaiseNetworkEvent(msg, player.ConnectedClient);
            }
            
            return new AudioSourceServer(this, id, players);
        }

        private static List<ICommonSession> PasInRange(IEnumerable<ICommonSession> players, MapCoordinates position, float range)
        {
            return players.Where(x =>
                    x.AttachedEntity != null &&
                    position.InRange(x.AttachedEntity.Transform.MapPosition, range))
                .ToList();
        }
    }
}
