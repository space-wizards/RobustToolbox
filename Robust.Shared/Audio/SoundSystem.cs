using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Player;
using System;

namespace Robust.Shared.Audio
{
    /// <summary>
    /// A static proxy class for interfacing with the AudioSystem.
    /// </summary>
    public static class SoundSystem
    {
        /// <summary>
        /// Play an audio file globally, without position.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="playerFilter">The set of players that will hear the sound.</param>
        /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
        [Obsolete("Use SharedAudioSystem.PlayGlobal()")]
        public static IPlayingAudioStream? Play(string filename, Filter playerFilter, AudioParams? audioParams = null)
        {
            return IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedAudioSystem>().PlayGlobal(filename, playerFilter, audioParams);
        }

        /// <summary>
        /// Play an audio file following an entity.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="playerFilter">The set of players that will hear the sound.</param>
        /// <param name="uid">The UID of the entity "emitting" the audio.</param>
        /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
        [Obsolete("Use SharedAudioSystem")]
        public static IPlayingAudioStream? Play(string filename, Filter playerFilter, EntityUid uid,
            AudioParams? audioParams = null)
        {
            return IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedAudioSystem>().Play(filename, playerFilter, uid, audioParams);
        }

        /// <summary>
        /// Play an audio file at a static position.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="playerFilter">The set of players that will hear the sound.</param>
        /// <param name="coordinates">The coordinates at which to play the audio.</param>
        /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
        [Obsolete("Use SharedAudioSystem")]
        public static IPlayingAudioStream? Play(string filename, Filter playerFilter, EntityCoordinates coordinates,
            AudioParams? audioParams = null)
        {
            return IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedAudioSystem>().Play(filename, playerFilter, coordinates, audioParams);
        }
    }
}
