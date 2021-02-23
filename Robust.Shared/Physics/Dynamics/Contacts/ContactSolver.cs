/*
Microsoft Permissive License (Ms-PL)

This license governs use of the accompanying software. If you use the software, you accept this license.
If you do not accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under
U.S. copyright law.
A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3,
each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution,
prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3,
each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to
make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or
derivative works of the contribution in the software.

3. Conditions and Limitations
(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software,
your patent license from such contributor to the software ends automatically.
(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark,
and attribution notices that are present in the software.
(D) If you distribute any portion of the software in source code form, you may do so only under this license by
including a complete copy of this license with your distribution.
If you distribute any portion of the software in compiled or object code form, you may only do so under a license that
complies with this license.
(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions.
You may have additional consumer rights under your local laws which this license cannot change.
To the extent permitted under your local laws, the contributors exclude the implied warranties of
merchantability, fitness for a particular purpose and non-infringement.
*/

using System;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Dynamics.Contacts
{
    internal sealed class ContactSolver
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;

        private bool _warmStarting;
        private float _velocityThreshold;
        private float _baumgarte;
        private float _linearSlop;
        private float _maxLinearCorrection;

        private Vector2[] _linearVelocities = Array.Empty<Vector2>();
        private float[] _angularVelocities = Array.Empty<float>();

        private Vector2[] _positions = Array.Empty<Vector2>();
        private float[] _angles = Array.Empty<float>();

        private Contact[] _contacts = Array.Empty<Contact>();
        private int _contactCount;

        private ContactVelocityConstraint[] _velocityConstraints = Array.Empty<ContactVelocityConstraint>();
        private ContactPositionConstraint[] _positionConstraints = Array.Empty<ContactPositionConstraint>();

        public void Initialize()
        {
            IoCManager.InjectDependencies(this);

            _warmStarting = _configManager.GetCVar(CVars.WarmStarting);
            _configManager.OnValueChanged(CVars.WarmStarting, value => _warmStarting = value);

            _velocityThreshold = _configManager.GetCVar(CVars.VelocityThreshold);
            _configManager.OnValueChanged(CVars.VelocityThreshold, value => _velocityThreshold = value);

            _baumgarte = _configManager.GetCVar(CVars.Baumgarte);
            _configManager.OnValueChanged(CVars.Baumgarte, value => _baumgarte = value);

            _linearSlop = _configManager.GetCVar(CVars.LinearSlop);
            _configManager.OnValueChanged(CVars.LinearSlop, value => _linearSlop = value);

            _maxLinearCorrection = _configManager.GetCVar(CVars.MaxLinearCorrection);
            _configManager.OnValueChanged(CVars.MaxLinearCorrection, value => _maxLinearCorrection = value);
        }

        public void Reset(SolverData data, int contactCount, Contact[] contacts)
        {
            _linearVelocities = data.LinearVelocities;
            _angularVelocities = data.AngularVelocities;

            _positions = data.Positions;
            _positions = data.Positions;
            _angles = data.Angles;

            _contactCount = contactCount;
            _contacts = contacts;

            // If we need more constraints then grow the cached arrays
            if (_velocityConstraints.Length < contactCount)
            {
                var oldLength = _velocityConstraints.Length;

                Array.Resize(ref _velocityConstraints, contactCount * 2);
                Array.Resize(ref _positionConstraints, contactCount * 2);

                for (var i = oldLength; i < _velocityConstraints.Length; i++)
                {
                    _velocityConstraints[i] = new ContactVelocityConstraint();
                    _positionConstraints[i] = new ContactPositionConstraint();
                }
            }

            // Build constraints
            // For now these are going to be bare but will change
            for (var i = 0; i < _contactCount; i++)
            {
                var contact = contacts[i];
                Fixture fixtureA = contact.FixtureA!;
                Fixture fixtureB = contact.FixtureB!;
                var shapeA = fixtureA.Shape;
                var shapeB = fixtureB.Shape;
                float radiusA = shapeA.Radius;
                float radiusB = shapeB.Radius;
                var bodyA = fixtureA.Body;
                var bodyB = fixtureB.Body;
                var manifold = contact.Manifold;

                int pointCount = manifold.PointCount;
                DebugTools.Assert(pointCount > 0);

                var velocityConstraint = _velocityConstraints[i];
                velocityConstraint.Friction = contact.Friction;
                velocityConstraint.Restitution = contact.Restitution;
                velocityConstraint.TangentSpeed = contact.TangentSpeed;
                velocityConstraint.IndexA = bodyA.IslandIndex;
                velocityConstraint.IndexB = bodyB.IslandIndex;
                velocityConstraint.InvMassA = bodyA.InvMass;
                velocityConstraint.InvMassB = bodyB.InvMass;
                velocityConstraint.InvIA = bodyA.InvI;
                velocityConstraint.InvIB = bodyB.InvI;
                velocityConstraint.ContactIndex = i;
                velocityConstraint.PointCount = pointCount;

                for (var x = 0; x < 2; x++)
                {
                    velocityConstraint.K[x] = Vector2.Zero;
                    velocityConstraint.NormalMass[x] = Vector2.Zero;
                }

                var positionConstraint = _positionConstraints[i];
                positionConstraint.IndexA = bodyA.IslandIndex;
                positionConstraint.IndexB = bodyB.IslandIndex;
                positionConstraint.InvMassA = bodyA.InvMass;
                positionConstraint.InvMassB = bodyB.InvMass;
                // TODO: Dis
                // positionConstraint.LocalCenterA = bodyA._sweep.LocalCenter;
                // positionConstraint.LocalCenterB = bodyB._sweep.LocalCenter;
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

                for (int j = 0; j < pointCount; ++j)
                {
                    var contactPoint = manifold.Points[j];
                    var constraintPoint = velocityConstraint.Points[j];

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

                    positionConstraint.LocalPoints[j] = contactPoint.LocalPoint;
                }
            }
        }

        public void InitializeVelocityConstraints()
        {
            for (var i = 0; i < _contactCount; ++i)
            {
                var velocityConstraint = _velocityConstraints[i];
                var positionConstraint = _positionConstraints[i];

                var radiusA = positionConstraint.RadiusA;
                var radiusB = positionConstraint.RadiusB;
                var manifold = _contacts[velocityConstraint.ContactIndex].Manifold;

                var indexA = velocityConstraint.IndexA;
                var indexB = velocityConstraint.IndexB;

                var invMassA = velocityConstraint.InvMassA;
                var invMassB = velocityConstraint.InvMassB;
                var invIA = velocityConstraint.InvIA;
                var invIB = velocityConstraint.InvIB;
                var localCenterA = positionConstraint.LocalCenterA;
                var localCenterB = positionConstraint.LocalCenterB;

                var centerA = _positions[indexA];
                var angleA = _angles[indexA];
                var linVelocityA = _linearVelocities[indexA];
                var angVelocityA = _angularVelocities[indexA];

                var centerB = _positions[indexB];
                var angleB = _angles[indexB];
                var linVelocityB = _linearVelocities[indexB];
                var angVelocityB = _angularVelocities[indexB];

                DebugTools.Assert(manifold.PointCount > 0);

                Transform xfA = new Transform(angleA);
                Transform xfB = new Transform(angleB);
                xfA.Position = centerA - Transform.Mul(xfA.Quaternion, localCenterA);
                xfB.Position = centerB - Transform.Mul(xfB.Quaternion, localCenterB);

                Vector2 normal;
                var points = new Vector2[2];
                InitializeManifold(manifold, xfA, xfB, radiusA, radiusB, out normal, out points);

                velocityConstraint.Normal = normal;

                int pointCount = velocityConstraint.PointCount;

                for (int j = 0; j < pointCount; ++j)
                {
                    VelocityConstraintPoint vcp = velocityConstraint.Points[j];

                    vcp.RelativeVelocityA = points[j] - centerA;
                    vcp.RelativeVelocityB = points[j] - centerB;

                    float rnA = Vector2.Cross(vcp.RelativeVelocityA, velocityConstraint.Normal);
                    float rnB = Vector2.Cross(vcp.RelativeVelocityB, velocityConstraint.Normal);

                    float kNormal = invMassA + invMassB + invIA * rnA * rnA + invIB * rnB * rnB;

                    vcp.NormalMass = kNormal > 0.0f ? 1.0f / kNormal : 0.0f;

                    Vector2 tangent = Vector2.Cross(velocityConstraint.Normal, 1.0f);

                    float rtA = Vector2.Cross(vcp.RelativeVelocityA, tangent);
                    float rtB = Vector2.Cross(vcp.RelativeVelocityB, tangent);

                    float kTangent = invMassA + invMassB + invIA * rtA * rtA + invIB * rtB * rtB;

                    vcp.TangentMass = kTangent > 0.0f ? 1.0f / kTangent : 0.0f;

                    // Setup a velocity bias for restitution.
                    vcp.VelocityBias = 0.0f;
                    float vRel = Vector2.Dot(velocityConstraint.Normal, linVelocityB + Vector2.Cross(angVelocityB, vcp.RelativeVelocityB) - linVelocityA - Vector2.Cross(angVelocityA, vcp.RelativeVelocityA));
                    if (vRel < -_velocityThreshold)
                    {
                        vcp.VelocityBias = -velocityConstraint.Restitution * vRel;
                    }
                }

                // If we have two points, then prepare the block solver.
                if (velocityConstraint.PointCount == 2)
                {
                    var vcp1 = velocityConstraint.Points[0];
                    var vcp2 = velocityConstraint.Points[1];

                    var rn1A = Vector2.Cross(vcp1.RelativeVelocityA, velocityConstraint.Normal);
                    var rn1B = Vector2.Cross(vcp1.RelativeVelocityB, velocityConstraint.Normal);
                    var rn2A = Vector2.Cross(vcp2.RelativeVelocityA, velocityConstraint.Normal);
                    var rn2B = Vector2.Cross(vcp2.RelativeVelocityB, velocityConstraint.Normal);

                    var k11 = invMassA + invMassB + invIA * rn1A * rn1A + invIB * rn1B * rn1B;
                    var k22 = invMassA + invMassB + invIA * rn2A * rn2A + invIB * rn2B * rn2B;
                    var k12 = invMassA + invMassB + invIA * rn1A * rn2A + invIB * rn1B * rn2B;

                    // Ensure a reasonable condition number.
                    const float k_maxConditionNumber = 1000.0f;
                    if (k11 * k11 < k_maxConditionNumber * (k11 * k22 - k12 * k12))
                    {
                        // K is safe to invert.
                        velocityConstraint.K[0] = new Vector2(k11, k12);
                        velocityConstraint.K[1] = new Vector2(k12, k22);
                        velocityConstraint.NormalMass = velocityConstraint.K.Inverse();
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

        public void WarmStart()
        {
            for (var i = 0; i < _contactCount; ++i)
            {
                var velocityConstraint = _velocityConstraints[i];

                var indexA = velocityConstraint.IndexA;
                var indexB = velocityConstraint.IndexB;
                var invMassA = velocityConstraint.InvMassA;
                var invIA = velocityConstraint.InvIA;
                var invMassB = velocityConstraint.InvMassB;
                var invIB = velocityConstraint.InvIB;
                var pointCount = velocityConstraint.PointCount;

                var linVelocityA = _linearVelocities[indexA];
                var angVelocityA = _angularVelocities[indexA];
                var linVelocityB = _linearVelocities[indexB];
                var angVelocityB = _angularVelocities[indexB];

                var normal = velocityConstraint.Normal;
                var tangent = Vector2.Cross(normal, 1.0f);

                for (var j = 0; j < pointCount; ++j)
                {
                    var constraintPoint = velocityConstraint.Points[j];
                    var P = normal * constraintPoint.NormalImpulse + tangent * constraintPoint.TangentImpulse;
                    angVelocityA -= invIA * Vector2.Cross(constraintPoint.RelativeVelocityA, P);
                    linVelocityA -= P * invMassA;
                    angVelocityB += invIB * Vector2.Cross(constraintPoint.RelativeVelocityB, P);
                    linVelocityB += P * invMassB;
                }

                _linearVelocities[indexA] = linVelocityA;
                _angularVelocities[indexA] = angVelocityA;
                _linearVelocities[indexB] = linVelocityB;
                _angularVelocities[indexB] = angVelocityB;
            }
        }

        public void SolveVelocityConstraints()
        {
            // Here be dragons
            for (var i = 0; i < _contactCount; ++i)
            {
                var velocityConstraint = _velocityConstraints[i];

                var indexA = velocityConstraint.IndexA;
                var indexB = velocityConstraint.IndexB;
                var mA = velocityConstraint.InvMassA;
                var iA = velocityConstraint.InvIA;
                var mB = velocityConstraint.InvMassB;
                var iB = velocityConstraint.InvIB;
                var pointCount = velocityConstraint.PointCount;

                var vA = _linearVelocities[indexA];
                var wA = _angularVelocities[indexA];
                var vB = _linearVelocities[indexB];
                var wB = _angularVelocities[indexB];

                var normal = velocityConstraint.Normal;
                var tangent = Vector2.Cross(normal, 1.0f);
                var friction = velocityConstraint.Friction;

                DebugTools.Assert(pointCount == 1 || pointCount == 2);

                // Solve tangent constraints first because non-penetration is more important
                // than friction.
                for (var j = 0; j < pointCount; ++j)
                {
                    VelocityConstraintPoint velConstraintPoint = velocityConstraint.Points[j];

                    // Relative velocity at contact
                    var dv = vB + Vector2.Cross(wB, velConstraintPoint.RelativeVelocityB) - vA - Vector2.Cross(wA, velConstraintPoint.RelativeVelocityA);

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
                    wA -= iA * Vector2.Cross(velConstraintPoint.RelativeVelocityA, P);

                    vB += P * mB;
                    wB += iB * Vector2.Cross(velConstraintPoint.RelativeVelocityB, P);
                }

                // Solve normal constraints
                if (velocityConstraint.PointCount == 1)
                {
                    VelocityConstraintPoint vcp = velocityConstraint.Points[0];

                    // Relative velocity at contact
                    Vector2 dv = vB + Vector2.Cross(wB, vcp.RelativeVelocityB) - vA - Vector2.Cross(wA, vcp.RelativeVelocityA);

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
                    wA -= iA * Vector2.Cross(vcp.RelativeVelocityA, P);

                    vB += P * mB;
                    wB += iB * Vector2.Cross(vcp.RelativeVelocityB, P);
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

                    VelocityConstraintPoint cp1 = velocityConstraint.Points[0];
                    VelocityConstraintPoint cp2 = velocityConstraint.Points[1];

                    Vector2 a = new Vector2(cp1.NormalImpulse, cp2.NormalImpulse);
                    DebugTools.Assert(a.X >= 0.0f && a.Y >= 0.0f);

                    // Relative velocity at contact
                    Vector2 dv1 = vB + Vector2.Cross(wB, cp1.RelativeVelocityB) - vA - Vector2.Cross(wA, cp1.RelativeVelocityA);
                    Vector2 dv2 = vB + Vector2.Cross(wB, cp2.RelativeVelocityB) - vA - Vector2.Cross(wA, cp2.RelativeVelocityA);

                    // Compute normal velocity
                    float vn1 = Vector2.Dot(dv1, normal);
                    float vn2 = Vector2.Dot(dv2, normal);

                    Vector2 b = new Vector2
                    {
                        X = vn1 - cp1.VelocityBias,
                        Y = vn2 - cp2.VelocityBias
                    };

                    // Compute b'
                    b -= Transform.Mul(velocityConstraint.K, a);

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
                        Vector2 x = -Transform.Mul(velocityConstraint.NormalMass, b);

                        if (x.X >= 0.0f && x.Y >= 0.0f)
                        {
                            // Get the incremental impulse
                            Vector2 d = x - a;

                            // Apply incremental impulse
                            Vector2 P1 = normal * d.X;
                            Vector2 P2 = normal * d.Y;
                            vA -= (P1 + P2) * mA;
                            wA -= iA * (Vector2.Cross(cp1.RelativeVelocityA, P1) + Vector2.Cross(cp2.RelativeVelocityA, P2));

                            vB += (P1 + P2) * mB;
                            wB += iB * (Vector2.Cross(cp1.RelativeVelocityB, P1) + Vector2.Cross(cp2.RelativeVelocityB, P2));

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
                        vn2 = velocityConstraint.K[0].Y * x.X + b.Y;

                        if (x.X >= 0.0f && vn2 >= 0.0f)
                        {
                            // Get the incremental impulse
                            Vector2 d = x - a;

                            // Apply incremental impulse
                            Vector2 P1 = normal * d.X;
                            Vector2 P2 = normal * d.Y;
                            vA -= (P1 + P2) * mA;
                            wA -= iA * (Vector2.Cross(cp1.RelativeVelocityA, P1) + Vector2.Cross(cp2.RelativeVelocityA, P2));

                            vB += (P1 + P2) * mB;
                            wB += iB * (Vector2.Cross(cp1.RelativeVelocityB, P1) + Vector2.Cross(cp2.RelativeVelocityB, P2));

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
                        vn1 = velocityConstraint.K[1].X * x.Y + b.X;
                        vn2 = 0.0f;

                        if (x.Y >= 0.0f && vn1 >= 0.0f)
                        {
                            // Resubstitute for the incremental impulse
                            Vector2 d = x - a;

                            // Apply incremental impulse
                            Vector2 P1 = normal * d.X;
                            Vector2 P2 = normal * d.Y;
                            vA -= (P1 + P2) * mA;
                            wA -= iA * (Vector2.Cross(cp1.RelativeVelocityA, P1) + Vector2.Cross(cp2.RelativeVelocityA, P2));

                            vB += (P1 + P2) * mB;
                            wB += iB * (Vector2.Cross(cp1.RelativeVelocityB, P1) + Vector2.Cross(cp2.RelativeVelocityB, P2));

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
                            wA -= iA * (Vector2.Cross(cp1.RelativeVelocityA, P1) + Vector2.Cross(cp2.RelativeVelocityA, P2));

                            vB += (P1 + P2) * mB;
                            wB += iB * (Vector2.Cross(cp1.RelativeVelocityB, P1) + Vector2.Cross(cp2.RelativeVelocityB, P2));

                            // Accumulate
                            cp1.NormalImpulse = x.X;
                            cp2.NormalImpulse = x.Y;

                            break;
                        }

                        // No solution, give up. This is hit sometimes, but it doesn't seem to matter.
                        break;
                    }
                }

                _linearVelocities[indexA] = vA;
                _angularVelocities[indexA] = wA;
                _linearVelocities[indexB] = vB;
                _angularVelocities[indexB] = wB;
            }
        }

        public void StoreImpulses()
        {
            for (int i = 0; i < _contactCount; ++i)
            {
                ContactVelocityConstraint velocityConstraint = _velocityConstraints[i];
                Collision.Manifold manifold = _contacts[velocityConstraint.ContactIndex].Manifold;

                for (int j = 0; j < velocityConstraint.PointCount; ++j)
                {
                    ManifoldPoint point = manifold.Points[j];
                    point.NormalImpulse = velocityConstraint.Points[j].NormalImpulse;
                    point.TangentImpulse = velocityConstraint.Points[j].TangentImpulse;
                    manifold.Points[j] = point;
                }

                _contacts[velocityConstraint.ContactIndex].Manifold = manifold;
            }
        }

        /// <summary>
        ///     Tries to solve positions for all contacts specified.
        /// </summary>
        /// <returns>true if all positions solved</returns>
        public bool SolvePositionConstraints()
        {
            float minSeparation = 0.0f;

            for (int i = 0; i < _contactCount; ++i)
            {
                ContactPositionConstraint pc = _positionConstraints[i];

                int indexA = pc.IndexA;
                int indexB = pc.IndexB;
                Vector2 localCenterA = pc.LocalCenterA;
                float mA = pc.InvMassA;
                float iA = pc.InvIA;
                Vector2 localCenterB = pc.LocalCenterB;
                float mB = pc.InvMassB;
                float iB = pc.InvIB;
                int pointCount = pc.PointCount;

                Vector2 centerA = _positions[indexA];
                float angleA = _angles[indexA];

                Vector2 centerB = _positions[indexB];
                float angleB = _angles[indexB];

                // Solve normal constraints
                for (int j = 0; j < pointCount; ++j)
                {
                    Transform xfA = new Transform(angleA);
                    Transform xfB = new Transform(angleB);
                    xfA.Position = centerA - Transform.Mul(xfA.Quaternion, localCenterA);
                    xfB.Position = centerB - Transform.Mul(xfB.Quaternion, localCenterB);

                    Vector2 normal;
                    Vector2 point;
                    float separation;

                    PositionSolverManifoldInitialize(pc, j, xfA, xfB, out normal, out point, out separation);

                    Vector2 rA = point - centerA;
                    Vector2 rB = point - centerB;

                    // Track max constraint error.
                    minSeparation = Math.Min(minSeparation, separation);

                    // Prevent large corrections and allow slop.
                    float C = Math.Clamp(_baumgarte * (separation + _linearSlop), -_maxLinearCorrection, 0.0f);

                    // Compute the effective mass.
                    float rnA = Vector2.Cross(rA, normal);
                    float rnB = Vector2.Cross(rB, normal);
                    float K = mA + mB + iA * rnA * rnA + iB * rnB * rnB;

                    // Compute normal impulse
                    float impulse = K > 0.0f ? -C / K : 0.0f;

                    Vector2 P = normal * impulse;

                    centerA -= P * mA;
                    angleA -= iA * Vector2.Cross(rA, P);

                    centerB += P * mB;
                    angleB += iB * Vector2.Cross(rB, P);
                }

                _positions[indexA] = centerA;
                _angles[indexA] = angleA;

                _positions[indexB] = centerB;
                _angles[indexB] = angleB;
            }

            // We can't expect minSpeparation >= -b2_linearSlop because we don't
            // push the separation above -b2_linearSlop.
            return minSeparation >= -3.0f * _linearSlop;
        }

        /// <summary>
        /// Evaluate the manifold with supplied transforms. This assumes
        /// modest motion from the original state. This does not change the
        /// point count, impulses, etc. The radii must come from the Shapes
        /// that generated the manifold.
        /// </summary>
        internal static void InitializeManifold(
            in Collision.Manifold manifold,
            in Transform xfA,
            in Transform xfB,
            float radiusA,
            float radiusB,
            out Vector2 normal,
            out Vector2[] points)
        {
            normal = Vector2.Zero;
            points = new Vector2[2];

            if (manifold.PointCount == 0)
            {
                return;
            }

            switch (manifold.Type)
            {
                case ManifoldType.Circles:
                {
                    normal = new Vector2(1.0f, 0.0f);
                    Vector2 pointA = Transform.Mul(xfA, manifold.LocalPoint);
                    Vector2 pointB = Transform.Mul(xfB, manifold.Points[0].LocalPoint);

                    if ((pointA - pointB).LengthSquared > float.Epsilon * float.Epsilon)
                    {
                        normal = pointB - pointA;
                        normal = normal.Normalized;
                    }

                    Vector2 cA = pointA + normal * radiusA;
                    Vector2 cB = pointB - normal * radiusB;
                    points[0] = (cA + cB) * 0.5f;
                }
                break;

                case ManifoldType.FaceA:
                {
                    normal = Transform.Mul(xfA.Quaternion, manifold.LocalNormal);
                    Vector2 planePoint = Transform.Mul(xfA, manifold.LocalPoint);

                    for (int i = 0; i < manifold.PointCount; ++i)
                    {
                        Vector2 clipPoint = Transform.Mul(xfB, manifold.Points[i].LocalPoint);
                        Vector2 cA = clipPoint + normal * (radiusA - Vector2.Dot(clipPoint - planePoint, normal));
                        Vector2 cB = clipPoint - normal * radiusB;
                        points[i] = (cA + cB) * 0.5f;
                    }
                }
                break;

                case ManifoldType.FaceB:
                {
                    normal = Transform.Mul(xfB.Quaternion, manifold.LocalNormal);
                    Vector2 planePoint = Transform.Mul(xfB, manifold.LocalPoint);

                    for (int i = 0; i < manifold.PointCount; ++i)
                    {
                        Vector2 clipPoint = Transform.Mul(xfA, manifold.Points[i].LocalPoint);
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
                            Vector2 pointA = Transform.Mul(xfA, pc.LocalPoint);
                            Vector2 pointB = Transform.Mul(xfB, pc.LocalPoints[0]);
                            normal = pointB - pointA;

                            //FPE: Fix to handle zero normalization
                            if (normal != Vector2.Zero)
                                normal = normal.Normalized;

                            point = (pointA + pointB) * 0.5f;
                            separation = Vector2.Dot(pointB - pointA, normal) - pc.RadiusA - pc.RadiusB;
                        }
                        break;

                    case ManifoldType.FaceA:
                        {
                            normal = Transform.Mul(xfA.Quaternion, pc.LocalNormal);
                            Vector2 planePoint = Transform.Mul(xfA, pc.LocalPoint);

                            Vector2 clipPoint = Transform.Mul(xfB, pc.LocalPoints[index]);
                            separation = Vector2.Dot(clipPoint - planePoint, normal) - pc.RadiusA - pc.RadiusB;
                            point = clipPoint;
                        }
                        break;

                    case ManifoldType.FaceB:
                        {
                            normal = Transform.Mul(xfB.Quaternion, pc.LocalNormal);
                            Vector2 planePoint = Transform.Mul(xfB, pc.LocalPoint);

                            Vector2 clipPoint = Transform.Mul(xfA, pc.LocalPoints[index]);
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
}
