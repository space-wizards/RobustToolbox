using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Players;

namespace Robust.Server.GameObjects
{
    [UsedImplicitly]
    public class AudioSystem : SharedAudioSystem, IAudioSystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

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

            RaiseNetworkEvent(msg, playerFilter);

            return new AudioSourceServer(this, id, playerFilter.Recipients.ToArray());
        }

        /// <inheritdoc />
        public IPlayingAudioStream? Play(Filter playerFilter, string filename, IEntity entity, AudioParams? audioParams = null)
        {
            return Play(playerFilter, filename, entity.Uid, audioParams);
        }

        public IPlayingAudioStream? Play(Filter playerFilter, string filename, EntityUid uid, AudioParams? audioParams = null)
        {
            //TODO: Calculate this from PAS
            var range = audioParams is null || audioParams.Value.MaxDistance <= 0 ? AudioDistanceRange : audioParams.Value.MaxDistance;

            if(!EntityManager.TryGetComponent<TransformComponent>(uid, out var transform))
                return null;

            var id = CacheIdentifier();

            var fallbackCoordinates = GetFallbackCoordinates(transform.MapPosition);

            var msg = new PlayAudioEntityMessage
            {
                FileName = filename,
                Coordinates = transform.Coordinates,
                FallbackCoordinates = fallbackCoordinates,
                EntityUid = uid,
                AudioParams = audioParams ?? AudioParams.Default,
                Identifier = id,
            };

            // We clone the filter here as to not modify the original instance.
            if (range > 0.0f)
                playerFilter = playerFilter.Clone().AddInRange(transform.MapPosition, range);

            RaiseNetworkEvent(msg, playerFilter);

            return new AudioSourceServer(this, id, playerFilter.Recipients.ToArray());
        }

        /// <inheritdoc />
        public IPlayingAudioStream Play(Filter playerFilter, string filename, EntityCoordinates coordinates, AudioParams? audioParams = null)
        {
            //TODO: Calculate this from PAS
            var range = audioParams is null || audioParams.Value.MaxDistance <= 0 ? AudioDistanceRange : audioParams.Value.MaxDistance;

            var id = CacheIdentifier();

            var fallbackCoordinates = GetFallbackCoordinates(coordinates.ToMap(_entityManager));

            var msg = new PlayAudioPositionalMessage
            {
                FileName = filename,
                Coordinates = coordinates,
                FallbackCoordinates = fallbackCoordinates,
                AudioParams = audioParams ?? AudioParams.Default,
                Identifier = id
            };

            // We clone the filter here as to not modify the original instance.
            if (range > 0.0f)
                playerFilter = playerFilter.Clone().AddInRange(coordinates.ToMap(EntityManager), range);

            RaiseNetworkEvent(msg, playerFilter);

            return new AudioSourceServer(this, id, playerFilter.Recipients.ToArray());
        }
    }
}
