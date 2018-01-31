using SS14.Shared.Maths;

namespace SS14.Client.Utility
{
    public static class GodotConversions
    {
        public static Vector2 Convert(this Godot.Vector2 vector2)
        {
            return new Vector2(vector2.x, vector2.y);
        }

        public static Godot.Vector2 Convert(this Vector2 vector2)
        {
            return new Godot.Vector2(vector2.X, vector2.Y);
        }

        public static Color Convert(this Godot.Color color)
        {
            return new Color(color.r, color.g, color.b, color.a);
        }

        public static Godot.Color Convert(this Color color)
        {
            return new Godot.Color(color.R, color.G, color.B, color.A);
        }

        public static Godot.Rect2 Convert(this Box2 box)
        {
            return new Godot.Rect2(box.Left, box.Top, box.Width, box.Height);
        }

        public static Box2 Convert(this Godot.Rect2 rect)
        {
            return new Box2(rect.Position.x, rect.Position.y, rect.End.x, rect.End.y);
        }
    }
}
