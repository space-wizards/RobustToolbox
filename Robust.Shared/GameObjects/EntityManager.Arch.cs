using System;
using System.Collections.Generic;
using System.Diagnostics;
using Arch.Core;
using Arch.Core.Utils;
using Robust.Shared.Prototypes;

namespace Robust.Shared.GameObjects;

public partial class EntityManager
{
    private World _world = default!;
    protected ComponentType[] DefaultArchetype = { typeof(MetaDataComponent) };

    protected void InitializeArch()
    {
        _world = World.Create();
    }

    protected void ShutdownArch()
    {
        World.Destroy(_world);
    }

    protected void DestroyArch(EntityUid uid)
    {
        var archEnt = (Entity) uid;
        var reference = _world.Reference(archEnt);

        if (reference.Version != (uid.Version - EntityUid.ArchVersionOffset))
        {
            throw new InvalidOperationException($"Tried to delete a different matching entity for Arch.");
        }

        _world.Destroy(archEnt);
    }

    private void SpawnEntityArch(out EntityUid entity)
    {
        var archEnt = _world.Create();
        var reference = _world.Reference(archEnt);
        entity = new EntityUid(reference);
    }

    internal void CleanupArch()
    {
        var sw = new Stopwatch();
        sw.Start();
        _world.TrimExcess();
        sw.Stop();
        _sawmill.Debug($"Trimming archetypes took {sw.Elapsed.TotalMilliseconds} milliseconds");
    }

    internal ComponentType[] GetComponentType(EntityPrototype prototype, ICollection<Type>? missing = null)
    {
        var compTypes = new ComponentType[prototype.Components.Count - missing?.Count ?? 0];
        var idx = 0;

        foreach (var comp in prototype.Components.Values)
        {
            var componentType = comp.Component.GetType();
            if (missing?.Contains(componentType) == true)
                continue;

            compTypes[idx++] = componentType;
        }

        return compTypes;
    }
}
