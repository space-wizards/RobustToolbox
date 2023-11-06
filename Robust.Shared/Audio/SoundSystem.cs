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
        // These functions are obsolete and I CBF adding new arguments to them.
        private static bool _recordReplay = false;

        /// <summary>
        /// Play an audio file globally, without position.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="playerFilter">The set of players that will hear the sound.</param>
        /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
        [Obsolete("Use SharedAudioSystem.PlayGlobal()")]
        public static IPlayingAudioStream? Play(string filename, Filter playerFilter, AudioParams? audioParams = null)
        {
            var entSystMan = IoCManager.Resolve<IEntitySystemManager>();

            // Some timers try to play audio after the system has shut down?
            entSystMan.TryGetEntitySystem(out SharedAudioSystem? audio);
            return audio?.PlayGlobal(filename, playerFilter, _recordReplay, audioParams);
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
            var entSystMan = IoCManager.Resolve<IEntitySystemManager>();

            // Some timers try to play audio after the system has shut down?
            entSystMan.TryGetEntitySystem(out SharedAudioSystem? audio);
            return audio?.Play(filename, playerFilter, uid, _recordReplay, audioParams);
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
            var entSystMan = IoCManager.Resolve<IEntitySystemManager>();

            // Some timers try to play audio after the system has shut down?
            entSystMan.TryGetEntitySystem(out SharedAudioSystem? audio);
            return audio?.Play(filename, playerFilter, coordinates, _recordReplay, audioParams);
        }
    }
}
