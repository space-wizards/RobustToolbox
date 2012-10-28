using System;

namespace SFML
{
    namespace Graphics
    {
        ////////////////////////////////////////////////////////////
        /// <summary>
        /// IntRect is an utility class for manipulating 2D rectangles
        /// with integer coordinates
        /// *defines extra methods from NetGore
        /// </summary>
        ////////////////////////////////////////////////////////////
        public partial struct IntRect : IEquatable<IntRect>
        {
            /// <summary>
            /// Indicates whether the current object is equal to another object of the same type.
            /// </summary>
            /// <param name="other">An object to compare with this object.</param>
            /// <returns>
            /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
            /// </returns>
            public bool Equals(IntRect other)
            {
                return other.Left == Left && other.Top == Top && other.Width == Width && other.Height == Height;
            }

            /// <summary>
            /// Indicates whether this instance and a specified object are equal.
            /// </summary>
            /// <param name="obj">Another object to compare to.</param>
            /// <returns>
            /// true if <paramref name="obj"/> and this instance are the same type and represent the same value; otherwise, false.
            /// </returns>
            public override bool Equals(object obj)
            {
                return obj is IntRect && this == (IntRect)obj;
            }

            /// <summary>
            /// Returns the hash code for this instance.
            /// </summary>
            /// <returns>
            /// A 32-bit signed integer that is the hash code for this instance.
            /// </returns>
            public override int GetHashCode()
            {
                unchecked
                {
                    var result = Left;
                    result = (result * 397) ^ Top;
                    result = (result * 397) ^ Width;
                    result = (result * 397) ^ Height;
                    return result;
                }
            }

            /// <summary>
            /// Implements the operator ==.
            /// </summary>
            /// <param name="left">The left argument.</param>
            /// <param name="right">The right argument.</param>
            /// <returns>The result of the operator.</returns>
            public static bool operator ==(IntRect left, IntRect right)
            {
                return left.Equals(right);
            }

            /// <summary>
            /// Implements the operator !=.
            /// </summary>
            /// <param name="left">The left argument.</param>
            /// <param name="right">The right argument.</param>
            /// <returns>The result of the operator.</returns>
            public static bool operator !=(IntRect left, IntRect right)
            {
                return !left.Equals(right);
            }
        }

        ////////////////////////////////////////////////////////////
    }
}