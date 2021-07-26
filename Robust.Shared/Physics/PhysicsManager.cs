using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Physics
{
    public interface IPhysicsManager
    {
        /// <summary>
        /// Clear all of the cached transforms.
        /// </summary>
        void ClearTransforms();

        public bool CreateTransform(PhysicsComponent body);

        public Transform GetOrCreateTransform(PhysicsComponent body);

        /// <summary>
        /// Get a cached transform for physics use.
        /// </summary>
        public Transform GetTransform(PhysicsComponent body);
    }

    public sealed class PhysicsManager : IPhysicsManager
    {
        private Dictionary<PhysicsComponent, Transform> _transforms = new(64);

        /// <inheritdoc />
        public void ClearTransforms()
        {
            _transforms.Clear();
        }

        public bool CreateTransform(PhysicsComponent body)
        {
            if (_transforms.ContainsKey(body)) return false;

            _transforms[body] = body.GetTransform();
            return true;
        }

        public Transform GetOrCreateTransform(PhysicsComponent body)
        {
            CreateTransform(body);
            return _transforms[body];
        }

        /// <inheritdoc />
        public Transform GetTransform(PhysicsComponent body)
        {
            return _transforms[body];
        }
    }
}
