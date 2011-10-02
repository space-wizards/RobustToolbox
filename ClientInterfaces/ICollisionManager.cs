using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace ClientInterfaces
{
    public interface ICollisionManager
    {
        /// <summary>
        /// Checks to see if the specified collision rectangle collides with any of the colliders under management.
        /// Also fires the OnCollide event of the first managed collidable to intersect with the collider.
        /// </summary>
        /// <param name="collider">Collision rectangle to check</param>
        /// <returns>true if collides, false if not</returns>
        bool Collide(RectangleF collider);
        void AddCollidable(ICollidable collidable);
    }
}
