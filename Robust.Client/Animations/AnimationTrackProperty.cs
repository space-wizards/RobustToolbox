using System;
using System.Collections.Generic;
using Robust.Shared.Animations;
using Robust.Shared.Maths;

namespace Robust.Client.Animations
{
    public abstract class AnimationTrackProperty : AnimationTrack
    {
        public readonly List<KeyFrame> KeyFrames = new List<KeyFrame>();
        public AnimationInterpolationMode InterpolationMode { get; set; } = AnimationInterpolationMode.Linear;

        public override (int KeyFrameIndex, float FramePlayingTime) InitPlayback()
        {
            return (-1, 0);
        }

        public override (int KeyFrameIndex, float FramePlayingTime) AdvancePlayback(object context,
            int prevKeyFrameIndex,
            float prevPlayingTime, float frameTime)
        {
            var playingTime = prevPlayingTime + frameTime;
            var keyFrameIndex = prevKeyFrameIndex;

            // Advance to the correct key frame.
            while (keyFrameIndex != KeyFrames.Count - 1 && KeyFrames[keyFrameIndex + 1].KeyTime < playingTime)
            {
                playingTime -= KeyFrames[keyFrameIndex + 1].KeyTime;
                keyFrameIndex += 1;
            }

            // Find the value we've interpolated to.
            object value;

            var nextKeyFrame = keyFrameIndex + 1;
            if (nextKeyFrame == 0)
            {
                // Still before the first keyframe, do nothing.
                return (keyFrameIndex, playingTime);
            }

            if (nextKeyFrame == KeyFrames.Count)
            {
                // After the last keyframe.
                value = KeyFrames[keyFrameIndex].Value;
            }
            else
            {
                // Get us a scale 0 -> 1 here.
                var t = playingTime / KeyFrames[nextKeyFrame].KeyTime;

                switch (InterpolationMode)
                {
                    case AnimationInterpolationMode.Linear:
                        value = InterpolateLinear(KeyFrames[keyFrameIndex].Value, KeyFrames[nextKeyFrame].Value, t);
                        break;
                    case AnimationInterpolationMode.Cubic:
                        var pre = keyFrameIndex > 0 ? keyFrameIndex - 1 : keyFrameIndex;
                        var post = nextKeyFrame < KeyFrames.Count - 1 ? nextKeyFrame + 1 : nextKeyFrame;

                        value = InterpolateCubic(KeyFrames[pre].Value, KeyFrames[keyFrameIndex].Value,
                            KeyFrames[nextKeyFrame].Value, KeyFrames[post].Value, t);

                        break;
                    case AnimationInterpolationMode.Nearest:
                        value = t < 0.5f ? KeyFrames[keyFrameIndex].Value : KeyFrames[nextKeyFrame].Value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // Set the value.
            ApplyProperty(context, value);

            return (keyFrameIndex, playingTime);
        }

        protected abstract void ApplyProperty(object context, object value);

        private static object InterpolateLinear(object a, object b, float t)
        {
            switch (a)
            {
                case Vector2 vector2:
                    return Vector2.Lerp(vector2, (Vector2) b, t);
                case Vector3 vector3:
                    return Vector3.Lerp(vector3, (Vector3) b, t);
                case Vector4 vector4:
                    return Vector4.Lerp(vector4, (Vector4) b, t);
                case float f:
                    return FloatMath.Lerp(f, (float) b, t);
                case double d:
                    return FloatMath.Lerp(d, (double) b, t);
                case Angle angle:
                    return Angle.Lerp(angle, (Angle) b, t);
                case Color color:
                    return Color.InterpolateBetween(color, (Color) b, t);
                case int i:
                    return (int) FloatMath.Lerp((double) i, (int) b, t);
                default:
                    throw new NotSupportedException($"Cannot linear interpolate object of type '{a.GetType()}'");
            }
        }

        private static object InterpolateCubic(object preA, object a, object b, object postB, float t)
        {
            switch (a)
            {
                case Vector2 vector2:
                    return Vector2.InterpolateCubic((Vector2)preA, vector2, (Vector2) b, (Vector2) postB, t);
                case Vector3 vector3:
                    return Vector3.InterpolateCubic((Vector3)preA, vector3, (Vector3) b, (Vector3) postB, t);
                case Vector4 vector4:
                    return Vector4.InterpolateCubic((Vector4)preA, vector4, (Vector4) b, (Vector4) postB, t);
                case float f:
                    return FloatMath.InterpolateCubic((float) preA, f, (float) b, (float) postB, t);
                case double d:
                    return FloatMath.InterpolateCubic((double) preA, d, (double) b, (double) postB, t);
                case int i:
                    return (int) FloatMath.InterpolateCubic((int) preA, (double) i, (int) b, (int) postB, t);
                default:
                    throw new NotSupportedException($"Cannot cubic interpolate object of type '{a.GetType()}'");
            }
        }

        public struct KeyFrame
        {
            public readonly object Value;

            /// <summary>
            ///     The time between this keyframe and the previous.
            /// </summary>
            public readonly float KeyTime;

            public KeyFrame(object value, float keyTime)
            {
                Value = value;
                KeyTime = keyTime;
            }
        }
    }
}
