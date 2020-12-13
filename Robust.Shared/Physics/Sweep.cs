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
        public float Angle;

        public float Angle0;

        /// <summary>
        ///     Fraction of the current time step in the range [0,1]
        ///     Center0 and Angle0 are the positions at alpha0.
        /// </summary>
        public float Alpha0;

        /// <summary>
        ///     Center world positions
        /// </summary>
        public Vector2 Center;

        public Vector2 Center0;

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
        /// <param name="beta">beta is a factor in [0,1], where 0 indicates alpha0.</param>
        public PhysicsTransform GetTransform(float beta)
        {
            var transform = new PhysicsTransform
            {
                Position =
                {
                    X = (1.0f - beta) * Center0.X + beta * Center.X,
                    Y = (1.0f - beta) * Center0.Y + beta * Center.Y
                }
            };
            float angle = (1.0f - beta) * Angle0 + beta * Angle;
            transform.Quaternion.Phase = angle;

            // Shift to origin
            transform.Position -= Complex.Multiply(LocalCenter, transform.Quaternion);
            return transform;
        }

        /// <summary>
        /// Advance the sweep forward, yielding a new initial state.
        /// </summary>
        /// <param name="alpha">new initial time..</param>
        public void Advance(float alpha)
        {
            DebugTools.Assert(Alpha0 < 1.0f);
            float beta = (alpha - Alpha0) / (1.0f - Alpha0);
            Center0 += (Center - Center0) * beta;
            Angle0 += (Angle - Angle0) * beta;
            Alpha0 = alpha;
        }

        /// <summary>
        /// Normalize the angles.
        /// </summary>
        public void Normalize()
        {
            // TODO: TAU MathF.Pi * 2
            float d = MathF.PI * 2 * MathF.Floor(Angle0 / MathF.PI * 2);
            Angle0 -= d;
            Angle -= d;
        }
    }
}
