using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;

namespace Robust.Shared.GameObjects;

public abstract partial class EntityManager
{
    public bool MatchesFilter(EntityUid ent, ComponentFilter filter)
    {
        foreach (var comp in filter)
        {
            if (!HasComponent(ent, comp))
                return false;
        }

        return true;
    }

    public bool ExactlyMatchesFilter(EntityUid ent, ComponentFilter filter)
    {
        foreach (var comp in _entCompIndex[ent])
        {
            if (comp.Deleted)
                continue;

            if (!filter.Contains(comp.GetType()))
                return false;
        }

        return true;
    }

    public IEnumerable<Type> EnumerateFilterMisses(EntityUid ent, ComponentFilter filter)
    {
        foreach (var comp in filter)
        {
            if (!HasComponent(ent, comp))
                yield return comp;
        }
    }

    public IEnumerable<Type> EnumerateEntityMisses(EntityUid ent, ComponentFilter filter)
    {
        foreach (var comp in _entCompIndex[ent])
        {
            if (comp.Deleted)
                continue;

            var ty = comp.GetType();

            if (!filter.Contains(ty))
                yield return ty;
        }
    }

    public IEnumerable<Type> EnumerateFilterHits(EntityUid ent, ComponentFilter filter)
    {
        foreach (var comp in filter)
        {
            if (HasComponent(ent, comp))
                yield return comp;
        }
    }

    public void FillMissesFromRegistry(EntityUid ent, ComponentFilter filter, ComponentRegistry registry)
    {
        var meta = MetaQuery.GetComponent(ent);

        foreach (var comp in filter)
        {
            if (!HasComponent(ent, comp))
            {
                if (!registry.TryGetComponent(_componentFactory, comp, out var toClone))
                {
                    throw new InvalidOperationException(
                        $"Tried to fill in a missing component, {comp}, from a registry, but couldn't find it.");
                }

                AddComponent(ent, new EntityPrototype.ComponentRegistryEntry(toClone), false, meta);
            }
        }
    }

    public void FillMissesWithNewComponents(EntityUid ent, ComponentFilter filter)
    {
        var meta = MetaQuery.GetComponent(ent);

        foreach (var comp in filter)
        {
            if (!HasComponent(ent, comp))
            {
                AddComponent(ent, _componentFactory.GetComponent(comp), false, meta);
            }
        }
    }

    public ComponentFilterQuery ConstructFilterQuery(ComponentFilter filter)
    {
        var tailCount = filter.Count - 1;
        var tails = new Dictionary<EntityUid, IComponent>[tailCount];

        Dictionary<EntityUid, IComponent>? lead = null;
        var tailIdx = 0;

        foreach (var entry in filter)
        {
            if (lead is null)
                lead = _entTraitDict[entry];
            else
            {
                tails[tailIdx++] = _entTraitDict[entry];
            }
        }

        return new ComponentFilterQuery(lead!, tails);
    }
}
