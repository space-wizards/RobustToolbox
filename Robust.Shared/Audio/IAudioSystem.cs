using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Robust.Shared.Audio
{
    public interface IAudioSystem
    {
        /// <summary>
        /// Default max range at which the sound can be heard.
        /// </summary>
        int DefaultSoundRange { get; }

        /// <summary>
        /// Used in the PAS to designate the physics collision mask of occluders.
        /// </summary>
        int OcclusionCollisionMask { get; set; }

        /// <summary>
        /// Play an audio file globally, without position.
        /// </summary>
        /// <param name="playerFilter">The set of players that will hear the sound.</param>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
        IPlayingAudioStream? Play(Filter playerFilter, string filename, AudioParams? audioParams = null);

        /// <summary>
        /// Play an audio file following an entity.
        /// </summary>
        /// <param name="playerFilter">The set of players that will hear the sound.</param>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="entity">The entity "emitting" the audio.</param>
        /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
        IPlayingAudioStream? Play(Filter playerFilter, string filename, IEntity entity, AudioParams? audioParams = null);

        /// <summary>
        /// Play an audio file at a static position.
        /// </summary>
        /// <param name="playerFilter">The set of players that will hear the sound.</param>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="coordinates">The coordinates at which to play the audio.</param>
        /// <param name="audioParams">Audio parameters to apply when playing the sound.</param>
        IPlayingAudioStream? Play(Filter playerFilter, string filename, EntityCoordinates coordinates, AudioParams? audioParams = null);
    }
}
