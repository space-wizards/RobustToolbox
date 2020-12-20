using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Solver;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    internal sealed class PhysicsIsland
    {
        /*
         * Sloth notes:
         * I considered doing islands per-grid to simplify things but then figured it might make for some jank
         * cross-shuttle collisions. The main downside of doing it in the world afaik is that you might get
         * issues with velocity differences between entities on shuttles and whatnot but I guess we'll see how we go.
         */

        private IContactManager _contactManager = default!;

        private ContactSolver _contactSolver = new ContactSolver();

        private Contact[] _contacts = {};
        private Joint[] _joints = {};

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

        internal void Reset(int bodyCapacity, int contactCapacity, int jointsCapacity, IContactManager contactManager)
        {
            BodyCapacity = bodyCapacity;
            ContactCapacity = contactCapacity;
            JointCapacity = jointsCapacity;
            BodyCount = 0;
            ContactCount = 0;
            JointCount = 0;

            // TODO: Make this a dependency instead?
            _contactManager = contactManager;

            if (Bodies.Length < bodyCapacity)
            {
                // Grow by 32
                var newBodyBufferCapacity = Math.Max(bodyCapacity, GrowSize);
                newBodyBufferCapacity = (newBodyBufferCapacity + GrowSize - 1) & ~(GrowSize - 1);
                Bodies = new PhysicsComponent[newBodyBufferCapacity];
                // velocities
                // positions
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

            // Integrate velocities and apply damping. Initialize the body state.
            for (var i = 0; i < BodyCount; ++i)
            {
                PhysicsComponent b = Bodies[i];
                var sweep = b.Sweeps[GridId];

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
                    v += (b.Force * b.InvMass) * step.DeltaTime;
                    w += b.InvI * b.Torque * step.DeltaTime;

                    // Apply damping.
                    // ODE: dv/dt + c * v = 0
                    // Solution: v(t) = v0 * exp(-c * t)
                    // Time step: v(t + dt) = v0 * exp(-c * (t + dt)) = v0 * exp(-c * t) * exp(-c * dt) = v * exp(-c * dt)
                    // v2 = exp(-c * dt) * v1
                    // Taylor expansion:
                    // v2 = (1.0f - c * dt) * v1
                    v *=  Math.Clamp(1.0f - step.DeltaTime * b.LinearDamping, 0.0f, 1.0f);
                    w *= Math.Clamp(1.0f - step.DeltaTime * b.AngularDamping, 0.0f, 1.0f);
                }

                _positions[i].Center = c;
                _positions[i].Angle = a;
                _velocities[i].LinearVelocity = v;
                _velocities[i].AngularVelocity = w;
            }

            var solverData = new SolverData();
            solverData.Positions = _positions;
            solverData.Velocities = _velocities;

            // TODO: Up to warmstarting
            _contactSolver.Reset();
            _contactSolver.InitializeVelocityConstraints();

            if (step.WarmStarting)
            {
                _contactSolver.WarmStart();
            }

            for (var i = 0; i < JointCount; i++)
            {
                if (_joints[i].Enabled)
                    _joints[i].InitVelocityConstraints(solverData);
            }
        }
    }
}
