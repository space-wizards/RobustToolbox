using System;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    internal sealed class PhysicsIsland
    {
        private IContactManager _contactManager = default!;

        private Contact[] _contacts = {};
        private Joint[] _joints = {};

        internal IPhysBody[] Bodies { get; set; } = { };

        internal int BodyCount { get; set; }
        internal int ContactCount { get; set; }
        internal int JointCount { get; set; }

        internal int BodyCapacity { get; set; }
        internal int ContactCapacity { get; set; }
        internal int JointCapacity { get; set; }

        private const int GrowSize = 32;

        internal void Add(IPhysBody body)
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
                Bodies = new IPhysBody[newBodyBufferCapacity];
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

            // TODO: Reduce
            float h = frameTime;

            // Integrate velocities and apply damping. Initialize the body state.
            for (int i = 0; i < BodyCount; ++i)
            {
                IPhysBody b = Bodies[i];

                Vector2 c = b._sweep.C;
                float a = b._sweep.A;
                Vector2 v = b._linearVelocity;
                float w = b._angularVelocity;

                // Store positions for continuous collision.
                b._sweep.C0 = b._sweep.C;
                b._sweep.A0 = b._sweep.A;

                if (b.BodyType == BodyType.Dynamic)
                {
                    // Integrate velocities.

                    // FPE: Only apply gravity if the body wants it.
                    if (b.IgnoreGravity)
                        v += h * (b._invMass * b._force);
                    else
                        v += h * (gravity + b._invMass * b._force);

                    w += h * b._invI * b._torque;

                    // Apply damping.
                    // ODE: dv/dt + c * v = 0
                    // Solution: v(t) = v0 * exp(-c * t)
                    // Time step: v(t + dt) = v0 * exp(-c * (t + dt)) = v0 * exp(-c * t) * exp(-c * dt) = v * exp(-c * dt)
                    // v2 = exp(-c * dt) * v1
                    // Taylor expansion:
                    // v2 = (1.0f - c * dt) * v1
                    v *= MathUtils.Clamp(1.0f - h * b.LinearDamping, 0.0f, 1.0f);
                    w *= MathUtils.Clamp(1.0f - h * b.AngularDamping, 0.0f, 1.0f);
                }

                _positions[i].c = c;
                _positions[i].a = a;
                _velocities[i].v = v;
                _velocities[i].w = w;
            }
        }
    }
}
