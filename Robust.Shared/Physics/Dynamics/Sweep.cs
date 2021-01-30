using Robust.Shared.Maths;

namespace Robust.Shared.Physics.Dynamics
{
    internal struct Sweep
    {
        public float Angle;

        public float Angle0;

        public float Alpha0;

        /// <summary>
        ///     Generally reflects the body's worldposition but not always.
        ///     Can be used to temporarily store it during CCD.
        /// </summary>
        public Vector2 Center;

        public float Center0;

        // I Didn't copy LocalCenter because it's also on the Body and it will normally be rarely set sooooo
    }
}
