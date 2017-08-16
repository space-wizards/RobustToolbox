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
    }
}
