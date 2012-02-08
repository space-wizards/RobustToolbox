using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace ClientInterfaces
{
    public interface ICollidable
    {
        RectangleF AABB { get; }
        bool IsHardCollidable {get;} // true if collisions should prevent movement, or just trigger bumps.
        void Bump();
    }
}
