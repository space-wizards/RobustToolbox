using System;
using System.Collections.Generic;
using Robust.Client.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;

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
        public List<KeyFrame> KeyFrames { get; private set; } = new();

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

                EntitySystem.Get<AudioSystem>()
                    .Play(keyFrame.Resource, entity, keyFrame.AudioParamsFunc.Invoke());
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
            ///     A function that returns the audio parameter to be used.
            ///     The reason this is a function is so that this can return
            ///     an AudioParam with different parameters each time, such as random pitch.
            /// </summary>
            public readonly Func<AudioParams> AudioParamsFunc;

            /// <summary>
            ///     The time between this keyframe and the last.
            /// </summary>
            public readonly float KeyTime;

            public KeyFrame(string resource, float keyTime, Func<AudioParams>? audioParams = null)
            {
                Resource = resource;
                KeyTime = keyTime;
                AudioParamsFunc = audioParams ?? (() => AudioParams.Default);
            }
        }
    }
}
