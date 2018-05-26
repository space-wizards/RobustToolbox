using SS14.Shared.Maths;
using SS14.Client.Utility;
using System.Collections.Generic;

namespace SS14.Client.Graphics
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
            private readonly (Texture icon, float delay)[][] Icons;

            internal State(Vector2u size, StateId stateId, DirectionType direction, (Texture icon, float delay)[][] icons)
            {
                Size = size;
                StateId = stateId;
                Directions = direction;
                Icons = icons;
            }

            public enum DirectionType : byte
            {
                Dir1,
                Dir4,
            }

            public enum Direction : byte
            {
                South = 0,
                North = 1,
                East = 2,
                West = 3,
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
                return GetFrame(dir.Convert(), 0).icon;
            }
        }
    }
}
