using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision;

namespace Robust.Shared.Physics.Dynamics.Contacts
{
    internal sealed class ContactPositionConstraint
    {
        /// <summary>
        ///     Index of BodyA in the island.
        /// </summary>
        public int IndexA { get; set; }

        /// <summary>
        ///     Index of BodyB in the island.
        /// </summary>
        public int IndexB { get; set; }

        public Vector2[] LocalPoints = new Vector2[2];

        public Vector2 LocalNormal;

        public Vector2 LocalPoint;

        public float InvMassA;

        public float InvMassB;

        public Vector2 LocalCenterA;

        public Vector2 LocalCenterB;

        public float InvIA;

        public float InvIB;

        public ManifoldType Type;

        public float RadiusA;

        public float RadiusB;

        public int PointCount;
    }
}
