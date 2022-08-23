/*
* Farseer Physics Engine:
* Copyright (c) 2012 Ian Qvist
*
* Original source Box2D:
* Copyright (c) 2006-2011 Erin Catto http://www.box2d.org
*
* This software is provided 'as-is', without any express or implied
* warranty.  In no event will the authors be held liable for any damages
* arising from the use of this software.
* Permission is granted to anyone to use this software for any purpose,
* including commercial applications, and to alter it and redistribute it
* freely, subject to the following restrictions:
* 1. The origin of this software must not be misrepresented; you must not
* claim that you wrote the original software. If you use this software
* in a product, an acknowledgment in the product documentation would be
* appreciated but is not required.
* 2. Altered source versions must be plainly marked as such, and must not be
* misrepresented as being the original software.
* 3. This notice may not be removed or altered from any source distribution.
*/

using System;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Dynamics
{
    /// This describes the motion of a body/shape for TOI computation.
    /// Shapes are defined with respect to the body origin, which may
    /// no coincide with the center of mass. However, to support dynamics
    /// we must interpolate the center of mass position.
    internal struct Sweep
    {
        /// <summary>
        /// Local center of mass position
        /// </summary>
        public Vector2 LocalCenter;

        // AKA A in box2d
        public float Angle;

        // AKA A0 in box2d
        /// <summary>
        /// Fraction of the current time step in the range [0,1]
        /// c0 and a0 are the positions at alpha0.
        /// </summary>
        public float Angle0;

        public float Alpha0;

        // AKA C in box2d
        public Vector2 Center;

        // AKA C0 in box2d
        public Vector2 Center0;

        /// <summary>
        /// Get the interpolated transform at a specific time.
        /// </summary>
        /// <param name="beta">beta is a factor in [0,1], where 0 indicates alpha0.</param>
        /// <returns>the output transform</returns>
        public Transform GetTransform(float beta)
        {
            var xf = new Transform(Center0 * (1.0f - beta) + Center * beta, (1.0f - beta) * Angle0 + beta * Angle);

            // Shift to origin
            xf.Position -= Transform.Mul(xf.Quaternion2D, LocalCenter);

            return xf;
        }

        /// <summary>
        /// Advance the sweep forward, yielding a new initial state.
        /// </summary>
        /// <param name="alpha">the new initial time.</param>
        public void Advance(float alpha)
        {
            DebugTools.Assert(Alpha0 < 1.0f);
            float beta = (alpha - Alpha0) / (1.0f - Alpha0);
            Center0 += (Center - Center0) * beta;
            Angle0 += beta * (Angle - Angle0);
            Alpha0 = alpha;
        }

        /// <summary>
        /// Normalize the angles.
        /// </summary>
        public void Normalize()
        {
            float twoPi = 2.0f * MathF.PI;
            float d =  twoPi * MathF.Floor(Angle0 / twoPi);
            Angle0 -= d;
            Angle -= d;
        }
    }
}
