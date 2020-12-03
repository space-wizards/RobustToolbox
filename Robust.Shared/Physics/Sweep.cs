using System;
using System.Reflection.Metadata;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    /// <summary>
    /// This describes the motion of a body/shape for TOI computation.
    /// Shapes are defined with respect to the body origin, which may
    /// no coincide with the center of mass. However, to support dynamics
    /// we must interpolate the center of mass position.
    /// </summary>
    public struct Sweep // moment
    {
        /// <summary>
        ///     World angles
        /// </summary>
        public float A;

        public float A0;

        /// <summary>
        ///     Fraction of the current time step in the range [0,1]
        ///     c0 and a0 are the positions at alpha0.
        /// </summary>
        public float Alpha0;

        /// <summary>
        ///     Center world positions
        /// </summary>
        public Vector2 C;

        public Vector2 C0;

        /// <summary>
        ///     Local center of mass position
        /// </summary>
        public Vector2 LocalCenter;

        // TODO: This shiznit AHHHHHHHHHHHHH
        // TODO: Just make your own PhysicsTransform coz fuck it it's faster I GUESS
        // Long-term probably have like an offset Vector2 you can use instead
        /// <summary>
        /// Get the interpolated transform at a specific time.
        /// </summary>
        /// <param name="xfb">The transform.</param>
        /// <param name="beta">beta is a factor in [0,1], where 0 indicates alpha0.</param>
        public void GetTransform(out ITransformComponent xfb, float beta)
        {
            xfb = new Transform();
            xfb.p.X = (1.0f - beta) * C0.X + beta * C.X;
            xfb.p.Y = (1.0f - beta) * C0.Y + beta * C.Y;
            float angle = (1.0f - beta) * A0 + beta * A;
            xfb.q.Phase = angle;

            // Shift to origin
            xfb.p -= Complex.Multiply(ref LocalCenter, ref xfb.q);
        }

        /// <summary>
        /// Advance the sweep forward, yielding a new initial state.
        /// </summary>
        /// <param name="alpha">new initial time..</param>
        public void Advance(float alpha)
        {
            DebugTools.Assert(Alpha0 < 1.0f);
            float beta = (alpha - Alpha0) / (1.0f - Alpha0);
            C0 += (C - C0) * beta;
            A0 += (A - A0) * beta;
            Alpha0 = alpha;
        }

        /// <summary>
        /// Normalize the angles.
        /// </summary>
        public void Normalize()
        {
            // TODO: TAU MathF.Pi * 2
            float d = MathF.PI * 2 * MathF.Floor(A0 / MathF.PI * 2);
            A0 -= d;
            A -= d;
        }
    }
}
