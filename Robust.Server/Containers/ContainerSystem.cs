using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Server.Containers;

public sealed class ContainerSystem : SharedContainerSystem
{

    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ContainerManagerComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, ContainerManagerComponent component, ComponentStartup args)
    {
        var query = GetEntityQuery<MetaDataComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        foreach (var cont in component.Containers.Values)
        {
            foreach (var ent in cont.ContainedEntities)
            {
                var meta = query.GetComponent(ent);

                //DebugTools.Assert((meta.Flags & MetaDataFlags.InContainer) != 0);

                // TODO remove all this just have the above assert all wrapped in an #if DEBUG
                // this is just here cause I CBF updating all maps.

                meta.Flags |= MetaDataFlags.InContainer;
                _lookup.RemoveFromEntityTree(ent, xformQuery.GetComponent(ent), xformQuery);
            }
        }
    }
}
