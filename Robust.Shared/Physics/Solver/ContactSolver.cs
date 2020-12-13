using Robust.Shared.Maths;

namespace Robust.Shared.Physics.Solver
{
    public sealed class ContactPositionConstraint
    {
        public Vector2[] LocalPoints = new Vector2[Settings.MaxManifoldPoints];
        public Vector2 LocalNormal;
        public Vector2 LocalPoint;
        public int IndexA;
        public int IndexB;
        public float InvMassA, InvMassB;
        public Vector2 localCenterA, localCenterB;
        public float invIA, invIB;
        public ManifoldType Type;
        public float RadiusA, RadiusB;
        public int PointCount;
    }

    public sealed class VelocityConstraintPoint
    {
        public Vector2 RelativeVelocityA;
        public Vector2 RelativeVelocityB;
        public float NormalImpulse;
        public float TangentImpulse;
        public float NormalMass;
        public float TangentMass;
        public float VelocityBias;
    }

    public sealed class ContactVelocityConstraint
    {
        public VelocityConstraintPoint[] points = new VelocityConstraintPoint[Settings.MaxManifoldPoints];
        public Vector2 normal;
        public Mat22 normalMass;
        public Mat22 K;
        public int indexA;
        public int indexB;
        public float invMassA, invMassB;
        public float invIA, invIB;
        public float friction;
        public float restitution;
        public float tangentSpeed;
        public int pointCount;
        public int contactIndex;

        public ContactVelocityConstraint()
        {
            for (int i = 0; i < Settings.MaxManifoldPoints; i++)
            {
                points[i] = new VelocityConstraintPoint();
            }
        }
    }

    public sealed class ContactSolver
    {
        // CHUNKY

    }
}
