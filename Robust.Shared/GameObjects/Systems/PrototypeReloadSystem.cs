using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Responsible for applying relevant changes to active entities when prototypes are reloaded.
/// </summary>
internal sealed class PrototypeReloadSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

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

    private void UpdateEntity(EntityUid entity, MetaDataComponent metaData, EntityPrototype newPrototype)
    {
        var oldPrototype = metaData.EntityPrototype;

        var oldPrototypeComponents = oldPrototype?.Components.Keys
            .Where(n => n != "Transform" && n != "MetaData")
            .Select(name => (name, _componentFactory.GetRegistration(name).Type))
            .ToList() ?? new List<(string name, Type Type)>();

        var newPrototypeComponents = newPrototype.Components.Keys
            .Where(n => n != "Transform" && n != "MetaData")
            .Select(name => (name, _componentFactory.GetRegistration(name).Type))
            .ToList();

        var ignoredComponents = new List<string>();

        // Find components to be removed, and remove them
        foreach (var (name, type) in oldPrototypeComponents.Except(newPrototypeComponents))
        {
            if (newPrototype.Components.ContainsKey(name))
            {
                ignoredComponents.Add(name);
                continue;
            }

            RemComp(entity, type);
        }

        EntityManager.CullRemovedComponents();

        // Add new components
        foreach (var (name, _) in newPrototypeComponents.Where(t => !ignoredComponents.Contains(t.name))
                     .Except(oldPrototypeComponents))
        {
            var data = newPrototype.Components[name];
            var component = _componentFactory.GetComponent(name);

            if (!EntityManager.HasComponent(entity, component.GetType()))
                EntityManager.AddComponent(entity, component);
        }

        // Update entity metadata
        metaData.EntityPrototype = newPrototype;
    }
}
