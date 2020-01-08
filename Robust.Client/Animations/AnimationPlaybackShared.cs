using System;

namespace Robust.Client.Animations
{
    /// <summary>
    ///     Infrastructure to handle playback of animations.
    /// </summary>
    internal static class AnimationPlaybackShared
    {
        public static bool UpdatePlayback(object context, AnimationPlayback playback, float frameTime)
        {
            var animation = playback.Animation;
            for (var i = 0; i < animation.AnimationTracks.Count; i++)
            {
                var track = animation.AnimationTracks[i];
                ref var trackPlayback = ref playback.TrackPlaybacks[i];

                var (keyFrame, playing) = track.AdvancePlayback(context, trackPlayback.KeyFrameIndex,
                    trackPlayback.KeyFrameTimePlaying, frameTime);

                trackPlayback.KeyFrameIndex = keyFrame;
                trackPlayback.KeyFrameTimePlaying = playing;
            }

            playback.PlayTime += frameTime;
            return TimeSpan.FromSeconds(playback.PlayTime) <= animation.Length;
        }

        /// <summary>
        ///     Represents an "active" playback of an animation.
        /// </summary>
        public sealed class AnimationPlayback
        {
            /// <summary>
            ///     The animation being played.
            /// </summary>
            public readonly Animation Animation;

            // Indices here correspond to the track indices in Animation.
            public readonly AnimationTrackPlayback[] TrackPlaybacks;

            public float PlayTime;

            public AnimationPlayback(Animation animation)
            {
                Animation = animation;
                TrackPlaybacks = new AnimationTrackPlayback[animation.AnimationTracks.Count];

                for (var i = 0; i < animation.AnimationTracks.Count; i++)
                {
                    var (keyFrame, left) = animation.AnimationTracks[i].InitPlayback();
                    TrackPlaybacks[i] = new AnimationTrackPlayback(keyFrame, left);
                }
            }
        }

        public struct AnimationTrackPlayback
        {
            public int KeyFrameIndex;
            public float KeyFrameTimePlaying;

            public AnimationTrackPlayback(int keyFrameIndex, float keyFrameTimePlaying)
            {
                KeyFrameIndex = keyFrameIndex;
                KeyFrameTimePlaying = keyFrameTimePlaying;
            }
        }
    }
}
