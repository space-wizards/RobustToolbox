using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Shared.Physics
{
    public interface IPhysicsManager
    {
        /// <summary>
        /// Clear all of the cached transforms.
        /// </summary>
        void ClearTransforms();

        public Transform EnsureTransform(PhysicsComponent body);

        public Transform EnsureTransform(EntityUid uid);

        public void SetTransform(EntityUid uid, Transform transform);

        public Transform UpdateTransform(EntityUid uid);

        /// <summary>
        /// Get a cached transform for physics use.
        /// </summary>
        public Transform GetTransform(PhysicsComponent body);
    }

    public sealed class PhysicsManager : IPhysicsManager
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        private Dictionary<EntityUid, Transform> _transforms = new(64);

        private Transform GetPhysicsTransform(EntityUid uid)
        {
            var xformComp = _entManager.GetComponent<TransformComponent>(uid);
            var (worldPos, worldRot) = xformComp.GetWorldPositionRotation();

            return new(worldPos, (float) worldRot.Theta);
        }

        /// <inheritdoc />
        public void ClearTransforms()
        {
            _transforms.Clear();
        }

        public Transform EnsureTransform(PhysicsComponent body)
        {
            return EnsureTransform((body).Owner);
        }

        public Transform EnsureTransform(EntityUid uid)
        {
            if (!_transforms.TryGetValue(uid, out var transform))
            {
                transform = GetPhysicsTransform(uid);
                _transforms[uid] = transform;
            }

            return transform;
        }

        public void SetTransform(EntityUid uid, Transform transform)
        {
            _transforms[uid] = transform;
        }

        public Transform UpdateTransform(EntityUid uid)
        {
            var xform = GetPhysicsTransform(uid);
            _transforms[uid] = xform;
            return xform;
        }

        /// <inheritdoc />
        public Transform GetTransform(PhysicsComponent body)
        {
            return _transforms[body.Owner];
        }

        public Transform GetTransform(EntityUid uid)
        {
            return _transforms[uid];
        }
    }
}
