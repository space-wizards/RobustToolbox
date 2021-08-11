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

            RaiseNetworkEvent(msg, playerFilter);

            return new AudioSourceServer(this, id, playerFilter.Recipients.ToArray());
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
                MapCoordinates = entity.Transform.MapPosition,
                EntityUid = entity.Uid,
                AudioParams = audioParams ?? AudioParams.Default,
                Identifier = id,
            };

            // We clone the filter here as to not modify the original instance.
            if (range > 0.0f)
                playerFilter = playerFilter.Clone().AddInRange(entity.Transform.MapPosition, range);

            RaiseNetworkEvent(msg, playerFilter);

            return new AudioSourceServer(this, id, playerFilter.Recipients.ToArray());
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
                MapCoordinates = coordinates.ToMap(EntityManager),
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
