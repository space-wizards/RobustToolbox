using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Robust.Shared.Audio
{
    /// <summary>
    /// A static proxy class for interfacing with the AudioSystem.
    /// </summary>
    public static class SoundSystem
    {
        /// <summary>
        /// Default max range at which the sound can be heard.
        /// </summary>
        public static int DefaultSoundRange => GetAudio()?.DefaultSoundRange ?? 0;

        /// <summary>
        /// Used in the PAS to designate the physics collision mask of occluders.
        /// </summary>
        public static int OcclusionCollisionMask
        {
            get => GetAudio()?.OcclusionCollisionMask ?? 0;
            set
            {
                var audio = GetAudio();

                if (audio is null)
                    return;
                audio.OcclusionCollisionMask = value;
            }
        }

        private static IAudioSystem? GetAudio()
        {
            // There appears to be no way to get a System by interface.
            var args = new QueryAudioSystem();
            IoCManager.Resolve<IEntityManager>().EventBus.RaiseEvent(EventSource.Local, args);
            return args.Audio;
        }

        /// <summary>
        /// Play an audio file globally, without position.
        /// </summary>
        /// <param name="playerFilter">The set of players that will hear the sound.</param>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
        public static IPlayingAudioStream? Play(Filter playerFilter, string filename, AudioParams? audioParams = null)
        {
            return GetAudio()?.Play(playerFilter, filename, audioParams);
        }

        /// <summary>
        /// Play an audio file following an entity.
        /// </summary>
        /// <param name="playerFilter">The set of players that will hear the sound.</param>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="entity">The entity "emitting" the audio.</param>
        /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
        public static IPlayingAudioStream? Play(Filter playerFilter, string filename, IEntity entity, AudioParams? audioParams = null)
        {
            return GetAudio()?.Play(playerFilter, filename, entity, audioParams);
        }

        /// <summary>
        /// Play an audio file following an entity.
        /// </summary>
        /// <param name="playerFilter">The set of players that will hear the sound.</param>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="uid">The UID of the entity "emitting" the audio.</param>
        /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
        public static IPlayingAudioStream? Play(Filter playerFilter, string filename, EntityUid uid, AudioParams? audioParams = null)
        {
            return GetAudio()?.Play(playerFilter, filename, uid, audioParams);
        }

        /// <summary>
        /// Play an audio file at a static position.
        /// </summary>
        /// <param name="playerFilter">The set of players that will hear the sound.</param>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="coordinates">The coordinates at which to play the audio.</param>
        /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
        public static IPlayingAudioStream? Play(Filter playerFilter, string filename, EntityCoordinates coordinates, AudioParams? audioParams = null)
        {
            return GetAudio()?.Play(playerFilter, filename, coordinates, audioParams);
        }

        internal class QueryAudioSystem : EntityEventArgs
        {
            public IAudioSystem? Audio { get; set; }
        }
    }
}
