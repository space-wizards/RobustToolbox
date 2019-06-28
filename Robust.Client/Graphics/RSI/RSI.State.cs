using System;
using Robust.Shared.Maths;
using Robust.Client.Utility;
using System.Collections.Generic;

namespace Robust.Client.Graphics
{
    public sealed partial class RSI
    {
        /// <summary>
        ///     Represents a single icon state inside an RSI.
        /// </summary>
        public sealed class State : IDirectionalTextureProvider
        {
            public Vector2u Size { get; }
            public StateId StateId { get; }
            public DirectionType Directions { get; }
            public Texture Frame0 => Icons[0][0].icon;
            public float? AnimationLength { get; }
            private readonly (Texture icon, float delay)[][] Icons;

            internal State(Vector2u size, StateId stateId, DirectionType direction, (Texture icon, float delay)[][] icons)
            {
                Size = size;
                StateId = stateId;
                Directions = direction;
                Icons = icons;

                float? animLength = null;
                foreach (var dirFrames in icons)
                {
                    if (dirFrames.Length <= 1)
                    {
                        continue;
                    }

                    var length = 0f;
                    foreach (var (_, delay) in dirFrames)
                    {
                        length += delay;
                    }

                    if (animLength == null)
                    {
                        animLength = length;
                    }
                    else
                    {
                        animLength = Math.Max(animLength.Value, length);
                    }
                }

                AnimationLength = animLength;
            }

            public enum DirectionType : byte
            {
                Dir1,
                Dir4,
                Dir8,
            }

            public enum Direction : byte
            {
                South = 0,
                North = 1,
                East = 2,
                West = 3,
                SouthEast = 4,
                SouthWest = 5,
                NorthEast = 6,
                NorthWest = 7,
            }

            public (Texture icon, float delay) GetFrame(Direction direction, int frame)
            {
                return Icons[(int)direction][frame];
            }

            public IReadOnlyCollection<(Texture icon, float delay)> GetDirectionFrames(Direction direction)
            {
                return Icons[(int)direction];
            }

            public int DelayCount(Direction direction)
            {
                return Icons[(int)direction].Length;
            }

            Texture IDirectionalTextureProvider.Default => GetFrame(Direction.South, 0).icon;

            Texture IDirectionalTextureProvider.TextureFor(Shared.Maths.Direction dir)
            {
                if (Directions == DirectionType.Dir1)
                {
                    return GetFrame(Direction.South, 0).icon;
                }
                return GetFrame(dir.Convert(Directions), 0).icon;
            }
        }
    }
}
