using SS14.Shared.Audio;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Map;

namespace SS14.Server.GameObjects.EntitySystems
{
    public class AudioSystem : EntitySystem
    {
        /// <summary>
        ///     Play an audio file globally, without position.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
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
        public void Play(string filename, GridLocalCoordinates coordinates, AudioParams? audioParams = null)
        {
            var msg = new PlayAudioPositionalMessage
            {
                FileName = filename,
                Coordinates = coordinates,
                AudioParams = audioParams ?? AudioParams.Default
            };
            RaiseNetworkEvent(msg);
        }
    }
}
