using System;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics.Dynamics.Contacts
{
    internal sealed class ContactSolver
    {
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;

        private Vector2[] _linearVelocities = Array.Empty<Vector2>();
        private float[] _angularVelocities = Array.Empty<float>();

        private Vector2[] _positions = Array.Empty<Vector2>();
        private float[] _angles = Array.Empty<float>();

        private Contact[] _contacts = Array.Empty<Contact>();
        private int _contactCount;

        private ContactVelocityConstraint[] _velocityConstraints = Array.Empty<ContactVelocityConstraint>();
        private ContactPositionConstraint[] _positionConstraints = Array.Empty<ContactPositionConstraint>();

        public void Initialize()
        {
            IoCManager.InjectDependencies(this);
        }

        public void Reset(int contactCount, Contact[] contacts, Vector2[] linearVelocities, float[] angularVelocities, Vector2[] positions, float[] angles)
        {
            _linearVelocities = linearVelocities;
            _angularVelocities = angularVelocities;

            _positions = positions;
            _angles = angles;

            _contactCount = contactCount;
            _contacts = contacts;

            // If we need more constraints then grow the cached arrays
            if (_velocityConstraints.Length < contactCount)
            {
                var oldLength = _velocityConstraints.Length;

                Array.Resize(ref _velocityConstraints, contactCount * 2);
                Array.Resize(ref _positionConstraints, contactCount * 2);

                for (var i = oldLength; i < _velocityConstraints.Length; i++)
                {
                    _velocityConstraints[i] = new ContactVelocityConstraint();
                    _positionConstraints[i] = new ContactPositionConstraint();
                }
            }

            // Build constraints
            // For now these are going to be bare but will change
            for (var i = 0; i < _contactCount; i++)
            {
                var contact = contacts[i];
                var bodyA = contact.Manifold.A;
                var bodyB = contact.Manifold.B;

                // TODO: Set data.
                var velocityConstraint = _velocityConstraints[i];
                velocityConstraint.ContactIndex = i;
                velocityConstraint.IndexA = bodyA.IslandIndex;
                velocityConstraint.IndexB = bodyB.IslandIndex;

                var positionConstraint = _positionConstraints[i];
                positionConstraint.IndexA = bodyA.IslandIndex;
                positionConstraint.IndexB = bodyB.IslandIndex;
            }
        }

        public void InitializeVelocityConstraints()
        {
            for (var i = 0; i < _contactCount; i++)
            {
                var velocityConstraint = _velocityConstraints[i];
                var positionConstraint = _positionConstraints[i];
            }
        }

        public void SolveVelocityConstraints()
        {
            // Just our old solver for now.
            for (var i = 0; i < _contactCount; i++)
            {
                var velocityConstraint = _velocityConstraints[i];
                var manifold = _contacts[velocityConstraint.ContactIndex].Manifold;

                var indexA = velocityConstraint.IndexA;
                var indexB = velocityConstraint.IndexB;

                var invMassA = manifold.A.InvMass;
                var invMassB = manifold.B.InvMass;

                var velocityA = _linearVelocities[indexA];
                var velocityB = _linearVelocities[indexB];

                var restitution = 0.01f;
                var normal = manifold.Normal;
                var rV = velocityB - velocityA;

                var vAlongNormal = Vector2.Dot(rV, normal);
                if (vAlongNormal > 0)
                {
                    continue;
                }

                var impulse = -(1.0f + restitution) * vAlongNormal;
                impulse /= invMassA + invMassB;

                var normalImpulse = manifold.Normal * impulse;

                _linearVelocities[indexA] = velocityA - normalImpulse * invMassA;
                _linearVelocities[indexB] = velocityB + normalImpulse * invMassB;
            }
        }

        /// <summary>
        ///     Tries to solve positions for all contacts specified.
        /// </summary>
        /// <returns>true if all positions solved</returns>
        public bool SolvePositionConstraints()
        {
            var divisions = 1.0f;
            const float allowance = 1 / 128.0f;
            var percent = MathHelper.Clamp(0.4f / divisions, 0.01f, 1f);
            var done = true;

            // Based off of Randy Gaul's ImpulseEngine code
            // https://github.com/RandyGaul/ImpulseEngine/blob/5181fee1648acc4a889b9beec8e13cbe7dac9288/Manifold.cpp#L123a

            for (var i = 0; i < _contactCount; i++)
            {
                var positionConstraint = _positionConstraints[i];

                var indexA = positionConstraint.IndexA;
                var indexB = positionConstraint.IndexB;

                // TODO: Need to cache these
                var manifold = _contacts[_velocityConstraints[i].ContactIndex].Manifold;

                var bodyA = manifold.A;
                var bodyB = manifold.B;

                var penetration = _physicsManager.CalculatePenetration(bodyA, bodyB);

                if (penetration <= allowance)
                    continue;

                done = false;
                //var correction = collision.Normal * Math.Abs(penetration) * percent;
                var correction = manifold.Normal * Math.Max(penetration - allowance, 0.0f) / (bodyA.InvMass + bodyB.InvMass) * percent;

                var positionA = _positions[indexA];
                _positions[indexA] = positionA - correction * bodyA.InvMass;

                var positionB = _positions[indexB];
                _positions[indexB] = positionB + correction * bodyB.InvMass;
            }

            return done;
        }
    }
}
