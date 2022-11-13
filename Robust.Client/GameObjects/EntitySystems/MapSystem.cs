using Robust.Client.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;

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
