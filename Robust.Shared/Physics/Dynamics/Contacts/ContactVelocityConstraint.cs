using Robust.Shared.Maths;

namespace Robust.Shared.Physics.Dynamics.Contacts
{
    internal sealed class ContactVelocityConstraint
    {
        public int ContactIndex { get; set; }

        /// <summary>
        ///     Index of BodyA in the island.
        /// </summary>
        public int IndexA { get; set; }

        /// <summary>
        ///     Index of BodyB in the island.
        /// </summary>
        public int IndexB { get; set; }

        // Use 2 as its the max number of manifold points.
        public VelocityConstraintPoint[] Points = new VelocityConstraintPoint[2];

        public Vector2 Normal;

        public Vector2[] NormalMass = new Vector2[2];

        public Vector2[] K = new Vector2[2];

        public float InvMassA;

        public float InvMassB;

        public float InvIA;

        public float InvIB;

        public float Friction;

        public float Restitution;

        public float TangentSpeed;

        public int PointCount;

        public ContactVelocityConstraint()
        {
            for (var i = 0; i < 2; i++)
            {
                Points[i] = new VelocityConstraintPoint();
            }
        }
    }

    internal sealed class VelocityConstraintPoint
    {
        public Vector2 RelativeVelocityA;

        public Vector2 RelativeVelocityB;

        public float NormalImpulse;

        public float TangentImpulse;

        public float NormalMass;

        public float TangentMass;

        public float VelocityBias;
    }
}
