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

        public bool CreateTransform(PhysicsComponent body);

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

            return new(xformComp.WorldPosition, (float) xformComp.WorldRotation.Theta);
        }

        /// <inheritdoc />
        public void ClearTransforms()
        {
            _transforms.Clear();
        }

        public bool CreateTransform(PhysicsComponent body)
        {
            return CreateTransform(body.Owner.Uid);
        }

        public bool CreateTransform(EntityUid uid)
        {
            if (_transforms.ContainsKey(uid)) return false;

            _transforms[uid] = GetPhysicsTransform(uid);
            return true;
        }

        public Transform EnsureTransform(PhysicsComponent body)
        {
            CreateTransform(body);
            return _transforms[body.Owner.Uid];
        }

        public Transform EnsureTransform(EntityUid uid)
        {
            CreateTransform(uid);
            return _transforms[uid];
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
            return _transforms[body.Owner.Uid];
        }

        public Transform GetTransform(EntityUid uid)
        {
            return _transforms[uid];
        }
    }
}
