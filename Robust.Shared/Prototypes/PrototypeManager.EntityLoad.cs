using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Robust.Shared.EntitySerialization;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager;

namespace Robust.Shared.Prototypes;

public abstract partial class PrototypeManager
{
    private FrozenDictionary<string, PrototypeComponentRegistration[]> _entityComponentRegistrations
        = FrozenDictionary<string, PrototypeComponentRegistration[]>.Empty;

    void IPrototypeManagerInternal.LoadEntity(Entity<MetaDataComponent> ent, IEntityLoadContext? context)
    {
        var (entity, meta) = ent;
        var prototype = meta.EntityPrototype;
        var ctx = context as ISerializationContext;

        if (prototype != null && context == null)
        {
            LoadPrototypeComponents(entity, meta, prototype);
            return;
        }

        if (prototype != null && context != null)
            LoadPrototypeComponentsWithContext(entity, meta, prototype, context, ctx);

        if (context != null)
            LoadExtraContextComponents(entity, prototype, context, ctx);
    }

    private PrototypeComponentRegistration[] GetComponentRegistrations(EntityPrototype prototype)
    {
        if (_entityComponentRegistrations.TryGetValue(prototype.ID, out var cached))
            return cached;

        // This should only happen if an entity is being loaded before prototype resolution has rebuilt the cache.
        return BuildComponentRegistrations(prototype);
    }

    private void RebuildEntityComponentRegistrationCache()
    {
        if (!TryGetInstances<EntityPrototype>(out var prototypes))
        {
            ClearEntityComponentRegistrationCache();
            return;
        }

        var registrations = new Dictionary<string, PrototypeComponentRegistration[]>(prototypes.Count);
        foreach (var (id, prototype) in prototypes)
        {
            registrations[id] = BuildComponentRegistrations(prototype);
        }

        _entityComponentRegistrations = registrations.ToFrozenDictionary();
    }

    private void ClearEntityComponentRegistrationCache()
    {
        _entityComponentRegistrations = FrozenDictionary<string, PrototypeComponentRegistration[]>.Empty;
    }

    private PrototypeComponentRegistration[] BuildComponentRegistrations(EntityPrototype prototype)
    {
        var registrations = new PrototypeComponentRegistration[prototype.Components.Count];
        var i = 0;

        foreach (var (name, entry) in prototype.Components)
        {
            registrations[i++] = new PrototypeComponentRegistration(name, entry, _factory.GetRegistration(name));
        }

        return registrations;
    }

    private void LoadPrototypeComponents(
        EntityUid entity,
        MetaDataComponent meta,
        EntityPrototype prototype)
    {
        foreach (var protoComponent in GetComponentRegistrations(prototype))
        {
            var component = EnsureCompExistsAndDeserialize(
                entity,
                protoComponent.Registration,
                protoComponent.Name,
                protoComponent.Entry.Component,
                null);

            RemoveUnsyncedNetComponent(meta, protoComponent.Entry.Component, protoComponent.Registration);
            _entMan.ClearPrototypeLoadTicks(component, protoComponent.Registration);
        }
    }

    private void LoadPrototypeComponentsWithContext(
        EntityUid entity,
        MetaDataComponent meta,
        EntityPrototype prototype,
        IEntityLoadContext context,
        ISerializationContext? serializationContext)
    {
        foreach (var protoComponent in GetComponentRegistrations(prototype))
        {
            var name = protoComponent.Name;
            var entry = protoComponent.Entry;

            if (context.ShouldSkipComponent(name))
                continue;

            var contextComponent = false;
            var fullData = entry.Component;
            if (context.TryGetComponent(name, out var data))
            {
                contextComponent = true;
                fullData = data;
            }

            var component = EnsureCompExistsAndDeserialize(
                entity,
                protoComponent.Registration,
                name,
                fullData,
                serializationContext);

            RemoveUnsyncedNetComponent(meta, entry.Component, protoComponent.Registration);

            if (!contextComponent)
                _entMan.ClearPrototypeLoadTicks(component, protoComponent.Registration);
        }
    }

    private void LoadExtraContextComponents(
        EntityUid entity,
        EntityPrototype? prototype,
        IEntityLoadContext context,
        ISerializationContext? serializationContext)
    {
        foreach (var name in context.GetExtraComponentTypes())
        {
            if (prototype != null && prototype.Components.ContainsKey(name))
            {
                // This component also exists in the prototype.
                // This means that the previous step already caught both the prototype data AND map data.
                // Meaning that re-running EnsureCompExistsAndDeserialize would wipe prototype data.
                continue;
            }

            if (!context.TryGetComponent(name, out var data))
            {
                throw new InvalidOperationException(
                    $"{nameof(IEntityLoadContext)} provided component name {name} but refused to provide data");
            }

            var compReg = _factory.GetRegistration(name);
            EnsureCompExistsAndDeserialize(entity, compReg, name, data, serializationContext);
        }
    }

    private static void RemoveUnsyncedNetComponent(
        MetaDataComponent meta,
        IComponent component,
        ComponentRegistration compReg)
    {
        if (!component.NetSyncEnabled && compReg.NetID is { } netId)
            meta.NetComponents.Remove(netId);
    }

    private IComponent EnsureCompExistsAndDeserialize(
        EntityUid entity,
        ComponentRegistration compReg,
        string compName,
        IComponent data,
        ISerializationContext? context)
    {
        var existed = true;
        if (!_entMan.TryGetComponent(entity, compReg.Idx, out var component))
        {
            existed = false;
            var newComponent = _factory.GetComponent(compReg);
            newComponent.Owner = entity;
            component = newComponent;
        }

        if (context is not EntityDeserializer map)
        {
            _serializationManager.CopyTo(data, ref component, context, notNullableOverride: true);
        }
        else
        {
            map.CurrentComponent = compName;
            _serializationManager.CopyTo(data, ref component, context, notNullableOverride: true);
            map.CurrentComponent = null;
        }

        if (!existed)
            _entMan.AddComponent(entity, component);

        return component;
    }

    private readonly record struct PrototypeComponentRegistration(
        string Name,
        EntityPrototype.ComponentRegistryEntry Entry,
        ComponentRegistration Registration);
}
