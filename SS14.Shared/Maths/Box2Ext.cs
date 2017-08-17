using OpenTK;

namespace SS14.Shared.Maths
{
    public static class Box2Ext
    {
        public static bool Intersects(this Box2 me, Box2 other)
        {
            return !(me.Bottom < other.Top || me.Top > other.Bottom || me.Left > other.Right || me.Right < other.Left);
        }

        public static bool IsEmpty(this Box2 rect)
        {
            return rect.Left == 0 && rect.Top == 0 && rect.Width == 0 && rect.Height == 0;
        }

        public static bool Encloses(this Box2 outer, Box2 inner)
        {
            return outer.Left <= inner.Left
                && inner.Right <= outer.Right
                && outer.Top <= inner.Top
                && inner.Bottom <= outer.Bottom;
        }
    }
}
