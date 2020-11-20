using System.Collections.Generic;

namespace Robust.Shared.Physics
{
    /// <summary>
    ///     All of the physics components on a particular map.
    /// </summary>
    /// <remarks>
    ///     What you'd call a "World" in some other engines.
    /// </remarks>
    internal sealed class PhysicsMap
    {
        private HashSet<IPhysBody> _bodies = new HashSet<IPhysBody>();

        private HashSet<IPhysBody> _awakeBodies = new HashSet<IPhysBody>();

        public bool AddBody(IPhysBody body)
        {
            if (body.Awake)
                _awakeBodies.Add(body);

            return _bodies.Add(body);
        }

        public bool RemoveBody(IPhysBody body)
        {
            _awakeBodies.Remove(body);
            return _bodies.Remove(body);
        }

        public void Solve(float frameTime)
        {
            foreach (var body in _awakeBodies)
            {

            }
        }
    }
}
