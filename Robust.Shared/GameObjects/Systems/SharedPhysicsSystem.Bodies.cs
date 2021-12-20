using Robust.Shared.Containers;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedPhysicsSystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    private void OnPhysicsInit(EntityUid uid, PhysicsComponent component, ComponentInit args)
    {
        if (component.BodyType == BodyType.Static)
        {
            component._awake = false;
        }

        var xform = Transform(uid);

        // TODO: Ordering fuckery need a new PR to fix some of this stuff
        if (xform.MapID != MapId.Nullspace)
            component.PhysicsMap = EntityManager.GetComponent<SharedPhysicsMapComponent>(_mapManager.GetMapEntityId(xform.MapID));

        Dirty(uid);
        // Yeah yeah TODO Combine these
        // Implicitly assume that stuff doesn't cover if a non-collidable is initialized.

        if (component.CanCollide)
        {
            if (!component.Awake)
            {
                EntityManager.EventBus.RaiseEvent(EventSource.Local, new PhysicsSleepMessage(component));
            }
            else
            {
                EntityManager.EventBus.RaiseEvent(EventSource.Local, new PhysicsWakeMessage(component));
            }

            if (!_containerSystem.IsEntityInContainer(uid, xform))
            {
                // TODO: Probably a bad idea but ehh future sloth's problem; namely that we have to duplicate code between here and CanCollide.
                EntityManager.EventBus.RaiseLocalEvent(uid, new CollisionChangeMessage(component, uid, component._canCollide));
                EntityManager.EventBus.RaiseLocalEvent(uid, new PhysicsUpdateMessage(component));
            }
        }
        else
        {
            component._awake = false;
        }

        var startup = new PhysicsInitializedEvent(uid);
        EntityManager.EventBus.RaiseLocalEvent(uid, ref startup);

        component.ResetMassData();
    }
}
