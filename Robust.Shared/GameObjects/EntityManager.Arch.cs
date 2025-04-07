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
    /*
     * Okay this is just a thoughts dump where you may be asking "why blank"
     * - We do null checks for components because bulk add / remove changes leave null slots on entities where we may be running other operations in the meantime.
     * - Our invalid EntityUid is different to arch's because we treat "default" (i.e. uid of 0,0) as the bad one so we can easily know if an entity is bad.
     */

    internal World _world = default!;

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
        _world.Destroy(uid);
    }

    private void SpawnEntityArch(out EntityUid entity)
    {
        var archEnt = _world.Create(DefaultArchetype);
        entity = new EntityUid(archEnt);
    }

    public void CleanupArch()
    {
        var sw = new Stopwatch();
        sw.Start();
        var arc = _world.Archetypes.Count;
        _world.TrimExcess();
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
