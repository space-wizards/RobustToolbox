namespace Robust.Shared.Physics.Systems;

public partial class SharedPhysicsSystem
{
    // Loosely corresponds to the box2d equivalent for sanity reasons.
    // EntityManager handles worlds not being connected so we just store this on the system.
    internal PhysicsWorld World { get; private set; } = default!;

    private void InitializeWorld()
    {
        World = new();
    }

    private void ShutdownWorld()
    {

    }
}
