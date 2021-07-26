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

        /// <summary>
        /// Get / create a cached transform for physics use.
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

        /// <inheritdoc />
        public Transform GetTransform(PhysicsComponent body)
        {
            if (_transforms.TryGetValue(body, out var transform))
            {
                return transform;
            }

            transform = body.GetTransform();
            _transforms[body] = transform;

            return transform;
        }
    }
}
