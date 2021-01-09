using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Joints;
using Robust.Shared.Physics.Solver;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    public sealed class PhysicsIsland
    {
        /*
         * Sloth notes:
         * I considered doing islands per-grid to simplify things but then figured it might make for some jank
         * cross-shuttle collisions. The main downside of doing it in the world afaik is that you might get
         * issues with velocity differences between entities on shuttles and whatnot but I guess we'll see how we go.
         */

        private ContactManager? _contactManager;

        private ContactSolver _contactSolver = new();

        private Contact[] _contacts = {};
        private Joint[] _joints = {};

        private const float LinTolSqr = PhysicsSettings.LinearSleepTolerance * PhysicsSettings.LinearSleepTolerance;
        private const float AngTolSqr = PhysicsSettings.AngularSleepTolerance * PhysicsSettings.AngularSleepTolerance;

        internal PhysicsComponent[] Bodies { get; set; } = Array.Empty<PhysicsComponent>();

        internal SolverVelocity[] _velocities = Array.Empty<SolverVelocity>();
        internal SolverPosition[] _positions = Array.Empty<SolverPosition>();
        // I didn't port the locks because some of the multi-threaded code was ICKY

        internal int BodyCount { get; set; }
        internal int ContactCount { get; set; }
        internal int JointCount { get; set; }

        internal int BodyCapacity { get; set; }
        internal int ContactCapacity { get; set; }
        internal int JointCapacity { get; set; }

        public TimeSpan JointUpdateTime;

        private const int GrowSize = 32;

        internal void Add(PhysicsComponent body)
        {
            DebugTools.Assert(BodyCount < BodyCapacity);
            body.IslandIndex = BodyCount;
            Bodies[BodyCount++] = body;
        }

        internal void Add(Contact contact)
        {
            DebugTools.Assert(ContactCount < ContactCapacity);
            _contacts[ContactCount++] = contact;
        }

        internal void Add(Joint joint)
        {
            DebugTools.Assert(JointCount < JointCapacity);
            _joints[JointCount++] = joint;
        }

        internal void Clear()
        {
            BodyCount = 0;
            ContactCount = 0;
            JointCount = 0;
        }

        internal void Reset(int bodyCapacity, int contactCapacity, int jointsCapacity, ContactManager contactManager)
        {
            _contactManager = contactManager;
            BodyCapacity = bodyCapacity;
            ContactCapacity = contactCapacity;
            JointCapacity = jointsCapacity;
            BodyCount = 0;
            ContactCount = 0;
            JointCount = 0;

            if (Bodies.Length < bodyCapacity)
            {
                // Grow by 32
                var newBodyBufferCapacity = Math.Max(bodyCapacity, GrowSize);
                newBodyBufferCapacity = (newBodyBufferCapacity + GrowSize - 1) & ~(GrowSize - 1);
                Bodies = new PhysicsComponent[newBodyBufferCapacity];
                _velocities = new SolverVelocity[newBodyBufferCapacity];
                _positions = new SolverPosition[newBodyBufferCapacity];
                // locks eeeeeee
            }

            if (_contacts.Length < contactCapacity)
            {
                // Grow by x 1.125f
                var newContactBufferCapacity = Math.Max(contactCapacity, GrowSize);
                // TODO: Should the bitshift be based on GrowSize? ehhh future problem
                newContactBufferCapacity = newContactBufferCapacity + (newContactBufferCapacity * 2 >> 4);
                newContactBufferCapacity = (newContactBufferCapacity + (GrowSize - 1)) & ~(GrowSize - 1);
                _contacts = new Contact[newContactBufferCapacity];
            }

            if (_joints.Length < jointsCapacity)
            {
                var newJointBufferCapacity = Math.Max(jointsCapacity, GrowSize);
                newJointBufferCapacity = (newJointBufferCapacity + (GrowSize - 1)) & ~(GrowSize - 1);
                _joints = new Joint[newJointBufferCapacity];
            }
        }

        // TODO: Gravity but not so important for us

        // TODO: Just have the one Sweep motion that is done in worldposition and don't do per-grid.
        // The ideal is that we can do collisions with entities spanning multiple grids and not have it bug the fuck out

        internal void Solve(PhysicsStep step)
        {
            // TODO: Our AABBs are going to need to be relative to the grid we are on most likely
            // So I'll need to change a bunch of internal shit for that (RIP me).
            // Broadphase will probably be the only area we need to change it I HOPE.
            // TODO: Maybe add a method called "GridAABB" on transform we can call?
            var h = step.DeltaTime;

            // Integrate velocities and apply damping. Initialize the body state.
            for (var i = 0; i < BodyCount; ++i)
            {
                PhysicsComponent b = Bodies[i];
                var sweep = b.Sweep;

                // TODO: Need to suss out where relative velocity done because if the shuttle's travelling at high speeds
                // we don't want collisions to be fucked.

                Vector2 c = sweep.Center;
                float a = sweep.Angle;
                Vector2 v = b.LinearVelocity;
                float w = b.AngularVelocity;

                // Store positions for continuous collision.
                sweep.Center0 = sweep.Center;
                sweep.Angle0 = sweep.Angle;

                if (b.BodyType == BodyType.Dynamic)
                {
                    // Integrate velocities.

                    // FPE: Only apply gravity if the body wants it.
                    v += (b.Force * b.InvMass) * h;
                    w += b.InvI * b.Torque * h;

                    // Apply damping.
                    // ODE: dv/dt + c * v = 0
                    // Solution: v(t) = v0 * exp(-c * t)
                    // Time step: v(t + dt) = v0 * exp(-c * (t + dt)) = v0 * exp(-c * t) * exp(-c * dt) = v * exp(-c * dt)
                    // v2 = exp(-c * dt) * v1
                    // Taylor expansion:
                    // v2 = (1.0f - c * dt) * v1
                    v *=  Math.Clamp(1.0f - h * b.LinearDamping, 0.0f, 1.0f);
                    w *= Math.Clamp(1.0f - h * b.AngularDamping, 0.0f, 1.0f);
                }

                _positions[i].Center = c;
                _positions[i].Angle = a;
                _velocities[i].LinearVelocity = v;
                _velocities[i].AngularVelocity = w;
            }

            var solverData = new SolverData
            {
                Positions = _positions,
                Velocities = _velocities
            };

            _contactSolver.Reset(ref step, ContactCount, _contacts, _positions, _velocities);
            _contactSolver.InitializeVelocityConstraints();

            if (step.WarmStarting)
            {
                _contactSolver.WarmStart();
            }

            for (var i = 0; i < JointCount; i++)
            {
                if (_joints[i].Enabled)
                    _joints[i].InitVelocityConstraints(ref solverData);
            }

            // Solve velocity constraints.
            for (int i = 0; i < step.VelocityIterations; ++i)
            {
                for (int j = 0; j < JointCount; ++j)
                {
                    Joint joint = _joints[j];

                    if (!joint.Enabled)
                        continue;

                    joint.SolveVelocityConstraints(ref solverData);
                    joint.Validate(step.InvDt);
                }

                _contactSolver.SolveVelocityConstraints();
            }

            // Store impulses for warm starting.
            _contactSolver.StoreImpulses();

            // Integrate positions
            for (int i = 0; i < BodyCount; ++i)
            {
                Vector2 c = _positions[i].Center;
                float a = _positions[i].Angle;
                Vector2 v = _velocities[i].LinearVelocity;
                float w = _velocities[i].AngularVelocity;

                // Check for large velocities
                Vector2 translation = v * h;
                if (Vector2.Dot(translation, translation) > PhysicsSettings.MaxTranslationSquared)
                {
                    float ratio = PhysicsSettings.MaxTranslation / translation.Length;
                    v *= ratio;
                }

                float rotation = h * w;
                if (rotation * rotation > PhysicsSettings.MaxRotationSquared)
                {
                    float ratio = PhysicsSettings.MaxRotation / Math.Abs(rotation);
                    w *= ratio;
                }

                // Integrate
                c += v * h;
                a += h * w;

                _positions[i].Center = c;
                _positions[i].Angle = a;
                _velocities[i].LinearVelocity = v;
                _velocities[i].AngularVelocity = w;
            }

            // TODO: I Should probably enable some kind of perf stuff back onto physics

            // Solve position constraints
            bool positionSolved = false;
            for (int i = 0; i < step.PositionIterations; ++i)
            {
                bool contactsOkay = _contactSolver.SolvePositionConstraints();

                bool jointsOkay = true;
                for (int j = 0; j < JointCount; ++j)
                {
                    Joint joint = _joints[j];

                    if (!joint.Enabled)
                        continue;

                    bool jointOkay = joint.SolvePositionConstraints(ref solverData);

                    jointsOkay = jointsOkay && jointOkay;
                }

                if (contactsOkay && jointsOkay)
                {
                    // Exit early if the position errors are small.
                    positionSolved = true;
                    break;
                }
            }

            // Copy state buffers back to the bodies
            for (int i = 0; i < BodyCount; ++i)
            {
                PhysicsComponent body = Bodies[i];
                body.Sweep.Center = _positions[i].Center;
                body.Sweep.Angle = _positions[i].Angle;
                body.LinearVelocity = _velocities[i].LinearVelocity;
                body.AngularVelocity = _velocities[i].AngularVelocity;
                body.SynchronizeTransform();
            }

            Report(_contactSolver._velocityConstraints);

            if (PhysicsSettings.AllowSleep)
            {
                float minSleepTime = float.MaxValue;

                for (int i = 0; i < BodyCount; ++i)
                {
                    PhysicsComponent b = Bodies[i];

                    if (b.BodyType == BodyType.Static)
                        continue;

                    if (!b.SleepingAllowed ||
                        b.AngularVelocity * b.AngularVelocity > AngTolSqr ||
                        Vector2.Dot(b.LinearVelocity, b.LinearVelocity) > LinTolSqr)
                    {
                        b.SleepTime = 0.0f;
                        minSleepTime = 0.0f;
                    }
                    else
                    {
                        b.SleepTime += h;
                        minSleepTime = Math.Min(minSleepTime, b.SleepTime);
                    }
                }

                if (minSleepTime >= PhysicsSettings.TimeToSleep && positionSolved)
                {
                    for (int i = 0; i < BodyCount; ++i)
                    {
                        PhysicsComponent b = Bodies[i];
                        b.Awake = false;
                    }
                }
            }
        }

        internal void SolveTOI(ref PhysicsStep subStep, int toiIndexA, int toiIndexB)
        {
            DebugTools.Assert(toiIndexA < BodyCount);
            DebugTools.Assert(toiIndexB < BodyCount);

            // Initialize the body state.
            for (int i = 0; i < BodyCount; ++i)
            {
                PhysicsComponent b = Bodies[i];
                _positions[i].Center = b.Sweep.Center;
                _positions[i].Angle = b.Sweep.Angle;
                _velocities[i].LinearVelocity = b.LinearVelocity;
                _velocities[i].AngularVelocity = b.AngularVelocity;
            }

            _contactSolver.Reset(ref subStep, ContactCount, _contacts, _positions, _velocities);

            // Solve position constraints.
            for (int i = 0; i < subStep.PositionIterations; ++i)
            {
                bool contactsOkay = _contactSolver.SolveTOIPositionConstraints(toiIndexA, toiIndexB);
                if (contactsOkay)
                {
                    break;
                }
            }

            // Leap of faith to new safe state.
            Bodies[toiIndexA].Sweep.Center0 = _positions[toiIndexA].Center;
            Bodies[toiIndexA].Sweep.Angle0 = _positions[toiIndexA].Angle;
            Bodies[toiIndexB].Sweep.Center0 = _positions[toiIndexB].Center;
            Bodies[toiIndexB].Sweep.Angle0 = _positions[toiIndexB].Angle;

            // No warm starting is needed for TOI events because warm
            // starting impulses were applied in the discrete solver.
            _contactSolver.InitializeVelocityConstraints();

            // Solve velocity constraints.
            for (int i = 0; i < subStep.VelocityIterations; ++i)
            {
                _contactSolver.SolveVelocityConstraints();
            }

            // Don't store the TOI contact forces for warm starting
            // because they can be quite large.

            float h = subStep.DeltaTime;

            // Integrate positions.
            for (int i = 0; i < BodyCount; ++i)
            {
                Vector2 c = _positions[i].Center;
                float a = _positions[i].Angle;
                Vector2 v = _velocities[i].LinearVelocity;
                float w = _velocities[i].AngularVelocity;

                // Check for large velocities
                Vector2 translation = v * h;
                if (Vector2.Dot(translation, translation) > PhysicsSettings.MaxTranslationSquared)
                {
                    float ratio = PhysicsSettings.MaxTranslation / translation.Length;
                    v *= ratio;
                }

                float rotation = h * w;
                if (rotation * rotation > PhysicsSettings.MaxRotationSquared)
                {
                    float ratio = PhysicsSettings.MaxRotation / Math.Abs(rotation);
                    w *= ratio;
                }

                // Integrate
                c += v * h;
                a += h * w;

                _positions[i].Center = c;
                _positions[i].Angle = a;
                _velocities[i].LinearVelocity = v;
                _velocities[i].AngularVelocity = w;

                // Sync bodies
                PhysicsComponent body = Bodies[i];
                body.Sweep.Center = c;
                body.Sweep.Angle = a;
                body.LinearVelocity = v;
                body.AngularVelocity = w;
                body.SynchronizeTransform();
            }

            Report(_contactSolver._velocityConstraints);
        }

        private void Report(ContactVelocityConstraint[] constraints)
        {
            if (_contactManager == null)
                return;

            for (int i = 0; i < ContactCount; ++i)
            {
                Contact c = _contacts[i];

                //FPE optimization: We don't store the impulses and send it to the delegate. We just send the whole contact.
                //FPE feature: added after collision
                c.FixtureA?.AfterCollision?.Invoke(c.FixtureA, c.FixtureB, c, constraints[i]);

                c.FixtureB?.AfterCollision?.Invoke(c.FixtureB, c.FixtureA, c, constraints[i]);

                // TODO: Need to start calling PostCollide again
                if (c.FixtureA != null && c.FixtureB != null)
                {
                    foreach (var component in c.FixtureA.Body.Owner.GetAllComponents<ICollideBehavior>())
                    {
                        component.CollideWith(c.FixtureB.Body.Owner);
                    }

                    foreach (var component in c.FixtureB.Body.Owner.GetAllComponents<ICollideBehavior>())
                    {
                        component.CollideWith(c.FixtureA.Body.Owner);
                    }
                }

                _contactManager.PostSolve?.Invoke(c, constraints[i]);
            }
        }
    }
}
