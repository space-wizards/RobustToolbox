using System;
using System.Collections.Generic;
using System.Text;
using SS3D_shared.HelperClasses;

//Helper class for the ui. Simple rectangle w/ some useful functions to figure out if the mouse is in it etc.
//Assumes origin (0,0) is top left.

namespace SS3D.HelperClasses
{
    public class Rect2D
    {
        public Vector2 size;
        public Vector2 location;

        public Rect2D(Vector2 Location, Vector2 Size)
        {
            size = Size;
            location = Location;
        }

        public bool ContainsPoint(float x, float y)
        {
            double left = location.X;
            double right = location.X + size.X;
            double top = location.Y;
            double bottom = location.Y + size.Y;
            return x > left && x < right && y < bottom && y > top;
        }

        public bool ContainsRect2D(Rect2D rect2)
        {

            double left = location.X;
            double right = location.X + size.X;
            double top = location.Y;
            double bottom = location.Y + size.Y;

            double left2 = rect2.location.X;
            double right2 = rect2.location.X + rect2.size.X;
            double top2 = rect2.location.Y;
            double bottom2 = rect2.location.Y + rect2.size.Y;

            if (bottom < top2) return (false);
            if (top > bottom2) return (false);
            if (right < left2) return (false);
            if (left > right2) return (false);

            return (true);
        }
    }
}
