using System.Collections.Generic;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Client.Animations
{
    /// <summary>
    ///     An animation track that plays a sound as keyframes.
    /// </summary>
    public sealed class AnimationTrackPlaySound : AnimationTrack
    {
        /// <summary>
        ///     A list of key frames for when to fire flicks.
        /// </summary>
        public readonly List<KeyFrame> KeyFrames = new List<KeyFrame>();

        public override (int KeyFrameIndex, float FramePlayingTime) InitPlayback()
        {
            return (-1, 0);
        }

        public override (int KeyFrameIndex, float FramePlayingTime)
            AdvancePlayback(object context, int prevKeyFrameIndex, float prevPlayingTime, float frameTime)
        {
            var entity = (IEntity) context;

            var playingTime = prevPlayingTime + frameTime;
            var keyFrameIndex = prevKeyFrameIndex;
            // Advance to the correct key frame.
            while (keyFrameIndex != KeyFrames.Count - 1 && KeyFrames[keyFrameIndex + 1].KeyTime < playingTime)
            {
                playingTime -= KeyFrames[keyFrameIndex + 1].KeyTime;
                keyFrameIndex += 1;

                var keyFrame = KeyFrames[keyFrameIndex];

                EntitySystemHelpers.EntitySystem<AudioSystem>()
                    .Play(keyFrame.Resource, entity);
            }

            return (keyFrameIndex, playingTime);
        }

        public struct KeyFrame
        {
            /// <summary>
            ///     The RSI state to play when this keyframe gets triggered.
            /// </summary>
            public readonly string Resource;

            /// <summary>
            ///     The time between this keyframe and the last.
            /// </summary>
            public readonly float KeyTime;

            public KeyFrame(string resource, float keyTime)
            {
                Resource = resource;
                KeyTime = keyTime;
            }
        }
    }
}
