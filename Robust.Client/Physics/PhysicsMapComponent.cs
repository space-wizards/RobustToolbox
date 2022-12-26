using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;

namespace Robust.Client.Physics
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedPhysicsMapComponent))]
    public sealed class PhysicsMapComponent : SharedPhysicsMapComponent
    {
    }
}
