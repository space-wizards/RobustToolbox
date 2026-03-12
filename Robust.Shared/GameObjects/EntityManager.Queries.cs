using System;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public partial class EntityManager
{
    public EntityQuery<TComp1> GetEntityQuery<TComp1>()
        where TComp1 : IComponent
    {
        DebugTools.Assert(_entTraitArray.Length > CompIdx.ArrayIndex<TComp1>(),
            $"Unknown component: {typeof(TComp1).Name}");
        var comps = _entTraitArray[CompIdx.ArrayIndex<TComp1>()];
        var meta = _entTraitArray[CompIdx.ArrayIndex<MetaDataComponent>()];

        return new EntityQuery<TComp1>(this, comps, meta);
    }

    public EntityQuery<IComponent> GetEntityQuery(Type type)
    {
        DebugTools.Assert(_entTraitDict.ContainsKey(type), $"Unknown component: {type.Name}");
        var comps = _entTraitDict[type];
        var meta = _entTraitArray[CompIdx.ArrayIndex<MetaDataComponent>()];

        return new EntityQuery<IComponent>(this, comps, meta);
    }

    public DynamicEntityQuery GetDynamicQuery(params (Type, DynamicEntityQuery.QueryFlags)[] userEntries)
    {
        var entries = new DynamicEntityQuery.QueryEntry[userEntries.Length];

        for (var i = 0; i < userEntries.Length; i++)
        {
            var entry = userEntries[i];
            DebugTools.Assert(_entTraitDict.ContainsKey(entry.Item1), $"Unknown component: {entry.Item1.Name}");
            entries[i] = new(_entTraitDict[entry.Item1], entry.Item2);
        }

        return new DynamicEntityQuery(entries, _entTraitArray[CompIdx.ArrayIndex<MetaDataComponent>()]);
    }

    public EntityQuery<TComp1, TComp2> GetEntityQuery<TComp1, TComp2>()
        where TComp1 : IComponent
        where TComp2 : IComponent
    {
        var dyQuery = GetDynamicQuery((typeof(TComp1), DynamicEntityQuery.QueryFlags.None),
            (typeof(TComp2), DynamicEntityQuery.QueryFlags.None));

        return new(dyQuery, this);
    }

    public EntityQuery<TComp1, TComp2, TComp3> GetEntityQuery<TComp1, TComp2, TComp3>()
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent
    {
        var dyQuery = GetDynamicQuery((typeof(TComp1), DynamicEntityQuery.QueryFlags.None),
            (typeof(TComp2), DynamicEntityQuery.QueryFlags.None),
            (typeof(TComp3), DynamicEntityQuery.QueryFlags.None));

        return new(dyQuery, this);
    }

    public EntityQuery<TComp1, TComp2, TComp3, TComp4> GetEntityQuery<TComp1, TComp2, TComp3, TComp4>()
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent
        where TComp4 : IComponent
    {
        var dyQuery = GetDynamicQuery((typeof(TComp1), DynamicEntityQuery.QueryFlags.None),
            (typeof(TComp2), DynamicEntityQuery.QueryFlags.None),
            (typeof(TComp3), DynamicEntityQuery.QueryFlags.None),
            (typeof(TComp4), DynamicEntityQuery.QueryFlags.None));

        return new(dyQuery, this);
    }
}
