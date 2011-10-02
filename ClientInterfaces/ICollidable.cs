using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace ClientInterfaces
{
    public interface ICollidable
    {
        event EventHandler OnCollide;
        RectangleF AABB { get; }
    }
}
