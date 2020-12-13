using System;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Solver;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    internal sealed class PhysicsIsland
    {
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

        internal void Solve(float frameTime)
        {
            // TODO: Our AABBs are going to need to be relative to the grid we are on most likely
            // So I'll need to change a bunch of internal shit for that (RIP me).
            // Broadphase will probably be the only area we need to change it I HOPE.
            // TODO: Maybe add a method called "GridAABB" on transform we can call?

            // Integrate velocities and apply damping. Initialize the body state.
            for (var i = 0; i < BodyCount; ++i)
            {
                PhysicsComponent b = Bodies[i];

                Vector2 c = b.Sweep.Center;
                float a = b.Sweep.Angle;
                Vector2 v = b.LinearVelocity;
                float w = b.AngularVelocity;

                // Store positions for continuous collision.
                b.Sweep.Center0 = b.Sweep.Center;
                b.Sweep.Angle0 = b.Sweep.Angle;

                if (b.BodyType == BodyType.Dynamic)
                {
                    // Integrate velocities.

                    // FPE: Only apply gravity if the body wants it.
                    v += (b.Force * b.InvMass) * frameTime;
                    w += b.InvI * b.Torque * frameTime;

                    // Apply damping.
                    // ODE: dv/dt + c * v = 0
                    // Solution: v(t) = v0 * exp(-c * t)
                    // Time step: v(t + dt) = v0 * exp(-c * (t + dt)) = v0 * exp(-c * t) * exp(-c * dt) = v * exp(-c * dt)
                    // v2 = exp(-c * dt) * v1
                    // Taylor expansion:
                    // v2 = (1.0f - c * dt) * v1
                    v *=  Math.Clamp(1.0f - frameTime * b.LinearDamping, 0.0f, 1.0f);
                    w *= Math.Clamp(1.0f - frameTime * b.AngularDamping, 0.0f, 1.0f);
                }

                _positions[i].C = c;
                _positions[i].A = a;
                _velocities[i].V = v;
                _velocities[i].W = w;
            }

            var solverData = new SolverData();
            solverData.Positions = _positions;
            solverData.Velocities = _velocities;

            // Reset
            //_contactSolver
            // TODO: Up to warmstarting
        }
    }
}
