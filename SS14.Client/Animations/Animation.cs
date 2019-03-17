using System;
using System.Collections.Generic;
using SS14.Client.GameObjects.Components.Animations;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Utility;

namespace SS14.Client.Animations
{
    /// <summary>
    ///     A animation represents a way to animate something, using keyframes and such.
    /// </summary>
    /// <remarks>
    ///     An animation is a collection of <see cref="AnimationTracks"/>, which are all executed in sync.
    /// </remarks>
    /// <seealso cref="AnimationPlayerComponent"/>
    public sealed class Animation
    {
        public readonly List<AnimationTrack> AnimationTracks = new List<AnimationTrack>();

        public TimeSpan Length { get; set; }
    }

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

    /// <summary>
    ///     An animation track that plays RSI state animations manually, so they can be precisely controlled etc.
    /// </summary>
    public sealed class AnimationTrackSpriteFlick : AnimationTrack
    {
        /// <summary>
        ///     A list of key frames for when to fire flicks.
        /// </summary>
        public readonly List<KeyFrame> KeyFrames = new List<KeyFrame>();

        // TODO: Should this layer key be per keyframe maybe?
        /// <summary>
        ///     The layer key of the layer to flick on.
        /// </summary>
        public object LayerKey { get; set; }

        public override (int KeyFrameIndex, float FramePlayingTime) InitPlayback()
        {
            return (-1, 0);
        }

        public override (int KeyFrameIndex, float FramePlayingTime)
            AdvancePlayback(object context, int prevKeyFrameIndex, float prevPlayingTime, float frameTime)
        {
            var entity = (IEntity) context;
            var sprite = entity.GetComponent<ISpriteComponent>();

            var playingTime = prevPlayingTime + frameTime;
            var keyFrameIndex = prevKeyFrameIndex;
            // Advance to the correct key frame.
            while (keyFrameIndex != KeyFrames.Count-1 && KeyFrames[keyFrameIndex+1].KeyTime < playingTime)
            {
                playingTime -= KeyFrames[keyFrameIndex+1].KeyTime;
                keyFrameIndex += 1;
            }

            if (keyFrameIndex >= 0)
            {
                var keyFrame = KeyFrames[keyFrameIndex];
                // Advance animation on current key frame.
                var rsi = sprite.LayerGetActualRSI(LayerKey);
                var state = rsi[keyFrame.State];
                DebugTools.Assert(state.AnimationLength != null, "state.AnimationLength != null");
                var animationTime = Math.Min(state.AnimationLength.Value-0.01f, playingTime);
                sprite.LayerSetAutoAnimated(LayerKey, false);
                // TODO: Doesn't setting the state explicitly reset the animation
                // so it's slightly more inefficient?
                sprite.LayerSetState(LayerKey, keyFrame.State);
                sprite.LayerSetAnimationTime(LayerKey, animationTime);
            }

            return (keyFrameIndex, playingTime);
        }

        public struct KeyFrame
        {
            /// <summary>
            ///     The RSI state to play when this keyframe gets triggered.
            /// </summary>
            public readonly RSI.StateId State;

            /// <summary>
            ///     The time between this keyframe and the last.
            /// </summary>
            public readonly float KeyTime;

            public KeyFrame(RSI.StateId state, float keyTime)
            {
                State = state;
                KeyTime = keyTime;
            }
        }
    }
}
