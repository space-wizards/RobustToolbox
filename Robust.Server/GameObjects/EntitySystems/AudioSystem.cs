using System;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;

namespace Robust.Server.GameObjects.EntitySystems
{
    public class AudioSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private IPlayerManager _playerManager;
#pragma warning restore 649


        public const int AudioDistanceRange = 20;

        /// <summary>
        ///     Play an audio file globally, without position.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="audioParams"></param>
        /// <param name="predicate">The predicate that will be used to send the audio to players, or null to send to everyone.</param>
        public void Play(string filename, AudioParams? audioParams = null, Func<IPlayerSession, bool> predicate = null)
        {
            var msg = new PlayAudioGlobalMessage
            {
                FileName = filename,
                AudioParams = audioParams ?? AudioParams.Default
            };

            if(predicate == null)
                RaiseNetworkEvent(msg);
            else
                foreach(var player in _playerManager.GetPlayersBy(predicate))
                {
                    RaiseNetworkEvent(msg, player.ConnectedClient);
                }
        }

        /// <summary>
        ///     Play an audio file following an entity.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="entity">The entity "emitting" the audio.</param>
        /// <param name="audioParams"></param>
        /// <param name="range">The max range at which the audio will be heard. Less than or equal to 0 to send to every player.</param>
        public void Play(string filename, IEntity entity, AudioParams? audioParams = null, int range = AudioDistanceRange)
        {
            var msg = new PlayAudioEntityMessage
            {
                FileName = filename,
                EntityUid = entity.Uid,
                AudioParams = audioParams ?? AudioParams.Default
            };

            if (range <= 0)
                RaiseNetworkEvent(msg);
            else
                foreach(var player in _playerManager.GetPlayersInRange(entity.Transform.GridPosition, range))
                {
                    RaiseNetworkEvent(msg, player.ConnectedClient);
                }
        }

        /// <summary>
        ///     Play an audio file at a static position.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="coordinates">The coordinates at which to play the audio.</param>
        /// <param name="audioParams"></param>
        /// <param name="range">The max range at which the audio will be heard. Less than or equal to 0 to send to every player.</param>
        public void Play(string filename, GridCoordinates coordinates, AudioParams? audioParams = null, int range = AudioDistanceRange)
        {
            var msg = new PlayAudioPositionalMessage
            {
                FileName = filename,
                Coordinates = coordinates,
                AudioParams = audioParams ?? AudioParams.Default
            };

            if (range <= 0)
                RaiseNetworkEvent(msg);
            else
                foreach(var player in _playerManager.GetPlayersInRange(coordinates, range))
                {
                    RaiseNetworkEvent(msg, player.ConnectedClient);
                }
        }
    }
}
