using System.Drawing;
using System.Collections.Generic;
using ClientInterfaces.GOC;
using SS13_Shared;
using SS13_Shared.GO;
using System.Xml.Linq;

namespace ClientInterfaces.Collision
{
    public interface ICollidable
    {
        RectangleF AABB { get; }
        bool IsHardCollidable {get;} // true if collisions should prevent movement, or just trigger bumps.
        void Bump(IEntity ent);
    }
}
