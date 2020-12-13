using Robust.Shared.Maths;

namespace Robust.Shared.Physics.Solver
{
    internal struct SolverPosition
    {
        public Vector2 C;
        public float A;
    }

    internal struct SolverVelocity
    {
        public Vector2 V;
        // TODO: wtf is this
        public float W;
    }

    internal struct SolverData
    {
        internal SolverPosition[] Positions;
        internal SolverVelocity[] Velocities;
    }
}
