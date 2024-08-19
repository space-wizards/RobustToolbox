using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Responsible for applying relevant changes to active entities when prototypes are reloaded.
/// </summary>
internal sealed class PrototypeReloadSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs eventArgs)
    {
        if (!eventArgs.ByType.TryGetValue(typeof(EntityPrototype), out var set))
            return;

        var query = AllEntityQuery<MetaDataComponent>();
        while (query.MoveNext(out var uid, out var metadata))
        {
            var id = metadata.EntityPrototype?.ID;
            if (id == null || !set.Modified.ContainsKey(id))
                continue;

            var proto = _prototypes.Index<EntityPrototype>(id);
            UpdateEntity(uid, metadata, proto);
        }
    }

    private bool IsIgnored(EntityPrototype.ComponentRegistryEntry entry)
    {
        var compType = entry.Component.GetType();

        if (compType == typeof(TransformComponent) || compType == typeof(MetaDataComponent))
            return true;

        return false;
    }

    private void UpdateEntity(EntityUid entity, MetaDataComponent metaData, EntityPrototype newPrototype)
    {
        var oldPrototype = metaData.EntityPrototype;
        var modified = false;

        if (oldPrototype != null)
        {
            foreach (var oldComp in oldPrototype.Components)
            {
                if (IsIgnored(oldComp.Value))
                    continue;

                // Removed
                if (!newPrototype.Components.TryGetValue(oldComp.Key, out var newComp))
                {
                    modified = true;
                    RemComp(entity, oldComp.Value.Component.GetType());
                    continue;
                }

                // Modified
                if (!newComp.Mapping.Equals(oldComp.Value.Mapping))
                {
                    modified = true;
                    EntityManager.AddComponent(entity, newComp, overwrite: true, metaData);
                }
            }
        }

        foreach (var newComp in newPrototype.Components)
        {
            if (IsIgnored(newComp.Value))
                continue;

            // Existing component, handled above
            if (oldPrototype?.Components.ContainsKey(newComp.Key) == true)
                continue;

            // Added
            modified = true;
            EntityManager.AddComponent(entity, newComp.Value, overwrite: true, metadata: metaData);
        }

        if (modified)
        {
            EntityManager.CullRemovedComponents();
        }

        // Update entity metadata
        metaData.EntityPrototype = newPrototype;
    }
}
