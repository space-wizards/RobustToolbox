using System;
using System.Collections.Generic;
using System.Diagnostics;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Core.Utils;
using Collections.Pooled;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public partial class EntityManager
{
    private World _world = default!;

    private static readonly ComponentType[] DefaultArchetype = new ComponentType[]
    {
        typeof(MetaDataComponent),
        typeof(TransformComponent),
    };

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
        var reference = _world.Reference(uid);

        if (!_world.IsAlive(reference))
        {
            throw new InvalidOperationException("Tried to delete an invalid entity reference");
        }

        _world.Destroy(reference);
    }

    private void SpawnEntityArch(out EntityUid entity)
    {
        var archEnt = _world.Create(DefaultArchetype);
        var reference = _world.Reference(archEnt);
        entity = new EntityUid(reference);
    }

    public void CleanupArch()
    {
        var sw = new Stopwatch();
        sw.Start();
        var arc = _world.Archetypes.Count;
        // _world.TrimExcess();
        arc -= _world.Archetypes.Count;
        sw.Stop();
        _sawmill.Debug($"Trimming {arc} archetypes took {sw.Elapsed.TotalMilliseconds} milliseconds");
    }

    internal ComponentType[] GetComponentType(EntityPrototype prototype, ICollection<Type>? added = null, ICollection<Type>? missing = null)
    {
        var compTypes = new ComponentType[prototype.Components.Count + (added?.Count ?? 0) - (missing?.Count ?? 0)];
        var idx = 0;

        foreach (var comp in prototype.Components.Values)
        {
            var componentType = comp.Component.GetType();
            if (missing?.Contains(componentType) == true || added?.Contains(componentType) == true)
                continue;

            compTypes[idx++] = componentType;
        }

        if (added != null)
        {
            foreach (var componentType in added)
            {
                if (missing?.Contains(componentType) == true)
                    continue;

                compTypes[idx++] = componentType;
            }
        }

        return compTypes;
    }

    /// <summary>
    /// Reserves additional slots for the specified ComponentTypes.
    /// </summary>
    internal void Reserve(ComponentType[] compTypes, int count)
    {
        _world.Reserve(compTypes, count);
    }

    /// <summary>
    /// WARNING: DO NOT CALL THIS UNLESS YOU KNOW WHAT YOU ARE DOING.
    /// Adds the component types to the entity, shuffling its archetype.
    /// </summary>
    internal void AddComponentRange(EntityUid uid, PooledList<ComponentType> compTypes)
    {
        DebugTools.Assert(compTypes.Count > 0);
        _world.AddRange(uid, compTypes.Span);
    }

    internal void RemoveComponentRange(EntityUid uid, PooledList<ComponentType> compTypes)
    {
        DebugTools.Assert(compTypes.Count > 0);
        _world.RemoveRange(uid, compTypes.Span);
    }

    public World GetWorld() => _world;
}
