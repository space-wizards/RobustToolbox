using SS14.Shared.Maths;

namespace SS14.Client.Utility
{
    internal static class GodotConversions
    {
        public static Vector2 Convert(this Godot.Vector2 vector2)
        {
            return new Vector2(vector2.x, vector2.y);
        }

        public static Godot.Vector2 Convert(this Vector2 vector2)
        {
            return new Godot.Vector2(vector2.X, vector2.Y);
        }

        public static Vector3 Convert(this Godot.Vector3 vector3)
        {
            return new Vector3(vector3.x, vector3.y, vector3.z);
        }

        public static Godot.Vector3 Convert(this Vector3 vector3)
        {
            return new Godot.Vector3(vector3.X, vector3.Y, vector3.Z);
        }

        public static Color Convert(this Godot.Color color)
        {
            return new Color(color.r, color.g, color.b, color.a);
        }

        public static Godot.Color Convert(this Color color)
        {
            return new Godot.Color(color.R, color.G, color.B, color.A);
        }

        public static Godot.Rect2 Convert(this UIBox2 box)
        {
            return new Godot.Rect2(box.Left, box.Top, box.Width, box.Height);
        }

        public static UIBox2 Convert(this Godot.Rect2 rect)
        {
            return new UIBox2(rect.Position.x, rect.Position.y, rect.End.x, rect.End.y);
        }

        /// <summary>
        ///     Loosely convert a 3x3 matrix into a Godot 2D transform.
        ///     This does not copy over the third row, as Godot's 2D transforms don't have it.
        /// </summary>
        public static Godot.Transform2D Convert(in this Matrix3 matrix)
        {
            return new Godot.Transform2D
            {
                o = new Godot.Vector2(matrix.R0C2, matrix.R1C2),
                x = new Godot.Vector2(matrix.R0C0, matrix.R1C0),
                y = new Godot.Vector2(matrix.R0C1, matrix.R1C1),
            };
        }

        /// <summary>
        ///     Loosely convert a 2D Godot transform into a 3x3 matrix.
        ///     The third row will be initialized to 0, 0, 1 as <see cref="Godot.Transform2D"/> does not carry it.
        /// </summary>
        public static Matrix3 Convert(in this Godot.Transform2D transform)
        {
            return new Matrix3
            {
                R0C0 = transform.x.x, R0C1 = transform.y.x, R0C2 = transform.o.x,
                R1C0 = transform.x.y, R1C1 = transform.y.y, R1C2 = transform.o.y,
                R2C0 = 0, R2C1 = 0, R2C2 = 1
            };
        }
    }
}
