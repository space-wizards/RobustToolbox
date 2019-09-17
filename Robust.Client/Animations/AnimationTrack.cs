namespace Robust.Client.Animations
{
    /// <summary>
    ///     A single track of an <see cref="Animation"/>.
    /// </summary>
    public abstract class AnimationTrack
    {
        /// <summary>
        ///     Return the values necessary to initialize a playback.
        /// </summary>
        /// <returns>
        ///     A tuple containing the new key frame the animation track is on and the new time left in said key frame.
        /// </returns>
        public abstract (int KeyFrameIndex, float FramePlayingTime) InitPlayback();

        /// <summary>
        ///     Advance this animation track's playback.
        /// </summary>
        /// <param name="context">The object this animation track is being played on, e.g. an entity.</param>
        /// <param name="prevKeyFrameIndex">The key frame this animation track is on.</param>
        /// <param name="prevPlayingTime">The amount of time this keyframe has been running.</param>
        /// <param name="frameTime">The amount of time to increase.</param>
        /// <returns>
        ///     A tuple containing the new key frame the animation track is on and the current time on said key frame.
        /// </returns>
        public abstract (int KeyFrameIndex, float FramePlayingTime)
            AdvancePlayback(object context, int prevKeyFrameIndex, float prevPlayingTime, float frameTime);
    }
}
