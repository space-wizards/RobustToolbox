using System;
using System.Diagnostics;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Shapes;

namespace Robust.Shared.Physics.Solver
{
    public sealed class ContactPositionConstraint
    {
        public Vector2[] LocalPoints = new Vector2[PhysicsSettings.MaxManifoldPoints];
        public Vector2 LocalNormal;
        public Vector2 LocalPoint;
        public int IndexA;
        public int IndexB;
        public float InvMassA, InvMassB;
        public Vector2 LocalCenterA, LocalCenterB;
        public float InvIA, InvIB;
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
        public VelocityConstraintPoint[] points = new VelocityConstraintPoint[PhysicsSettings.MaxManifoldPoints];
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
            for (int i = 0; i < PhysicsSettings.MaxManifoldPoints; i++)
            {
                points[i] = new VelocityConstraintPoint();
            }
        }
    }

    public sealed class ContactSolver
    {
        // CHUNKY
        internal SolverPosition[] _positions = {};
        internal SolverVelocity[] _velocities = {};
        public ContactPositionConstraint[] _positionConstraints = {};
        public ContactVelocityConstraint[] _velocityConstraints = {};
        public Contact[] _contacts = {};
        public int _count;
        int _velocityConstraintsMultithreadThreshold;
        int _positionConstraintsMultithreadThreshold;

        internal void Reset(ref PhysicsStep step, int count, Contact[] contacts, SolverPosition[] positions, SolverVelocity[] velocities)
        {
            _count = count;
            _positions = positions;
            _velocities = velocities;
            _contacts = contacts;
            _velocityConstraintsMultithreadThreshold = 0; // velocityConstraintsMultithreadThreshold;
            _positionConstraintsMultithreadThreshold = 0; // positionConstraintsMultithreadThreshold;

            // grow the array
            if (_velocityConstraints.Length < count)
            {
                int newBufferCount = Math.Max(count, 32);
                newBufferCount = newBufferCount + (newBufferCount * 2 >> 4); // grow by x1.125f
                newBufferCount = (newBufferCount + 31) & (~31); // grow in chunks of 32.
                int oldBufferCount = _velocityConstraints.Length;
                Array.Resize(ref _velocityConstraints, newBufferCount);
                Array.Resize(ref _positionConstraints, newBufferCount);

                for (int i = oldBufferCount; i < newBufferCount; i++)
                {
                    _velocityConstraints[i] = new ContactVelocityConstraint();
                    _positionConstraints[i] = new ContactPositionConstraint();
                }
            }

            // Initialize position independent portions of the constraints.
            for (int i = 0; i < _count; ++i)
            {
                Contact contact = contacts[i];

                Debug.Assert(contact.FixtureA != null && contact.FixtureB != null);

                Fixture fixtureA = contact.FixtureA;
                Fixture fixtureB = contact.FixtureB;
                Shape shapeA = fixtureA.Shape;
                Shape shapeB = fixtureB.Shape;
                float radiusA = shapeA.Radius;
                float radiusB = shapeB.Radius;
                PhysicsComponent bodyA = fixtureA.Body;
                PhysicsComponent bodyB = fixtureB.Body;
                Manifold manifold = contact.Manifold;

                int pointCount = manifold.PointCount;
                Debug.Assert(pointCount > 0);

                ContactVelocityConstraint vc = _velocityConstraints[i];
                vc.friction = contact.Friction;
                vc.restitution = contact.Restitution;
                vc.tangentSpeed = contact.TangentSpeed;
                vc.indexA = bodyA.IslandIndex;
                vc.indexB = bodyB.IslandIndex;
                vc.invMassA = bodyA.InvMass;
                vc.invMassB = bodyB.InvMass;
                vc.invIA = bodyA.InvI;
                vc.invIB = bodyB.InvI;
                vc.contactIndex = i;
                vc.pointCount = pointCount;
                vc.K.SetZero();
                vc.normalMass.SetZero();

                ContactPositionConstraint pc = _positionConstraints[i];
                pc.IndexA = bodyA.IslandIndex;
                pc.IndexB = bodyB.IslandIndex;
                pc.InvMassA = bodyA.InvMass;
                pc.InvMassB = bodyB.InvMass;
                pc.LocalCenterA = bodyA.Sweep.LocalCenter;
                pc.LocalCenterB = bodyB.Sweep.LocalCenter;
                pc.InvIA = bodyA.InvI;
                pc.InvIB = bodyB.InvI;
                pc.LocalNormal = manifold.LocalNormal;
                pc.LocalPoint = manifold.LocalPoint;
                pc.PointCount = pointCount;
                pc.RadiusA = radiusA;
                pc.RadiusB = radiusB;
                pc.Type = manifold.Type;

                for (int j = 0; j < pointCount; ++j)
                {
                    ManifoldPoint cp = manifold.Points[j];
                    VelocityConstraintPoint vcp = vc.points[j];

                    if (step.WarmStarting)
                    {
                        vcp.NormalImpulse = step.DtRatio * cp.NormalImpulse;
                        vcp.TangentImpulse = step.DtRatio * cp.TangentImpulse;
                    }
                    else
                    {
                        vcp.NormalImpulse = 0.0f;
                        vcp.TangentImpulse = 0.0f;
                    }

                    vcp.RelativeVelocityA = Vector2.Zero;
                    vcp.RelativeVelocityB = Vector2.Zero;
                    vcp.NormalMass = 0.0f;
                    vcp.TangentMass = 0.0f;
                    vcp.VelocityBias = 0.0f;

                    pc.LocalPoints[j] = cp.LocalPoint;
                }
            }
        }

        public void InitializeVelocityConstraints()
        {
            for (int i = 0; i < _count; ++i)
            {
                ContactVelocityConstraint vc = _velocityConstraints[i];
                ContactPositionConstraint pc = _positionConstraints[i];

                float radiusA = pc.RadiusA;
                float radiusB = pc.RadiusB;
                Manifold manifold = _contacts[vc.contactIndex].Manifold;

                int indexA = vc.indexA;
                int indexB = vc.indexB;

                float mA = vc.invMassA;
                float mB = vc.invMassB;
                float iA = vc.invIA;
                float iB = vc.invIB;
                Vector2 localCenterA = pc.LocalCenterA;
                Vector2 localCenterB = pc.LocalCenterB;

                Vector2 cA = _positions[indexA].Center;
                float aA = _positions[indexA].Angle;
                Vector2 vA = _velocities[indexA].LinearVelocity;
                float wA = _velocities[indexA].AngularVelocity;

                Vector2 cB = _positions[indexB].Center;
                float aB = _positions[indexB].Angle;
                Vector2 vB = _velocities[indexB].LinearVelocity;
                float wB = _velocities[indexB].AngularVelocity;

                Debug.Assert(manifold.PointCount > 0);

                PhysicsTransform xfA = new PhysicsTransform(Vector2.Zero, aA);
                PhysicsTransform xfB = new PhysicsTransform(Vector2.Zero, aB);
                xfA.Position = cA - Complex.Multiply(localCenterA, ref xfA.Quaternion);
                xfB.Position = cB - Complex.Multiply(localCenterB, ref xfB.Quaternion);

                Vector2 normal;
                Vector2[] points;
                WorldManifold.Initialize(manifold, ref xfA, radiusA, ref xfB, radiusB, out normal, out points);

                vc.normal = normal;
                Vector2 tangent = Vector2.Rot270(vc.normal);

                int pointCount = vc.pointCount;
                for (int j = 0; j < pointCount; ++j)
                {
                    VelocityConstraintPoint vcp = vc.points[j];

                    vcp.RelativeVelocityA = points[j] - cA;
                    vcp.RelativeVelocityB = points[j] - cB;

                    float rnA = Vector2.Cross(vcp.RelativeVelocityA, vc.normal);
                    float rnB = Vector2.Cross(vcp.RelativeVelocityB, vc.normal);

                    float kNormal = mA + mB + iA * rnA * rnA + iB * rnB * rnB;

                    vcp.NormalMass = kNormal > 0.0f ? 1.0f / kNormal : 0.0f;


                    float rtA = Vector2.Cross(vcp.RelativeVelocityA, tangent);
                    float rtB = Vector2.Cross(vcp.RelativeVelocityB, tangent);

                    float kTangent = mA + mB + iA * rtA * rtA + iB * rtB * rtB;

                    vcp.TangentMass = kTangent > 0.0f ? 1.0f / kTangent : 0.0f;

                    // Setup a velocity bias for restitution.
                    vcp.VelocityBias = 0.0f;
                    float vRel = Vector2.Dot(vc.normal, vB + Vector2.Cross(wB, vcp.RelativeVelocityB) - vA - Vector2.Cross(wA, vcp.RelativeVelocityA));
                    if (vRel < -PhysicsSettings.VelocityThreshold)
                    {
                        vcp.VelocityBias = -vc.restitution * vRel;
                    }
                }

                // If we have two points, then prepare the block solver.
                if (vc.pointCount == 2)
                {
                    VelocityConstraintPoint vcp1 = vc.points[0];
                    VelocityConstraintPoint vcp2 = vc.points[1];

                    float rn1A = Vector2.Cross(vcp1.RelativeVelocityA, vc.normal);
                    float rn1B = Vector2.Cross(vcp1.RelativeVelocityB, vc.normal);
                    float rn2A = Vector2.Cross(vcp2.RelativeVelocityA, vc.normal);
                    float rn2B = Vector2.Cross(vcp2.RelativeVelocityB, vc.normal);

                    float k11 = mA + mB + iA * rn1A * rn1A + iB * rn1B * rn1B;
                    float k22 = mA + mB + iA * rn2A * rn2A + iB * rn2B * rn2B;
                    float k12 = mA + mB + iA * rn1A * rn2A + iB * rn1B * rn2B;

                    // Ensure a reasonable condition number.
                    const float k_maxConditionNumber = 1000.0f;
                    if (k11 * k11 < k_maxConditionNumber * (k11 * k22 - k12 * k12))
                    {
                        // K is safe to invert.
                        vc.K.ex = new Vector2(k11, k12);
                        vc.K.ey = new Vector2(k12, k22);
                        vc.normalMass = vc.K.Inverse;
                    }
                    else
                    {
                        // The constraints are redundant, just use one.
                        // TODO_ERIN use deepest?
                        vc.pointCount = 1;
                    }
                }
            }
        }

        public void WarmStart()
        {
            // Warm start.
            for (int i = 0; i < _count; ++i)
            {
                ContactVelocityConstraint vc = _velocityConstraints[i];

                int indexA = vc.indexA;
                int indexB = vc.indexB;
                float mA = vc.invMassA;
                float iA = vc.invIA;
                float mB = vc.invMassB;
                float iB = vc.invIB;
                int pointCount = vc.pointCount;

                Vector2 vA = _velocities[indexA].LinearVelocity;
                float wA = _velocities[indexA].AngularVelocity;
                Vector2 vB = _velocities[indexB].LinearVelocity;
                float wB = _velocities[indexB].AngularVelocity;

                Vector2 normal = vc.normal;
                Vector2 tangent = Vector2.Rot270(normal);

                for (int j = 0; j < pointCount; ++j)
                {
                    VelocityConstraintPoint vcp = vc.points[j];
                    Vector2 P = normal * vcp.NormalImpulse + tangent * vcp.TangentImpulse;
                    wA -= iA * Vector2.Cross(vcp.RelativeVelocityA, P);
                    vA -= P * mA;
                    wB += iB * Vector2.Cross(vcp.RelativeVelocityB, P);
                    vB += P * mB;
                }

                _velocities[indexA].LinearVelocity = vA;
                _velocities[indexA].AngularVelocity = wA;
                _velocities[indexB].LinearVelocity = vB;
                _velocities[indexB].AngularVelocity = wB;
            }
        }

        public void SolveVelocityConstraints()
        {
            /*
            if (_count >= _velocityConstraintsMultithreadThreshold && System.Environment.ProcessorCount > 1)
            {
                if (_count == 0) return;
                var batchSize = (int)Math.Ceiling((float)_count / System.Environment.ProcessorCount);
                var batches = (int)Math.Ceiling((float)_count / batchSize);

#if NET40 || NET45 || NETSTANDARD2_0
                SolveVelocityConstraintsWaitLock.Reset(batches);
                for (int i = 0; i < batches; i++)
                {
                    var start = i * batchSize;
                    var end = Math.Min(start + batchSize, _count);
                    ThreadPool.QueueUserWorkItem( SolveVelocityConstraintsCallback, SolveVelocityConstraintsState.Get(this, start,end));
                }
                // We avoid SolveVelocityConstraintsWaitLock.Wait(); because it spins a few milliseconds before going into sleep. Going into sleep(0) directly in a while loop is faster.
                while (SolveVelocityConstraintsWaitLock.CurrentCount > 0)
                    Thread.Sleep(0);
#elif PORTABLE40 || PORTABLE45 || W10 || W8_1 || WP8_1
                Parallel.For(0, batches, (i) =>
                {
                    var start = i * batchSize;
                    var end = Math.Min(start + batchSize, _count);
                    SolveVelocityConstraints(start, end);
                });
#else

                SolveVelocityConstraints(0, _count);
//#endif
            }
            else
            {
            */
                SolveVelocityConstraints(0, _count);
            //}

            return;
        }

#if NET40 || NET45 || NETSTANDARD2_0
        CountdownEvent SolveVelocityConstraintsWaitLock = new CountdownEvent(0);
        static void SolveVelocityConstraintsCallback(object state)
        {
            var svcState = (SolveVelocityConstraintsState)state;

            svcState.ContactSolver.SolveVelocityConstraints(svcState.Start, svcState.End);
            SolveVelocityConstraintsState.Return(svcState);
            svcState.ContactSolver.SolveVelocityConstraintsWaitLock.Signal();
        }

        private class SolveVelocityConstraintsState
        {
            private static System.Collections.Concurrent.ConcurrentQueue<SolveVelocityConstraintsState> _queue = new System.Collections.Concurrent.ConcurrentQueue<SolveVelocityConstraintsState>(); // pool

            public ContactSolver ContactSolver;
            public int Start { get; private set; }
            public int End { get; private set; }

            private SolveVelocityConstraintsState()
            {
            }

            internal static object Get(ContactSolver contactSolver, int start, int end)
            {
                SolveVelocityConstraintsState result;
                if (!_queue.TryDequeue(out result))
                    result = new SolveVelocityConstraintsState();

                result.ContactSolver = contactSolver;
                result.Start = start;
                result.End = end;

                return result;
            }

            internal static void Return(object state)
            {
                _queue.Enqueue((SolveVelocityConstraintsState)state);
            }
        }
#endif

        private void SolveVelocityConstraints(int start, int end)
        {
            for (int i = start; i < end; ++i)
            {
                ContactVelocityConstraint vc = _velocityConstraints[i];

#if NET40 || NET45 || NETSTANDARD2_0 || PORTABLE40 || PORTABLE45 || W10 || W8_1 || WP8_1
                // find lower order item
                int orderedIndexA = vc.indexA;
                int orderedIndexB = vc.indexB;
                if (orderedIndexB < orderedIndexA)
                {
                    orderedIndexA = vc.indexB;
                    orderedIndexB = vc.indexA;
                }

                for (; ; )
                {
                    if (Interlocked.CompareExchange(ref _locks[orderedIndexA], 1, 0) == 0)
                    {
                        if (Interlocked.CompareExchange(ref _locks[orderedIndexB], 1, 0) == 0)
                            break;
                        System.Threading.Interlocked.Exchange(ref _locks[orderedIndexA], 0);
                    }
#if NET40 || NET45 || NETSTANDARD2_0
                    Thread.Sleep(0);
#endif
                }
#endif

                int indexA = vc.indexA;
                int indexB = vc.indexB;
                float mA = vc.invMassA;
                float iA = vc.invIA;
                float mB = vc.invMassB;
                float iB = vc.invIB;
                int pointCount = vc.pointCount;

                Vector2 vA = _velocities[indexA].LinearVelocity;
                float wA = _velocities[indexA].AngularVelocity;
                Vector2 vB = _velocities[indexB].LinearVelocity;
                float wB = _velocities[indexB].AngularVelocity;

                Vector2 normal = vc.normal;
                Vector2 tangent = Vector2.Rot270(normal);
                float friction = vc.friction;

                Debug.Assert(pointCount == 1 || pointCount == 2);

                // Solve tangent constraints first because non-penetration is more important
                // than friction.
                for (int j = 0; j < pointCount; ++j)
                {
                    VelocityConstraintPoint vcp = vc.points[j];

                    // Relative velocity at contact
                    Vector2 dv = vB + Vector2.Cross(wB, vcp.RelativeVelocityB) - vA - Vector2.Cross(wA, vcp.RelativeVelocityA);

                    // Compute tangent force
                    float vt = Vector2.Dot(dv, tangent) - vc.tangentSpeed;
                    float lambda = vcp.TangentMass * (-vt);

                    // b2Clamp the accumulated force
                    float maxFriction = friction * vcp.NormalImpulse;
                    float newImpulse = Math.Clamp(vcp.TangentImpulse + lambda, -maxFriction, maxFriction);
                    lambda = newImpulse - vcp.TangentImpulse;
                    vcp.TangentImpulse = newImpulse;

                    // Apply contact impulse
                    Vector2 P = tangent * lambda;

                    vA -= P * mA;
                    wA -= iA * Vector2.Cross(vcp.RelativeVelocityA, P);

                    vB += P * mB;
                    wB += iB * Vector2.Cross(vcp.RelativeVelocityB, P);
                }

                // Solve normal constraints
                if (vc.pointCount == 1)
                {
                    VelocityConstraintPoint vcp = vc.points[0];

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

                    VelocityConstraintPoint cp1 = vc.points[0];
                    VelocityConstraintPoint cp2 = vc.points[1];

                    Vector2 a = new Vector2(cp1.NormalImpulse, cp2.NormalImpulse);
                    Debug.Assert(a.X >= 0.0f && a.Y >= 0.0f);

                    // Relative velocity at contact
                    Vector2 dv1 = vB + Vector2.Cross(wB, cp1.RelativeVelocityB) - vA - Vector2.Cross(wA, cp1.RelativeVelocityA);
                    Vector2 dv2 = vB + Vector2.Cross(wB, cp2.RelativeVelocityB) - vA - Vector2.Cross(wA, cp2.RelativeVelocityA);

                    // Compute normal velocity
                    float vn1 = Vector2.Dot(dv1, normal);
                    float vn2 = Vector2.Dot(dv2, normal);

                    Vector2 b = new Vector2();
                    b.X = vn1 - cp1.VelocityBias;
                    b.Y = vn2 - cp2.VelocityBias;

                    // Compute b'
                    b -= PhysicsMath.Mul(ref vc.K, ref a);

                    const float k_errorTol = 1e-3f;
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
                        Vector2 x = -PhysicsMath.Mul(ref vc.normalMass, ref b);

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

                            /*
#if B2_DEBUG_SOLVER
					// Postconditions
					dv1 = vB + Vector2.Cross(wB, cp1.RelativeVelocityB) - vA - Vector2.Cross(wA, cp1.RelativeVelocityA);
					dv2 = vB + Vector2.Cross(wB, cp2.RelativeVelocityB) - vA - Vector2.Cross(wA, cp2.RelativeVelocityA);

					// Compute normal velocity
					vn1 = Vector2.Dot(dv1, normal);
					vn2 = Vector2.Dot(dv2, normal);

					b2Assert(b2Abs(vn1 - cp1.VelocityBias) < k_errorTol);
					b2Assert(b2Abs(vn2 - cp2.VelocityBias) < k_errorTol);
#endif
*/
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
                        vn2 = vc.K.ex.Y * x.X + b.Y;

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

                            /*
#if B2_DEBUG_SOLVER
					// Postconditions
					dv1 = vB + Vector2.Cross(wB, cp1.RelativeVelocityB) - vA - Vector2.Cross(wA, cp1.RelativeVelocityA);

					// Compute normal velocity
					vn1 = Vector2.Dot(dv1, normal);

					b2Assert(b2Abs(vn1 - cp1.VelocityBias) < k_errorTol);
#endif
*/
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
                        vn1 = vc.K.ey.X * x.Y + b.X;
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

                            /*
#if B2_DEBUG_SOLVER
					// Postconditions
					dv2 = vB + Vector2.Cross(wB, cp2.RelativeVelocityB) - vA - Vector2.Cross(wA, cp2.RelativeVelocityA);

					// Compute normal velocity
					vn2 = Vector2.Dot(dv2, normal);

					b2Assert(b2Abs(vn2 - cp2.VelocityBias) < k_errorTol);
#endif
*/
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

                _velocities[indexA].LinearVelocity = vA;
                _velocities[indexA].AngularVelocity = wA;
                _velocities[indexB].LinearVelocity = vB;
                _velocities[indexB].AngularVelocity = wB;

#if NET40 || NET45 || NETSTANDARD2_0 || PORTABLE40 || PORTABLE45 || W10 || W8_1 || WP8_1
                System.Threading.Interlocked.Exchange(ref _locks[orderedIndexB], 0);
                System.Threading.Interlocked.Exchange(ref _locks[orderedIndexA], 0);
#endif
            }
        }

        public void StoreImpulses()
        {
            for (int i = 0; i < _count; ++i)
            {
                ContactVelocityConstraint vc = _velocityConstraints[i];
                Manifold manifold = _contacts[vc.contactIndex].Manifold;

                for (int j = 0; j < vc.pointCount; ++j)
                {
                    ManifoldPoint point = manifold.Points[j];
                    point.NormalImpulse = vc.points[j].NormalImpulse;
                    point.TangentImpulse = vc.points[j].TangentImpulse;
                    manifold.Points[j] = point;
                }

                _contacts[vc.contactIndex].Manifold = manifold;
            }
        }

        public bool SolvePositionConstraints()
        {
            bool contactsOkay = false;

            /* Sloth: Look I didn't wanna fuck with multithreading for the port
            if (_count >= _positionConstraintsMultithreadThreshold && System.Environment.ProcessorCount > 1)
            {
                if (_count == 0) return true;
                var batchSize = (int)Math.Ceiling((float)_count / System.Environment.ProcessorCount);
                var batches = (int)Math.Ceiling((float)_count / batchSize);

#if NET40 || NET45 || NETSTANDARD2_0 || PORTABLE40 || PORTABLE45 || W10 || W8_1 || WP8_1
                Parallel.For(0, batches, (i) =>
                {
                    var start = i * batchSize;
                    var end = Math.Min(start + batchSize, _count);
                    var res = SolvePositionConstraints(start, end);
                    lock (this)
                    {
                        contactsOkay = contactsOkay || res;
                    }
                });
#else
                contactsOkay = SolvePositionConstraints(0, _count);
#endif
            }
            else
            {
            */
                contactsOkay = SolvePositionConstraints(0, _count);
            //}

            return contactsOkay;
        }

        private bool SolvePositionConstraints(int start, int end)
        {
            float minSeparation = 0.0f;

            for (int i = start; i < end; ++i)
            {
                ContactPositionConstraint pc = _positionConstraints[i];

                /*
#if NET40 || NET45 || NETSTANDARD2_0 || PORTABLE40 || PORTABLE45 || W10 || W8_1 || WP8_1
                // Find lower order item.
                int orderedIndexA = pc.IndexA;
                int orderedIndexB = pc.IndexB;
                if (orderedIndexB < orderedIndexA)
                {
                    orderedIndexA = pc.IndexB;
                    orderedIndexB = pc.IndexA;
                }

                // Lock bodies.
                for (; ; )
                {
                    if (Interlocked.CompareExchange(ref _locks[orderedIndexA], 1, 0) == 0)
                    {
                        if (Interlocked.CompareExchange(ref _locks[orderedIndexB], 1, 0) == 0)
                            break;
                        System.Threading.Interlocked.Exchange(ref _locks[orderedIndexA], 0);
                    }
#if NET40 || NET45 || NETSTANDARD2_0
                    Thread.Sleep(0);
#endif
                }
#endif
*/


                int indexA = pc.IndexA;
                int indexB = pc.IndexB;
                Vector2 localCenterA = pc.LocalCenterA;
                float mA = pc.InvMassA;
                float iA = pc.InvIA;
                Vector2 localCenterB = pc.LocalCenterB;
                float mB = pc.InvMassB;
                float iB = pc.InvIB;
                int pointCount = pc.PointCount;

                Vector2 cA = _positions[indexA].Center;
                float aA = _positions[indexA].Angle;
                Vector2 cB = _positions[indexB].Center;
                float aB = _positions[indexB].Angle;

                // Solve normal constraints
                for (int j = 0; j < pointCount; ++j)
                {
                    PhysicsTransform xfA = new PhysicsTransform(Vector2.Zero, aA);
                    PhysicsTransform xfB = new PhysicsTransform(Vector2.Zero, aB);
                    xfA.Position = cA - Complex.Multiply(localCenterA, ref xfA.Quaternion);
                    xfB.Position = cB - Complex.Multiply(localCenterB, ref xfB.Quaternion);

                    Vector2 normal;
                    Vector2 point;
                    float separation;

                    PositionSolverManifold.Initialize(pc, ref xfA, ref xfB, j, out normal, out point, out separation);

                    Vector2 rA = point - cA;
                    Vector2 rB = point - cB;

                    // Track max constraint error.
                    minSeparation = Math.Min(minSeparation, separation);

                    // Prevent large corrections and allow slop.
                    float C = Math.Clamp(PhysicsSettings.Baumgarte * (separation + PhysicsSettings.LinearSlop), -PhysicsSettings.MaxLinearCorrection, 0.0f);

                    // Compute the effective mass.
                    float rnA = Vector2.Cross(rA, normal);
                    float rnB = Vector2.Cross(rB, normal);
                    float K = mA + mB + iA * rnA * rnA + iB * rnB * rnB;

                    // Compute normal impulse
                    float impulse = K > 0.0f ? -C / K : 0.0f;

                    Vector2 P = normal * impulse;

                    cA -= P * mA;
                    aA -= iA * Vector2.Cross(rA, P);

                    cB += P * mB;
                    aB += iB * Vector2.Cross(rB, P);
                }

                _positions[indexA].Center = cA;
                _positions[indexA].Angle = aA;
                _positions[indexB].Center = cB;
                _positions[indexB].Angle = aB;

                /*
#if NET40 || NET45 || NETSTANDARD2_0 || PORTABLE40 || PORTABLE45 || W10 || W8_1 || WP8_1
                // Unlock bodies.
                System.Threading.Interlocked.Exchange(ref _locks[orderedIndexB], 0);
                System.Threading.Interlocked.Exchange(ref _locks[orderedIndexA], 0);
#endif
*/
            }

            // We can't expect minSpeparation >= -b2_linearSlop because we don't
            // push the separation above -b2_linearSlop.
            return minSeparation >= -3.0f * PhysicsSettings.LinearSlop;
        }

        // Sequential position solver for position constraints.
        public bool SolveTOIPositionConstraints(int toiIndexA, int toiIndexB)
        {
            float minSeparation = 0.0f;

            for (int i = 0; i < _count; ++i)
            {
                ContactPositionConstraint pc = _positionConstraints[i];

                int indexA = pc.IndexA;
                int indexB = pc.IndexB;
                Vector2 localCenterA = pc.LocalCenterA;
                Vector2 localCenterB = pc.LocalCenterB;
                int pointCount = pc.PointCount;

                float mA = 0.0f;
                float iA = 0.0f;
                if (indexA == toiIndexA || indexA == toiIndexB)
                {
                    mA = pc.InvMassA;
                    iA = pc.InvIA;
                }

                float mB = 0.0f;
                float iB = 0.0f;
                if (indexB == toiIndexA || indexB == toiIndexB)
                {
                    mB = pc.InvMassB;
                    iB = pc.InvIB;
                }

                Vector2 cA = _positions[indexA].Center;
                float aA = _positions[indexA].Angle;

                Vector2 cB = _positions[indexB].Center;
                float aB = _positions[indexB].Angle;

                // Solve normal constraints
                for (int j = 0; j < pointCount; ++j)
                {
                    PhysicsTransform xfA = new PhysicsTransform(Vector2.Zero, aA);
                    PhysicsTransform xfB = new PhysicsTransform(Vector2.Zero, aB);
                    xfA.Position = cA - Complex.Multiply(localCenterA, ref xfA.Quaternion);
                    xfB.Position = cB - Complex.Multiply(localCenterB, ref xfB.Quaternion);

                    Vector2 normal;
                    Vector2 point;
                    float separation;

                    PositionSolverManifold.Initialize(pc, ref xfA, ref xfB, j, out normal, out point, out separation);

                    Vector2 rA = point - cA;
                    Vector2 rB = point - cB;

                    // Track max constraint error.
                    minSeparation = Math.Min(minSeparation, separation);

                    // Prevent large corrections and allow slop.
                    float C = Math.Clamp(PhysicsSettings.Baumgarte * (separation + PhysicsSettings.LinearSlop), -PhysicsSettings.MaxLinearCorrection, 0.0f);

                    // Compute the effective mass.
                    float rnA = Vector2.Cross(rA, normal);
                    float rnB = Vector2.Cross(rB, normal);
                    float K = mA + mB + iA * rnA * rnA + iB * rnB * rnB;

                    // Compute normal impulse
                    float impulse = K > 0.0f ? -C / K : 0.0f;

                    Vector2 P = normal * impulse;

                    cA -= P * mA;
                    aA -= iA * Vector2.Cross(rA, P);

                    cB += P * mB;
                    aB += iB * Vector2.Cross(rB, P);
                }

                _positions[indexA].Center = cA;
                _positions[indexA].Angle = aA;

                _positions[indexB].Center = cB;
                _positions[indexB].Angle = aB;
            }

            // We can't expect minSpeparation >= -b2_linearSlop because we don't
            // push the separation above -b2_linearSlop.
            return minSeparation >= -1.5f * PhysicsSettings.LinearSlop;
        }

        public static class WorldManifold
        {
            /// <summary>
            /// Evaluate the manifold with supplied PhysicsTransforms. This assumes
            /// modest motion from the original state. This does not change the
            /// point count, impulses, etc. The radii must come from the Shapes
            /// that generated the manifold.
            /// </summary>
            /// <param name="manifold">The manifold.</param>
            /// <param name="xfA">The PhysicsTransform for A.</param>
            /// <param name="radiusA">The radius for A.</param>
            /// <param name="xfB">The PhysicsTransform for B.</param>
            /// <param name="radiusB">The radius for B.</param>
            /// <param name="normal">World vector pointing from A to B</param>
            /// <param name="points">Torld contact point (point of intersection).</param>
            public static void Initialize(Manifold manifold, ref PhysicsTransform xfA, float radiusA, ref PhysicsTransform xfB, float radiusB, out Vector2 normal, out Vector2[] points)
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
                            Vector2 pointA = PhysicsTransform.Multiply(ref manifold.LocalPoint, ref xfA);
                            Vector2 pointB = PhysicsTransform.Multiply(manifold.Points[0].LocalPoint, ref xfB);
                            if (Vector2.DistanceSquared(pointA, pointB) > float.Epsilon * float.Epsilon)
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
                            normal = Complex.Multiply(manifold.LocalNormal, ref xfA.Quaternion);
                            Vector2 planePoint = PhysicsTransform.Multiply(ref manifold.LocalPoint, ref xfA);

                            for (int i = 0; i < manifold.PointCount; ++i)
                            {
                                Vector2 clipPoint = PhysicsTransform.Multiply(manifold.Points[i].LocalPoint, ref xfB);
                                Vector2 cA = clipPoint + normal * (radiusA - Vector2.Dot(clipPoint - planePoint, normal));
                                Vector2 cB = clipPoint - normal * radiusB;
                                points[i] = (cA + cB) * 0.5f;
                            }
                        }
                        break;

                    case ManifoldType.FaceB:
                        {
                            normal = Complex.Multiply(manifold.LocalNormal, ref xfB.Quaternion);
                            Vector2 planePoint = PhysicsTransform.Multiply(ref manifold.LocalPoint, ref xfB);

                            for (int i = 0; i < manifold.PointCount; ++i)
                            {
                                Vector2 clipPoint = PhysicsTransform.Multiply(manifold.Points[i].LocalPoint, ref xfA);
                                Vector2 cB = clipPoint + normal * (radiusB - Vector2.Dot(clipPoint - planePoint, normal));
                                Vector2 cA = clipPoint - normal * radiusA;
                                points[i] = (cA + cB) * 0.5f;
                            }

                            // Ensure normal points from A to B.
                            normal = -normal;
                        }
                        break;
                }
            }
        }

        private static class PositionSolverManifold
        {
            public static void Initialize(ContactPositionConstraint pc, ref PhysicsTransform xfA, ref PhysicsTransform xfB, int index, out Vector2 normal, out Vector2 point, out float separation)
            {
                Debug.Assert(pc.PointCount > 0);

                switch (pc.Type)
                {
                    case ManifoldType.Circles:
                        {
                            Vector2 pointA = PhysicsTransform.Multiply(ref pc.LocalPoint, ref xfA);
                            Vector2 pointB = PhysicsTransform.Multiply(pc.LocalPoints[0], ref xfB);
                            normal = pointB - pointA;

                            // Handle zero normalization
                            if (normal != Vector2.Zero)
                                normal = normal.Normalized;

                            point = (pointA + pointB) * 0.5f;
                            separation = Vector2.Dot(pointB - pointA, normal) - pc.RadiusA - pc.RadiusB;
                        }
                        break;

                    case ManifoldType.FaceA:
                        {
                            Complex.Multiply(pc.LocalNormal, xfA.Quaternion, out normal);
                            Vector2 planePoint = PhysicsTransform.Multiply(ref pc.LocalPoint, ref xfA);

                            Vector2 clipPoint = PhysicsTransform.Multiply(pc.LocalPoints[index], ref xfB);
                            separation = Vector2.Dot(clipPoint - planePoint, normal) - pc.RadiusA - pc.RadiusB;
                            point = clipPoint;
                        }
                        break;

                    case ManifoldType.FaceB:
                        {
                            Complex.Multiply(pc.LocalNormal, xfB.Quaternion, out normal);
                            Vector2 planePoint = PhysicsTransform.Multiply(ref pc.LocalPoint, ref xfB);

                            Vector2 clipPoint = PhysicsTransform.Multiply(pc.LocalPoints[index], ref xfA);
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
}
