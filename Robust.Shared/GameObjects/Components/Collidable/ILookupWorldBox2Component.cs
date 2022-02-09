using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects
{
    public interface ILookupWorldBox2Component
    {
        /// <summary>
        /// Return the local AABB of this entity, assuming position 0,0 and rotation 0.
        /// </summary>
        Box2 GetLocalAABB();
    }
}
