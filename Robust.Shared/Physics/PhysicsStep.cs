namespace Robust.Shared.Physics
{
    // AKA TimeStep in Aether2d
    internal struct PhysicsStep
    {
        public float DeltaTime;

        /// <summary>
        ///     DeltaTime * InvDt0
        /// </summary>
        public float DtRatio;

        /// <summary>
        ///     Inverse of <see cref="DeltaTime"/> (0f if DeltaTime is 0f)
        /// </summary>
        public float InvDt;

        // TODO: bytes
        public int PositionIterations;

        public int VelocityIterations;

        public bool WarmStarting;
    }
}
