using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
