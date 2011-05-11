using System;
using System.Collections.Generic;
using System.Text;

using Mogre;

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
            float left = location.x;
            float right = location.x + size.x;
            float top = location.y;
            float bottom = location.y + size.y;
            return x > left && x < right && y < bottom && y > top;
        }

        public bool ContainsRect2D(Rect2D rect2)
        {

            float left = location.x;
            float right = location.x + size.x;
            float top = location.y;
            float bottom = location.y + size.y;

            float left2 = rect2.location.x;
            float right2 = rect2.location.x + rect2.size.x;
            float top2 = rect2.location.y;
            float bottom2 = rect2.location.y + rect2.size.y;

            if (bottom < top2) return (false);
            if (top > bottom2) return (false);
            if (right < left2) return (false);
            if (left > right2) return (false);

            return (true);
        }
    }
}
