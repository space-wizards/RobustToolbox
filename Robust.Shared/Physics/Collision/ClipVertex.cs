using Robust.Shared.Maths;

namespace Robust.Shared.Physics.Collision
{
    /// <summary>
    /// Used for computing contact manifolds.
    /// </summary>
    internal struct ClipVertex
    {
        public ContactID ID;
        public Vector2 V;
    }
}
