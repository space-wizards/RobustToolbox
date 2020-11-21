using System;
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
            throw new NotImplementedException();

            for (var i = 0; i < BodyCount; i++)
            {
                var body = Bodies[i];
                // TODO.
            }
        }
    }
}
