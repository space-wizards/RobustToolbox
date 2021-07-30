using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics.Dynamics.Joints
{
    public class RevoluteJoint : Joint
    {
        [NonSerialized] private Vector2 _impulse;
        [NonSerialized] private int _indexA;

        public RevoluteJoint(PhysicsComponent bodyA, PhysicsComponent bodyB) : base(bodyA, bodyB)
        {
        }

        public override JointType JointType => JointType.Revolute;

        public override Vector2 WorldAnchorA { get; set; }
        public override Vector2 WorldAnchorB { get; set; }

        public override Vector2 GetReactionForce(float invDt)
        {
            var P = new Vector2(_impulse.X, _impulse.Y);
            return P * invDt;
        }

        public override float GetReactionTorque(float invDt)
        {
            return invDt * (_motorImpulse + _lowerImpulse - _upperImpulse);
        }

        internal override void InitVelocityConstraints(SolverData data)
        {
            _indexA = BodyA.IslandIndex[data.IslandIndex];
	        _indexB = BodyB.IslandIndex[data.IslandIndex];
            _localCenterA = Vector2.Zero; //BodyA->m_sweep.localCenter;
            _localCenterB = Vector2.Zero; //BodyB->m_sweep.localCenter;
	        _invMassA = BodyA.InvMass;
	        _invMassB = BodyB.InvMass;
	        _invIA = BodyA.InvI;
	        _invIB = BodyB.InvI;

	        float aA = data.Angles[_indexA];
	        var vA = data.LinearVelocities[_indexA];
	        float wA = data.AngularVelocities[_indexA];

	        float aB = data.Angles[_indexB];
	        var vB = data.LinearVelocities[_indexB];
	        float wB = data.AngularVelocities[_indexB];

	        Quaternion2D qA = new(aA), qB = new(aB);

	        m_rA = b2Mul(qA, m_localAnchorA - m_localCenterA);
	        m_rB = b2Mul(qB, m_localAnchorB - m_localCenterB);

	        // J = [-I -r1_skew I r2_skew]
	        // r_skew = [-ry; rx]

	        // Matlab
	        // K = [ mA+r1y^2*iA+mB+r2y^2*iB,  -r1y*iA*r1x-r2y*iB*r2x]
	        //     [  -r1y*iA*r1x-r2y*iB*r2x, mA+r1x^2*iA+mB+r2x^2*iB]

	        float mA = m_invMassA, mB = m_invMassB;
	        float iA = m_invIA, iB = m_invIB;

	        m_K.ex.x = mA + mB + m_rA.y * m_rA.y * iA + m_rB.y * m_rB.y * iB;
	        m_K.ey.x = -m_rA.y * m_rA.x * iA - m_rB.y * m_rB.x * iB;
	        m_K.ex.y = m_K.ey.x;
	        m_K.ey.y = mA + mB + m_rA.x * m_rA.x * iA + m_rB.x * m_rB.x * iB;

	        m_axialMass = iA + iB;
	        bool fixedRotation;
	        if (m_axialMass > 0.0f)
	        {
		        m_axialMass = 1.0f / m_axialMass;
		        fixedRotation = false;
	        }
	        else
	        {
		        fixedRotation = true;
	        }

	        m_angle = aB - aA - m_referenceAngle;
	        if (m_enableLimit == false || fixedRotation)
	        {
		        m_lowerImpulse = 0.0f;
		        m_upperImpulse = 0.0f;
	        }

	        if (m_enableMotor == false || fixedRotation)
	        {
		        m_motorImpulse = 0.0f;
	        }

	        if (data.step.warmStarting)
	        {
		        // Scale impulses to support a variable time step.
		        m_impulse *= data.step.dtRatio;
		        m_motorImpulse *= data.step.dtRatio;
		        m_lowerImpulse *= data.step.dtRatio;
		        m_upperImpulse *= data.step.dtRatio;

		        float axialImpulse = m_motorImpulse + m_lowerImpulse - m_upperImpulse;
		        b2Vec2 P(m_impulse.x, m_impulse.y);

		        vA -= mA * P;
		        wA -= iA * (b2Cross(m_rA, P) + axialImpulse);

		        vB += mB * P;
		        wB += iB * (b2Cross(m_rB, P) + axialImpulse);
	        }
	        else
	        {
		        m_impulse.SetZero();
		        m_motorImpulse = 0.0f;
		        m_lowerImpulse = 0.0f;
		        m_upperImpulse = 0.0f;
	        }

	        data.velocities[m_indexA].v = vA;
	        data.velocities[m_indexA].w = wA;
	        data.velocities[m_indexB].v = vB;
	        data.velocities[m_indexB].w = wB;
        }

        internal override void SolveVelocityConstraints(SolverData data)
        {
            var vA = data.LinearVelocities[_indexA];
	        float wA = data.AngularVelocities[_indexA];
	        var vB = data.LinearVelocities[_indexB];
	        float wB = data.AngularVelocities[_indexB];

	        float mA = _invMassA, mB = _invMassB;
	        float iA = _invIA, iB = _invIB;

	        bool fixedRotation = (iA + iB == 0.0f);

	        // Solve motor constraint.
	        if (_enableMotor && !fixedRotation)
	        {
		        float Cdot = wB - wA - _motorSpeed;
		        float impulse = -_axialMass * Cdot;
		        float oldImpulse = _motorImpulse;
		        float maxImpulse = data.step.dt * m_maxMotorTorque;
		        m_motorImpulse = b2Clamp(m_motorImpulse + impulse, -maxImpulse, maxImpulse);
		        impulse = m_motorImpulse - oldImpulse;

		        wA -= iA * impulse;
		        wB += iB * impulse;
	        }

	        if (m_enableLimit && fixedRotation == false)
	        {
		        // Lower limit
		        {
			        float C = m_angle - m_lowerAngle;
			        float Cdot = wB - wA;
			        float impulse = -m_axialMass * (Cdot + b2Max(C, 0.0f) * data.step.inv_dt);
			        float oldImpulse = m_lowerImpulse;
			        m_lowerImpulse = b2Max(m_lowerImpulse + impulse, 0.0f);
			        impulse = m_lowerImpulse - oldImpulse;

			        wA -= iA * impulse;
			        wB += iB * impulse;
		        }

		        // Upper limit
		        // Note: signs are flipped to keep C positive when the constraint is satisfied.
		        // This also keeps the impulse positive when the limit is active.
		        {
			        float C = m_upperAngle - m_angle;
			        float Cdot = wA - wB;
			        float impulse = -m_axialMass * (Cdot + b2Max(C, 0.0f) * data.step.inv_dt);
			        float oldImpulse = m_upperImpulse;
			        m_upperImpulse = b2Max(m_upperImpulse + impulse, 0.0f);
			        impulse = m_upperImpulse - oldImpulse;

			        wA += iA * impulse;
			        wB -= iB * impulse;
		        }
	        }

	        // Solve point-to-point constraint
	        {
		        b2Vec2 Cdot = vB + b2Cross(wB, m_rB) - vA - b2Cross(wA, m_rA);
		        b2Vec2 impulse = m_K.Solve(-Cdot);

		        m_impulse.x += impulse.x;
		        m_impulse.y += impulse.y;

		        vA -= mA * impulse;
		        wA -= iA * b2Cross(m_rA, impulse);

		        vB += mB * impulse;
		        wB += iB * b2Cross(m_rB, impulse);
	        }

	        data.velocities[m_indexA].v = vA;
	        data.velocities[m_indexA].w = wA;
	        data.velocities[m_indexB].v = vB;
	        data.velocities[m_indexB].w = wB;
        }

        internal override bool SolvePositionConstraints(SolverData data)
        {
            b2Vec2 cA = data.positions[m_indexA].c;
	        float aA = data.positions[m_indexA].a;
	        b2Vec2 cB = data.positions[m_indexB].c;
	        float aB = data.positions[m_indexB].a;

	        b2Rot qA(aA), qB(aB);

	        float angularError = 0.0f;
	        float positionError = 0.0f;

	        bool fixedRotation = (m_invIA + m_invIB == 0.0f);

	        // Solve angular limit constraint
	        if (m_enableLimit && fixedRotation == false)
	        {
		        float angle = aB - aA - m_referenceAngle;
		        float C = 0.0f;

		        if (b2Abs(m_upperAngle - m_lowerAngle) < 2.0f * b2_angularSlop)
		        {
			        // Prevent large angular corrections
			        C = b2Clamp(angle - m_lowerAngle, -b2_maxAngularCorrection, b2_maxAngularCorrection);
		        }
		        else if (angle <= m_lowerAngle)
		        {
			        // Prevent large angular corrections and allow some slop.
			        C = b2Clamp(angle - m_lowerAngle + b2_angularSlop, -b2_maxAngularCorrection, 0.0f);
		        }
		        else if (angle >= m_upperAngle)
		        {
			        // Prevent large angular corrections and allow some slop.
			        C = b2Clamp(angle - m_upperAngle - b2_angularSlop, 0.0f, b2_maxAngularCorrection);
		        }

		        float limitImpulse = -m_axialMass * C;
		        aA -= m_invIA * limitImpulse;
		        aB += m_invIB * limitImpulse;
		        angularError = b2Abs(C);
	        }

	        // Solve point-to-point constraint.
	        {
		        qA.Set(aA);
		        qB.Set(aB);
		        b2Vec2 rA = b2Mul(qA, m_localAnchorA - m_localCenterA);
		        b2Vec2 rB = b2Mul(qB, m_localAnchorB - m_localCenterB);

		        b2Vec2 C = cB + rB - cA - rA;
		        positionError = C.Length();

		        float mA = m_invMassA, mB = m_invMassB;
		        float iA = m_invIA, iB = m_invIB;

		        b2Mat22 K;
		        K.ex.x = mA + mB + iA * rA.y * rA.y + iB * rB.y * rB.y;
		        K.ex.y = -iA * rA.x * rA.y - iB * rB.x * rB.y;
		        K.ey.x = K.ex.y;
		        K.ey.y = mA + mB + iA * rA.x * rA.x + iB * rB.x * rB.x;

		        var impulse = -K.Solve(C);

		        cA -= mA * impulse;
		        aA -= iA * Vector2.Cross(rA, impulse);

		        cB += mB * impulse;
		        aB += iB * Vector2.Cross(rB, impulse);
	        }

	        data.Positions[_indexA] = cA;
	        data.Angles[_indexA] = aA;
	        data.Positions[_indexB] = cB;
	        data.Angles[_indexB] = aB;

	        return positionError <= _linearSlop && angularError <= _angularSlop;
        }
    }
}
