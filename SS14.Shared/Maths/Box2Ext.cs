using System;
using OpenTK;

namespace SS14.Shared.Maths
{
    public static class Box2Ext
    {
        private const float Epsilon = 1.0e-8f;

        public static bool Intersects(this Box2 me, Box2 other)
        {
            return !(me.Bottom < other.Top || me.Top > other.Bottom || me.Left > other.Right || me.Right < other.Left);
        }

        public static bool IsEmpty(this Box2 me)
        {
            return Math.Abs(me.Width) < Epsilon && Math.Abs(me.Height) < Epsilon;
        }

        public static bool Encloses(this Box2 outer, Box2 inner)
        {
            return outer.Left <= inner.Left
                   && inner.Right <= outer.Right
                   && outer.Top <= inner.Top
                   && inner.Bottom <= outer.Bottom;
        }

        /// <summary>
        ///     Uniformly scales the box by a given scalar.
        /// </summary>
        /// <param name="me">Box2 to scale.</param>
        /// <param name="scalar">Value to scale the box by.</param>
        /// <returns>Scaled box.</returns>
        public static Box2 Scale(this Box2 me, float scalar)
        {
            return new Box2(
                me.Left * scalar,
                me.Top * scalar,
                me.Right * scalar,
                me.Bottom * scalar);
        }
    }
}
