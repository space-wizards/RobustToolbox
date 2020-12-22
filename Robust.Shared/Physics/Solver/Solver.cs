using Robust.Shared.Maths;

namespace Robust.Shared.Physics.Solver
{
    internal struct SolverPosition
    {
        public Vector2 Center;
        public float Angle;
    }

    internal struct SolverVelocity
    {
        public Vector2 LinearVelocity;
        // TODO: wtf is this
        public float AngularVelocity;
    }

    internal struct SolverData
    {
        internal PhysicsStep Step;
        internal SolverPosition[] Positions;
        internal SolverVelocity[] Velocities;
    }
}
