using OpenTK;

namespace SS14.Shared.Maths
{
    public static class Box2Ext
    {
        public static bool Intersects(this Box2 me, Box2 other)
        {
            return !(me.Bottom < other.Top || me.Top > other.Bottom || me.Left > other.Right || me.Right < other.Left);
        }
    }
}
