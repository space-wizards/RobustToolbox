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
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems;

public abstract partial class SharedPhysicsSystem
{
    private void ResetSolver(
        in SolverData data,
        in IslandData island,
        ContactVelocityConstraint[] velocityConstraints,
        ContactPositionConstraint[] positionConstraints)
    {
        var contactCount = island.Contacts.Count;

        // Build constraints
        // For now these are going to be bare but will change
        for (var i = 0; i < contactCount; i++)
        {
            var contact = island.Contacts[i];
            Fixture fixtureA = contact.FixtureA!;
            Fixture fixtureB = contact.FixtureB!;
            var shapeA = fixtureA.Shape;
            var shapeB = fixtureB.Shape;
            float radiusA = shapeA.Radius;
            float radiusB = shapeB.Radius;
            var bodyA = contact.BodyA!;
            var bodyB = contact.BodyB!;
            var manifold = contact.Manifold;

            int pointCount = manifold.PointCount;
            DebugTools.Assert(pointCount > 0);

            ref var velocityConstraint = ref velocityConstraints[i];
            velocityConstraint.Friction = contact.Friction;
            velocityConstraint.Restitution = contact.Restitution;
            velocityConstraint.TangentSpeed = contact.TangentSpeed;
            velocityConstraint.IndexA = bodyA.IslandIndex[island.Index];
            velocityConstraint.IndexB = bodyB.IslandIndex[island.Index];
            // Don't need to reset point data as it all gets set below.

            var (invMassA, invMassB) = GetInvMass(bodyA, bodyB);

            (velocityConstraint.InvMassA, velocityConstraint.InvMassB) = (invMassA, invMassB);
            velocityConstraint.InvIA = bodyA.InvI;
            velocityConstraint.InvIB = bodyB.InvI;
            velocityConstraint.ContactIndex = i;
            velocityConstraint.PointCount = pointCount;

            velocityConstraint.K = System.Numerics.Vector4.Zero;
            velocityConstraint.NormalMass = System.Numerics.Vector4.Zero;

            ref var positionConstraint = ref positionConstraints[i];
            positionConstraint.IndexA = bodyA.IslandIndex[island.Index];
            positionConstraint.IndexB = bodyB.IslandIndex[island.Index];
            (positionConstraint.InvMassA, positionConstraint.InvMassB) = (invMassA, invMassB);
            positionConstraint.LocalCenterA = bodyA.LocalCenter;
            positionConstraint.LocalCenterB = bodyB.LocalCenter;

            positionConstraint.InvIA = bodyA.InvI;
            positionConstraint.InvIB = bodyB.InvI;
            positionConstraint.LocalNormal = manifold.LocalNormal;
            positionConstraint.LocalPoint = manifold.LocalPoint;
            positionConstraint.PointCount = pointCount;
            positionConstraint.RadiusA = radiusA;
            positionConstraint.RadiusB = radiusB;
            positionConstraint.Type = manifold.Type;
            var points = manifold.Points.AsSpan;
            var posPoints = positionConstraint.LocalPoints.AsSpan;
            var velPoints = velocityConstraint.Points.AsSpan;

            for (var j = 0; j < pointCount; ++j)
            {
                var contactPoint = points[j];
                ref var constraintPoint = ref velPoints[j];

                if (_warmStarting)
                {
                    constraintPoint.NormalImpulse = data.DtRatio * contactPoint.NormalImpulse;
                    constraintPoint.TangentImpulse = data.DtRatio * contactPoint.TangentImpulse;
                }
                else
                {
                    constraintPoint.NormalImpulse = 0.0f;
                    constraintPoint.TangentImpulse = 0.0f;
                }

                constraintPoint.RelativeVelocityA = Vector2.Zero;
                constraintPoint.RelativeVelocityB = Vector2.Zero;
                constraintPoint.NormalMass = 0.0f;
                constraintPoint.TangentMass = 0.0f;
                constraintPoint.VelocityBias = 0.0f;

                posPoints[j] = contactPoint.LocalPoint;
            }
        }
    }

    private (float, float) GetInvMass(PhysicsComponent bodyA, PhysicsComponent bodyB)
    {
        // God this is shitcodey but uhhhh we need to snowflake KinematicController for nice collisions.
        // TODO: Might need more finagling with the kinematic bodytype
        switch (bodyA.BodyType)
        {
            case BodyType.Kinematic:
            case BodyType.Static:
                return (bodyA.InvMass, bodyB.InvMass);
            case BodyType.KinematicController:
                switch (bodyB.BodyType)
                {
                    case BodyType.Kinematic:
                    case BodyType.Static:
                        return (bodyA.InvMass, bodyB.InvMass);
                    case BodyType.Dynamic:
                        return (bodyA.InvMass, 0f);
                    case BodyType.KinematicController:
                        return (0f, 0f);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            case BodyType.Dynamic:
                switch (bodyB.BodyType)
                {
                    case BodyType.Kinematic:
                    case BodyType.Static:
                    case BodyType.Dynamic:
                        return (bodyA.InvMass, bodyB.InvMass);
                    case BodyType.KinematicController:
                        return (0f, bodyB.InvMass);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void InitializeVelocityConstraints(
        in SolverData data,
        in IslandData island,
        ContactVelocityConstraint[] velocityConstraints,
        ContactPositionConstraint[] positionConstraints,
        Vector2[] positions,
        float[] angles,
        Vector2[] linearVelocities,
        float[] angularVelocities)
    {
        Span<Vector2> points = stackalloc Vector2[2];
        var contactCount = island.Contacts.Count;
        var contacts = island.Contacts;
        var offset = island.Offset;

        for (var i = 0; i < contactCount; ++i)
        {
            ref var velocityConstraint = ref velocityConstraints[i];
            var positionConstraint = positionConstraints[i];

            var radiusA = positionConstraint.RadiusA;
            var radiusB = positionConstraint.RadiusB;
            var manifold = contacts[velocityConstraint.ContactIndex].Manifold;

            var indexA = velocityConstraint.IndexA;
            var indexB = velocityConstraint.IndexB;

            var invMassA = velocityConstraint.InvMassA;
            var invMassB = velocityConstraint.InvMassB;
            var invIA = velocityConstraint.InvIA;
            var invIB = velocityConstraint.InvIB;
            var localCenterA = positionConstraint.LocalCenterA;
            var localCenterB = positionConstraint.LocalCenterB;

            var centerA = positions[indexA];
            var angleA = angles[indexA];
            var linVelocityA = linearVelocities[offset + indexA];
            var angVelocityA = angularVelocities[offset + indexA];

            var centerB = positions[indexB];
            var angleB = angles[indexB];
            var linVelocityB = linearVelocities[offset + indexB];
            var angVelocityB = angularVelocities[offset + indexB];

            DebugTools.Assert(manifold.PointCount > 0);

            var xfA = new Transform(angleA);
            var xfB = new Transform(angleB);
            xfA.Position = centerA - Physics.Transform.Mul(xfA.Quaternion2D, localCenterA);
            xfB.Position = centerB - Physics.Transform.Mul(xfB.Quaternion2D, localCenterB);

            InitializeManifold(ref manifold, xfA, xfB, radiusA, radiusB, out var normal, points);

            velocityConstraint.Normal = normal;

            int pointCount = velocityConstraint.PointCount;
            var velPoints = velocityConstraint.Points.AsSpan;

            for (int j = 0; j < pointCount; ++j)
            {
                ref var vcp = ref velPoints[j];

                vcp.RelativeVelocityA = points[j] - centerA;
                vcp.RelativeVelocityB = points[j] - centerB;

                float rnA = Vector2Helpers.Cross(vcp.RelativeVelocityA, velocityConstraint.Normal);
                float rnB = Vector2Helpers.Cross(vcp.RelativeVelocityB, velocityConstraint.Normal);

                float kNormal = invMassA + invMassB + invIA * rnA * rnA + invIB * rnB * rnB;

                vcp.NormalMass = kNormal > 0.0f ? 1.0f / kNormal : 0.0f;

                Vector2 tangent = Vector2Helpers.Cross(velocityConstraint.Normal, 1.0f);

                float rtA = Vector2Helpers.Cross(vcp.RelativeVelocityA, tangent);
                float rtB = Vector2Helpers.Cross(vcp.RelativeVelocityB, tangent);

                float kTangent = invMassA + invMassB + invIA * rtA * rtA + invIB * rtB * rtB;

                vcp.TangentMass = kTangent > 0.0f ? 1.0f / kTangent : 0.0f;

                // Setup a velocity bias for restitution.
                vcp.VelocityBias = 0.0f;
                float vRel = Vector2.Dot(velocityConstraint.Normal, linVelocityB + Vector2Helpers.Cross(angVelocityB, vcp.RelativeVelocityB) - linVelocityA - Vector2Helpers.Cross(angVelocityA, vcp.RelativeVelocityA));
                if (vRel < -data.VelocityThreshold)
                {
                    vcp.VelocityBias = -velocityConstraint.Restitution * vRel;
                }
            }

            // If we have two points, then prepare the block solver.
            if (velocityConstraint.PointCount == 2)
            {
                var vcp1 = velocityConstraint.Points._00;
                var vcp2 = velocityConstraint.Points._01;

                var rn1A = Vector2Helpers.Cross(vcp1.RelativeVelocityA, velocityConstraint.Normal);
                var rn1B = Vector2Helpers.Cross(vcp1.RelativeVelocityB, velocityConstraint.Normal);
                var rn2A = Vector2Helpers.Cross(vcp2.RelativeVelocityA, velocityConstraint.Normal);
                var rn2B = Vector2Helpers.Cross(vcp2.RelativeVelocityB, velocityConstraint.Normal);

                var k11 = invMassA + invMassB + invIA * rn1A * rn1A + invIB * rn1B * rn1B;
                var k22 = invMassA + invMassB + invIA * rn2A * rn2A + invIB * rn2B * rn2B;
                var k12 = invMassA + invMassB + invIA * rn1A * rn2A + invIB * rn1B * rn2B;

                // Ensure a reasonable condition number.
                const float k_maxConditionNumber = 1000.0f;
                if (k11 * k11 < k_maxConditionNumber * (k11 * k22 - k12 * k12))
                {
                    // K is safe to invert.
                    velocityConstraint.K = new System.Numerics.Vector4(k11, k12, k12, k22);

                    velocityConstraint.NormalMass = Vector4Helpers.Inverse(velocityConstraint.K);
                }
                else
                {
                    // The constraints are redundant, just use one.
                    // TODO_ERIN use deepest?
                    velocityConstraint.PointCount = 1;
                }
            }
        }
    }

    private void WarmStart(
        in SolverData data,
        in IslandData island,
        ContactVelocityConstraint[] velocityConstraints,
        Vector2[] linearVelocities,
        float[] angularVelocities)
    {
        var offset = island.Offset;

        for (var i = 0; i < island.Contacts.Count; ++i)
        {
            var velocityConstraint = velocityConstraints[i];
            var velPoints = velocityConstraint.Points.AsSpan;

            var indexA = velocityConstraint.IndexA;
            var indexB = velocityConstraint.IndexB;
            var invMassA = velocityConstraint.InvMassA;
            var invIA = velocityConstraint.InvIA;
            var invMassB = velocityConstraint.InvMassB;
            var invIB = velocityConstraint.InvIB;
            var pointCount = velocityConstraint.PointCount;

            ref var linVelocityA = ref linearVelocities[offset + indexA];
            ref var angVelocityA = ref angularVelocities[offset + indexA];
            ref var linVelocityB = ref linearVelocities[offset + indexB];
            ref var angVelocityB = ref angularVelocities[offset + indexB];

            var normal = velocityConstraint.Normal;
            var tangent = Vector2Helpers.Cross(normal, 1.0f);

            for (var j = 0; j < pointCount; ++j)
            {
                var constraintPoint = velPoints[j];
                var P = normal * constraintPoint.NormalImpulse + tangent * constraintPoint.TangentImpulse;
                angVelocityA -= invIA * Vector2Helpers.Cross(constraintPoint.RelativeVelocityA, P);
                linVelocityA -= P * invMassA;
                angVelocityB += invIB * Vector2Helpers.Cross(constraintPoint.RelativeVelocityB, P);
                linVelocityB += P * invMassB;
            }
        }
    }

    private void SolveVelocityConstraints(IslandData island,
        ParallelOptions? options,
        ContactVelocityConstraint[] velocityConstraints,
        Vector2[] linearVelocities,
        float[] angularVelocities)
    {
        var contactCount = island.Contacts.Count;

        if (options != null && contactCount > VelocityConstraintsPerThread * 2)
        {
            var batches = (int) Math.Ceiling((float) contactCount / VelocityConstraintsPerThread);

            Parallel.For(0, batches, options, i =>
            {
                var start = i * VelocityConstraintsPerThread;
                var end = Math.Min(start + VelocityConstraintsPerThread, contactCount);
                SolveVelocityConstraints(island, start, end, velocityConstraints, linearVelocities, angularVelocities);
            });
        }
        else
        {
            SolveVelocityConstraints(island, 0, contactCount, velocityConstraints, linearVelocities, angularVelocities);
        }
    }

    private void SolveVelocityConstraints(
        IslandData island,
        int start,
        int end,
        ContactVelocityConstraint[] velocityConstraints,
        Vector2[] linearVelocities,
        float[] angularVelocities)
    {
        var offset = island.Offset;

        // Here be dragons
        for (var i = start; i < end; ++i)
        {
            ref var velocityConstraint = ref velocityConstraints[i];

            var indexA = velocityConstraint.IndexA;
            var indexB = velocityConstraint.IndexB;
            var mA = velocityConstraint.InvMassA;
            var iA = velocityConstraint.InvIA;
            var mB = velocityConstraint.InvMassB;
            var iB = velocityConstraint.InvIB;
            var pointCount = velocityConstraint.PointCount;

            ref var vA = ref linearVelocities[offset + indexA];
            ref var wA = ref angularVelocities[offset + indexA];
            ref var vB = ref linearVelocities[offset + indexB];
            ref var wB = ref angularVelocities[offset + indexB];

            var normal = velocityConstraint.Normal;
            var tangent = Vector2Helpers.Cross(normal, 1.0f);
            var friction = velocityConstraint.Friction;

            DebugTools.Assert(pointCount is 1 or 2);
            var velPoints = velocityConstraint.Points.AsSpan;

            // Solve tangent constraints first because non-penetration is more important
            // than friction.
            for (var j = 0; j < pointCount; ++j)
            {
                ref var velConstraintPoint = ref velPoints[j];

                // Relative velocity at contact
                var dv = vB + Vector2Helpers.Cross(wB, velConstraintPoint.RelativeVelocityB) - vA - Vector2Helpers.Cross(wA, velConstraintPoint.RelativeVelocityA);

                // Compute tangent force
                float vt = Vector2.Dot(dv, tangent) - velocityConstraint.TangentSpeed;
                float lambda = velConstraintPoint.TangentMass * (-vt);

                // b2Clamp the accumulated force
                var maxFriction = friction * velConstraintPoint.NormalImpulse;
                var newImpulse = Math.Clamp(velConstraintPoint.TangentImpulse + lambda, -maxFriction, maxFriction);
                lambda = newImpulse - velConstraintPoint.TangentImpulse;
                velConstraintPoint.TangentImpulse = newImpulse;

                // Apply contact impulse
                Vector2 P = tangent * lambda;

                vA -= P * mA;
                wA -= iA * Vector2Helpers.Cross(velConstraintPoint.RelativeVelocityA, P);

                vB += P * mB;
                wB += iB * Vector2Helpers.Cross(velConstraintPoint.RelativeVelocityB, P);
            }

            // Solve normal constraints
            if (velocityConstraint.PointCount == 1)
            {
                ref var vcp = ref velocityConstraint.Points._00;

                // Relative velocity at contact
                Vector2 dv = vB + Vector2Helpers.Cross(wB, vcp.RelativeVelocityB) - vA - Vector2Helpers.Cross(wA, vcp.RelativeVelocityA);

                // Compute normal impulse
                float vn = Vector2.Dot(dv, normal);
                float lambda = -vcp.NormalMass * (vn - vcp.VelocityBias);

                // b2Clamp the accumulated impulse
                float newImpulse = Math.Max(vcp.NormalImpulse + lambda, 0.0f);
                lambda = newImpulse - vcp.NormalImpulse;
                vcp.NormalImpulse = newImpulse;

                // Apply contact impulse
                Vector2 P = normal * lambda;
                vA -= P * mA;
                wA -= iA * Vector2Helpers.Cross(vcp.RelativeVelocityA, P);

                vB += P * mB;
                wB += iB * Vector2Helpers.Cross(vcp.RelativeVelocityB, P);
            }
            else
            {
                // Block solver developed in collaboration with Dirk Gregorius (back in 01/07 on Box2D_Lite).
                // Build the mini LCP for this contact patch
                //
                // vn = A * x + b, vn >= 0, , vn >= 0, x >= 0 and vn_i * x_i = 0 with i = 1..2
                //
                // A = J * W * JT and J = ( -n, -r1 x n, n, r2 x n )
                // b = vn0 - velocityBias
                //
                // The system is solved using the "Total enumeration method" (s. Murty). The complementary constraint vn_i * x_i
                // implies that we must have in any solution either vn_i = 0 or x_i = 0. So for the 2D contact problem the cases
                // vn1 = 0 and vn2 = 0, x1 = 0 and x2 = 0, x1 = 0 and vn2 = 0, x2 = 0 and vn1 = 0 need to be tested. The first valid
                // solution that satisfies the problem is chosen.
                //
                // In order to account of the accumulated impulse 'a' (because of the iterative nature of the solver which only requires
                // that the accumulated impulse is clamped and not the incremental impulse) we change the impulse variable (x_i).
                //
                // Substitute:
                //
                // x = a + d
                //
                // a := old total impulse
                // x := new total impulse
                // d := incremental impulse
                //
                // For the current iteration we extend the formula for the incremental impulse
                // to compute the new total impulse:
                //
                // vn = A * d + b
                //    = A * (x - a) + b
                //    = A * x + b - A * a
                //    = A * x + b'
                // b' = b - A * a;

                ref var cp1 = ref velocityConstraint.Points._00;
                ref var cp2 = ref velocityConstraint.Points._01;

                Vector2 a = new Vector2(cp1.NormalImpulse, cp2.NormalImpulse);
                DebugTools.Assert(a.X >= 0.0f && a.Y >= 0.0f);

                // Relative velocity at contact
                Vector2 dv1 = vB + Vector2Helpers.Cross(wB, cp1.RelativeVelocityB) - vA - Vector2Helpers.Cross(wA, cp1.RelativeVelocityA);
                Vector2 dv2 = vB + Vector2Helpers.Cross(wB, cp2.RelativeVelocityB) - vA - Vector2Helpers.Cross(wA, cp2.RelativeVelocityA);

                // Compute normal velocity
                float vn1 = Vector2.Dot(dv1, normal);
                float vn2 = Vector2.Dot(dv2, normal);

                Vector2 b = new Vector2
                {
                    X = vn1 - cp1.VelocityBias,
                    Y = vn2 - cp2.VelocityBias
                };

                // Compute b'
                b -= Physics.Transform.Mul(velocityConstraint.K, a);

                //const float k_errorTol = 1e-3f;
                //B2_NOT_USED(k_errorTol);

                for (; ; )
                {
                    //
                    // Case 1: vn = 0
                    //
                    // 0 = A * x + b'
                    //
                    // Solve for x:
                    //
                    // x = - inv(A) * b'
                    //
                    Vector2 x = -Physics.Transform.Mul(velocityConstraint.NormalMass, b);

                    if (x.X >= 0.0f && x.Y >= 0.0f)
                    {
                        // Get the incremental impulse
                        Vector2 d = x - a;

                        // Apply incremental impulse
                        Vector2 P1 = normal * d.X;
                        Vector2 P2 = normal * d.Y;
                        vA -= (P1 + P2) * mA;
                        wA -= iA * (Vector2Helpers.Cross(cp1.RelativeVelocityA, P1) + Vector2Helpers.Cross(cp2.RelativeVelocityA, P2));

                        vB += (P1 + P2) * mB;
                        wB += iB * (Vector2Helpers.Cross(cp1.RelativeVelocityB, P1) + Vector2Helpers.Cross(cp2.RelativeVelocityB, P2));

                        // Accumulate
                        cp1.NormalImpulse = x.X;
                        cp2.NormalImpulse = x.Y;

                        break;
                    }

                    //
                    // Case 2: vn1 = 0 and x2 = 0
                    //
                    //   0 = a11 * x1 + a12 * 0 + b1'
                    // vn2 = a21 * x1 + a22 * 0 + b2'
                    //
                    x.X = -cp1.NormalMass * b.X;
                    x.Y = 0.0f;
                    vn1 = 0.0f;
                    vn2 = velocityConstraint.K.Y * x.X + b.Y;

                    if (x.X >= 0.0f && vn2 >= 0.0f)
                    {
                        // Get the incremental impulse
                        Vector2 d = x - a;

                        // Apply incremental impulse
                        Vector2 P1 = normal * d.X;
                        Vector2 P2 = normal * d.Y;
                        vA -= (P1 + P2) * mA;
                        wA -= iA * (Vector2Helpers.Cross(cp1.RelativeVelocityA, P1) + Vector2Helpers.Cross(cp2.RelativeVelocityA, P2));

                        vB += (P1 + P2) * mB;
                        wB += iB * (Vector2Helpers.Cross(cp1.RelativeVelocityB, P1) + Vector2Helpers.Cross(cp2.RelativeVelocityB, P2));

                        // Accumulate
                        cp1.NormalImpulse = x.X;
                        cp2.NormalImpulse = x.Y;

                        break;
                    }


                    //
                    // Case 3: vn2 = 0 and x1 = 0
                    //
                    // vn1 = a11 * 0 + a12 * x2 + b1'
                    //   0 = a21 * 0 + a22 * x2 + b2'
                    //
                    x.X = 0.0f;
                    x.Y = -cp2.NormalMass * b.Y;
                    vn1 = velocityConstraint.K.Z * x.Y + b.X;
                    vn2 = 0.0f;

                    if (x.Y >= 0.0f && vn1 >= 0.0f)
                    {
                        // Resubstitute for the incremental impulse
                        Vector2 d = x - a;

                        // Apply incremental impulse
                        Vector2 P1 = normal * d.X;
                        Vector2 P2 = normal * d.Y;
                        vA -= (P1 + P2) * mA;
                        wA -= iA * (Vector2Helpers.Cross(cp1.RelativeVelocityA, P1) + Vector2Helpers.Cross(cp2.RelativeVelocityA, P2));

                        vB += (P1 + P2) * mB;
                        wB += iB * (Vector2Helpers.Cross(cp1.RelativeVelocityB, P1) + Vector2Helpers.Cross(cp2.RelativeVelocityB, P2));

                        // Accumulate
                        cp1.NormalImpulse = x.X;
                        cp2.NormalImpulse = x.Y;

                        break;
                    }

                    //
                    // Case 4: x1 = 0 and x2 = 0
                    //
                    // vn1 = b1
                    // vn2 = b2;
                    x.X = 0.0f;
                    x.Y = 0.0f;
                    vn1 = b.X;
                    vn2 = b.Y;

                    if (vn1 >= 0.0f && vn2 >= 0.0f)
                    {
                        // Resubstitute for the incremental impulse
                        Vector2 d = x - a;

                        // Apply incremental impulse
                        Vector2 P1 = normal * d.X;
                        Vector2 P2 = normal * d.Y;
                        vA -= (P1 + P2) * mA;
                        wA -= iA * (Vector2Helpers.Cross(cp1.RelativeVelocityA, P1) + Vector2Helpers.Cross(cp2.RelativeVelocityA, P2));

                        vB += (P1 + P2) * mB;
                        wB += iB * (Vector2Helpers.Cross(cp1.RelativeVelocityB, P1) + Vector2Helpers.Cross(cp2.RelativeVelocityB, P2));

                        // Accumulate
                        cp1.NormalImpulse = x.X;
                        cp2.NormalImpulse = x.Y;

                        break;
                    }

                    // No solution, give up. This is hit sometimes, but it doesn't seem to matter.
                    break;
                }
            }
        }
    }

    private void StoreImpulses(in IslandData island, ContactVelocityConstraint[] velocityConstraints)
    {
        for (var i = 0; i < island.Contacts.Count; ++i)
        {
            var velocityConstraint = velocityConstraints[i];
            ref var manifold = ref island.Contacts[velocityConstraint.ContactIndex].Manifold;
            var manPoints = manifold.Points.AsSpan;
            var velPoints = velocityConstraint.Points.AsSpan;

            for (var j = 0; j < velocityConstraint.PointCount; ++j)
            {
                ref var point = ref manPoints[j];
                point.NormalImpulse = velPoints[j].NormalImpulse;
                point.TangentImpulse = velPoints[j].TangentImpulse;
            }
        }
    }

    private bool SolvePositionConstraints(
        SolverData data,
        in IslandData island,
        ParallelOptions? options,
        ContactPositionConstraint[] positionConstraints,
        Vector2[] positions,
        float[] angles)
    {
        var contactCount = island.Contacts.Count;

        // Parallel
        if (options != null && contactCount > PositionConstraintsPerThread * 2)
        {
            var unsolved = 0;
            var batches = (int) Math.Ceiling((float) contactCount / PositionConstraintsPerThread);

            Parallel.For(0, batches, options, i =>
            {
                var start = i * PositionConstraintsPerThread;
                var end = Math.Min(start + PositionConstraintsPerThread, contactCount);

                if (!SolvePositionConstraints(data, start, end, positionConstraints, positions, angles))
                    Interlocked.Increment(ref unsolved);
            });

            return unsolved == 0;
        }

        // No parallel
        return SolvePositionConstraints(data, 0, contactCount, positionConstraints, positions, angles);
    }

    /// <summary>
    ///     Tries to solve positions for all contacts specified.
    /// </summary>
    /// <returns>true if all positions solved</returns>
    private bool SolvePositionConstraints(
        SolverData data,
        int start,
        int end,
        ContactPositionConstraint[] positionConstraints,
        Vector2[] positions,
        float[] angles)
    {
        float minSeparation = 0.0f;

        for (int i = start; i < end; ++i)
        {
            var pc = positionConstraints[i];

            int indexA = pc.IndexA;
            int indexB = pc.IndexB;
            Vector2 localCenterA = pc.LocalCenterA;
            float mA = pc.InvMassA;
            float iA = pc.InvIA;
            Vector2 localCenterB = pc.LocalCenterB;
            float mB = pc.InvMassB;
            float iB = pc.InvIB;
            int pointCount = pc.PointCount;

            ref var centerA = ref positions[indexA];
            ref var angleA = ref angles[indexA];
            ref var centerB = ref positions[indexB];
            ref var angleB = ref angles[indexB];

            // Solve normal constraints
            for (int j = 0; j < pointCount; ++j)
            {
                Transform xfA = new Transform(angleA);
                Transform xfB = new Transform(angleB);
                xfA.Position = centerA - Physics.Transform.Mul(xfA.Quaternion2D, localCenterA);
                xfB.Position = centerB - Physics.Transform.Mul(xfB.Quaternion2D, localCenterB);

                Vector2 normal;
                Vector2 point;
                float separation;

                PositionSolverManifoldInitialize(pc, j, xfA, xfB, out normal, out point, out separation);

                Vector2 rA = point - centerA;
                Vector2 rB = point - centerB;

                // Track max constraint error.
                minSeparation = Math.Min(minSeparation, separation);

                // Prevent large corrections and allow slop.
                float C = Math.Clamp(data.Baumgarte * (separation + PhysicsConstants.LinearSlop), -_maxLinearCorrection, 0.0f);

                // Compute the effective mass.
                float rnA = Vector2Helpers.Cross(rA, normal);
                float rnB = Vector2Helpers.Cross(rB, normal);
                float K = mA + mB + iA * rnA * rnA + iB * rnB * rnB;

                // Compute normal impulse
                float impulse = K > 0.0f ? -C / K : 0.0f;

                Vector2 P = normal * impulse;

                centerA -= P * mA;
                angleA -= iA * Vector2Helpers.Cross(rA, P);

                centerB += P * mB;
                angleB += iB * Vector2Helpers.Cross(rB, P);
            }
        }

        // We can't expect minSpeparation >= -b2_linearSlop because we don't
        // push the separation above -b2_linearSlop.
        return minSeparation >= -3.0f * PhysicsConstants.LinearSlop;
    }

    /// <summary>
    /// Evaluate the manifold with supplied transforms. This assumes
    /// modest motion from the original state. This does not change the
    /// point count, impulses, etc. The radii must come from the Shapes
    /// that generated the manifold.
    /// </summary>
    internal static void InitializeManifold(
        ref Manifold manifold,
        in Transform xfA,
        in Transform xfB,
        float radiusA,
        float radiusB,
        out Vector2 normal,
        Span<Vector2> points)
    {
        normal = Vector2.Zero;

        if (manifold.PointCount == 0)
        {
            return;
        }

        switch (manifold.Type)
        {
            case ManifoldType.Circles:
            {
                normal = new Vector2(1.0f, 0.0f);
                Vector2 pointA = Physics.Transform.Mul(xfA, manifold.LocalPoint);
                Vector2 pointB = Physics.Transform.Mul(xfB, manifold.Points._00.LocalPoint);

                if ((pointA - pointB).LengthSquared() > float.Epsilon * float.Epsilon)
                {
                    normal = pointB - pointA;
                    normal = normal.Normalized();
                }

                Vector2 cA = pointA + normal * radiusA;
                Vector2 cB = pointB - normal * radiusB;
                points[0] = (cA + cB) * 0.5f;
            }
            break;

            case ManifoldType.FaceA:
            {
                normal = Physics.Transform.Mul(xfA.Quaternion2D, manifold.LocalNormal);
                Vector2 planePoint = Physics.Transform.Mul(xfA, manifold.LocalPoint);
                var manPoints = manifold.Points.AsSpan;

                for (int i = 0; i < manifold.PointCount; ++i)
                {
                    Vector2 clipPoint = Physics.Transform.Mul(xfB, manPoints[i].LocalPoint);
                    Vector2 cA = clipPoint + normal * (radiusA - Vector2.Dot(clipPoint - planePoint, normal));
                    Vector2 cB = clipPoint - normal * radiusB;
                    points[i] = (cA + cB) * 0.5f;
                }
            }
            break;

            case ManifoldType.FaceB:
            {
                normal = Physics.Transform.Mul(xfB.Quaternion2D, manifold.LocalNormal);
                Vector2 planePoint = Physics.Transform.Mul(xfB, manifold.LocalPoint);
                var manPoints = manifold.Points.AsSpan;

                for (int i = 0; i < manifold.PointCount; ++i)
                {
                    Vector2 clipPoint = Physics.Transform.Mul(xfA, manPoints[i].LocalPoint);
                    Vector2 cB = clipPoint + normal * (radiusB - Vector2.Dot(clipPoint - planePoint, normal));
                    Vector2 cA = clipPoint - normal * radiusA;
                    points[i] = (cA + cB) * 0.5f;
                }

                // Ensure normal points from A to B.
                normal = -normal;
            }
            break;
            default:
                // Shouldn't happentm
                throw new InvalidOperationException();

        }
    }

    private static void PositionSolverManifoldInitialize(
        in ContactPositionConstraint pc,
        int index,
        in Transform xfA,
        in Transform xfB,
        out Vector2 normal,
        out Vector2 point,
        out float separation)
    {
        DebugTools.Assert(pc.PointCount > 0);

            switch (pc.Type)
            {
                case ManifoldType.Circles:
                    {
                        Vector2 pointA = Physics.Transform.Mul(xfA, pc.LocalPoint);
                        Vector2 pointB = Physics.Transform.Mul(xfB, pc.LocalPoints._00);
                        normal = pointB - pointA;

                        //FPE: Fix to handle zero normalization
                        if (normal != Vector2.Zero)
                            normal = normal.Normalized();

                        point = (pointA + pointB) * 0.5f;
                        separation = Vector2.Dot(pointB - pointA, normal) - pc.RadiusA - pc.RadiusB;
                    }
                    break;

                case ManifoldType.FaceA:
                    {
                        var pcPoints = pc.LocalPoints.AsSpan;
                        normal = Physics.Transform.Mul(xfA.Quaternion2D, pc.LocalNormal);
                        Vector2 planePoint = Physics.Transform.Mul(xfA, pc.LocalPoint);

                        Vector2 clipPoint = Physics.Transform.Mul(xfB, pcPoints[index]);
                        separation = Vector2.Dot(clipPoint - planePoint, normal) - pc.RadiusA - pc.RadiusB;
                        point = clipPoint;
                    }
                    break;

                case ManifoldType.FaceB:
                    {
                        var pcPoints = pc.LocalPoints.AsSpan;
                        normal = Physics.Transform.Mul(xfB.Quaternion2D, pc.LocalNormal);
                        Vector2 planePoint = Physics.Transform.Mul(xfB, pc.LocalPoint);

                        Vector2 clipPoint = Physics.Transform.Mul(xfA, pcPoints[index]);
                        separation = Vector2.Dot(clipPoint - planePoint, normal) - pc.RadiusA - pc.RadiusB;
                        point = clipPoint;

                        // Ensure normal points from A to B
                        normal = -normal;
                    }
                    break;
                default:
                    normal = Vector2.Zero;
                    point = Vector2.Zero;
                    separation = 0;
                    break;

            }
    }
}
