using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics.Dynamics.Joints
{
    [Serializable, NetSerializable]
    public class SlothJoint : Joint
    {
        // Solver temp
        [NonSerialized] private int _indexA;
        [NonSerialized] private int _indexB;
        [NonSerialized] private Vector2 _rA;
        [NonSerialized] private Vector2 _rB;
        [NonSerialized] private Vector2 _localCenterA;
        [NonSerialized] private Vector2 _localCenterB;
        [NonSerialized] private float _invMassA;
        [NonSerialized] private float _invMassB;
        [NonSerialized] private float _invIA;
        [NonSerialized] private float _invIB;
        [NonSerialized] private float _mass;
        [NonSerialized] private float _currentLength;
        [NonSerialized] private float _softMass;

        public SlothJoint(PhysicsComponent bodyA, PhysicsComponent bodyB) : base(bodyA, bodyB)
        {
        }

        public override JointType JointType => JointType.Distance;

        [field:NonSerialized]
        public override Vector2 WorldAnchorA { get; set; }
        [field:NonSerialized]
        public override Vector2 WorldAnchorB { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float MaxLength
        {
            get => _maxLength;
            set
            {
                if (MathHelper.CloseTo(value, _maxLength)) return;

                _maxLength = value;
                Dirty();
            }
        }

        private float _maxLength;

        public override Vector2 GetReactionForce(float invDt)
        {
            // TODO: Need break force
            return Vector2.Zero;
        }

        public override float GetReactionTorque(float invDt)
        {
            return 0f;
        }

        internal override void InitVelocityConstraints(SolverData data)
        {
            _indexA = BodyA.IslandIndex;
            _indexB = BodyB.IslandIndex;
            _localCenterA = Vector2.Zero; //BodyA->m_sweep.localCenter;
            _localCenterB = Vector2.Zero; //BodyB->m_sweep.localCenter;
            _invMassA = BodyA.InvMass;
            _invMassB = BodyB.InvMass;
            _invIA = BodyA.InvI;
            _invIB = BodyB.InvI;

            _currentLength = (data.Positions[_indexA] - data.Positions[_indexB]).Length;

            _softMass = _mass;
        }

        internal override void SolveVelocityConstraints(SolverData data)
        {
            if (_currentLength < _maxLength) return;

            var posA = data.Positions[_indexA];
            var posB = data.Positions[_indexB];

            var vA = data.LinearVelocities[_indexA];
            float wA = data.AngularVelocities[_indexA];
            var vB = data.LinearVelocities[_indexB];
            float wB = data.AngularVelocities[_indexB];

            var correctionDistance = _maxLength - _currentLength;

            //var P = _u * impulse;
            //vA -= P * _invMassA;
            //wA -= _invIA * Vector2.Cross(_rA, P);
            //vB += P * _invMassB;
            //wB += _invIB * Vector2.Cross(_rB, P);
        }

        internal override bool SolvePositionConstraints(SolverData data)
        {
            // TODO: Should use these? IDK
            return true;
        }
    }
}
