using System;
using Robust.Client.Utility;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics
{
    public sealed partial class RSI
    {
        // See https://github.com/space-wizards/RobustToolbox/issues/905 for the simplification thing of playback.
        /// <summary>
        ///     Represents a single icon state inside an RSI.
        /// </summary>
        /// <remarks>
        ///     While the RSI spec allows different animation timing for directions in the same frame,
        ///     RSIs are folded into a single set of animation timings when loaded.
        ///     This is to simplify animation playback code in-engine.
        /// </remarks>
        public sealed class State : IRsiStateLike
        {
            // List of delays for the frame to reach the next frame.
            private readonly float[] Delays;

            // 2D array for the texture to use for each animation frame at each direction.
            private readonly Texture[][] Icons;

            internal State(Vector2i size, StateId stateId, DirectionType direction, float[] delays, Texture[][] icons)
            {
                DebugTools.Assert(size.X > 0);
                DebugTools.Assert(size.Y > 0);
                DebugTools.Assert(stateId.IsValid);

                Size = size;
                StateId = stateId;
                Directions = direction;
                Delays = delays;
                Icons = icons;

                foreach (var delay in delays)
                {
                    AnimationLength += delay;
                }
            }

            /// <summary>
            ///     The size of each individual frame in this state.
            /// </summary>
            public Vector2i Size { get; }

            /// <summary>
            ///     The identifier for this state inside an RSI.
            /// </summary>
            public StateId StateId { get; }

            /// <summary>
            ///     How many directions this state has.
            /// </summary>
            public DirectionType Directions { get; }

            /// <summary>
            ///     The first frame of the "south" direction.
            /// </summary>
            /// <remarks>
            ///     Always available and better than nothing for previews etc.
            /// </remarks>
            public Texture Frame0 => Icons[0][0];

            /// <summary>
            ///     The total play length of this state's animation, in seconds.
            /// </summary>
            public float AnimationLength { get; }

            /// <summary>
            ///     The amount of frames in the animation of this state.
            /// </summary>
            public int DelayCount => Delays.Length;

            /// <summary>
            ///     If true, this state has an animation to play.
            /// </summary>
            public bool IsAnimated => DelayCount > 1;

            int IRsiStateLike.AnimationFrameCount => DelayCount;

            public Texture GetFrame(Direction direction, int frame)
            {
                return Icons[(int) direction][frame];
            }

            public Texture[] GetFrames(Direction direction)
            {
                return Icons[(int) direction];
            }

            /// <summary>
            ///     Gets the delay between the specified frame and the next frame.
            /// </summary>
            /// <param name="frame">The index of the frame.</param>
            /// <returns>The delay, in seconds.</returns>
            /// <exception cref="IndexOutOfRangeException">
            ///     Thrown if the frame index does not exist.
            /// </exception>
            public float GetDelay(int frame)
            {
                return Delays[frame];
            }

            public float[] GetDelays()
            {
                return Delays;
            }

            Texture IDirectionalTextureProvider.Default => Frame0;

            Texture IDirectionalTextureProvider.TextureFor(Shared.Maths.Direction dir)
            {
                if (Directions == DirectionType.Dir1)
                {
                    return Frame0;
                }

                return GetFrame(dir.Convert(Directions), 0);
            }

            /// <summary>
            ///     Specifies which types of directions an RSI state has.
            /// </summary>
            public enum DirectionType : byte
            {
                /// <summary>
                ///     A single direction, namely South.
                /// </summary>
                Dir1,

                /// <summary>
                ///     4 cardinal directions.
                /// </summary>
                Dir4,

                /// <summary>
                ///     4 cardinal + 4 diagonal directions.
                /// </summary>
                Dir8,
            }

            /// <summary>
            ///     Specifies a direction in an RSI state.
            /// </summary>
            public enum Direction : byte
            {
                // Value of the enum here matches the index used to store it in the icons array.
                South = 0,
                North = 1,
                East = 2,
                West = 3,
                SouthEast = 4,
                SouthWest = 5,
                NorthEast = 6,
                NorthWest = 7,
            }
        }
    }
}
