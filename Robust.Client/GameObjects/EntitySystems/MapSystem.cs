using Robust.Client.Physics;
using Robust.Shared.GameObjects;

namespace Robust.Client.GameObjects
{
    public sealed class MapSystem : SharedMapSystem
    {
        protected override void OnMapAdd(EntityUid uid, MapComponent component, ComponentAdd args)
        {
            EnsureComp<PhysicsMapComponent>(uid);
        }
    }
}
