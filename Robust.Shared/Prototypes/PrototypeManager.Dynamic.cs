using Robust.Shared.Serialization.Markdown.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Prototypes;

public partial class PrototypeManager
{

    /// <inheritdoc/>
    public bool TryLoadDynamic(MappingDataNode data)
    {
        ExtractedMappingData? extracted;
        try
        {
            extracted = ExtractMapping(data);
        }
        catch (Exception e)
        {
            Sawmill.Error($"Exception while attempting to dynamically create prototype:\n{e}");
            return false;
        }
        if (extracted is null)
        {
            Sawmill.Error("Failed to dynamically create prototype, ExtractMapping returned null.");
            return false;
        }
        var changed = new Dictionary<Type, HashSet<string>>();
        try
        {
            MergeMapping(extracted, false, changed);
        }
        catch (Exception e)
        {
            Sawmill.Error($"Exception while attempting to dynamically create prototype:\n{e}");
            return false;
        }
        ReloadPrototypes(changed);
        _locMan.ReloadLocalizations();
        return true;
    }

    /// <inheritdoc/>
    public bool TryDelete<T>([ForbidLiteral] string id) where T : class, IPrototype
    {
        Type? kind = default!;
        if (!_kindNameCache.TryGetValue(typeof(T), out var kindname))
        {
            var attribute = (PrototypeAttribute?)Attribute.GetCustomAttribute(typeof(T), typeof(PrototypeAttribute));
            if (attribute is null)
                return false;
            kindname = attribute.Type ?? CalculatePrototypeName(typeof(T));
            var dict = _kindNameCache.ToDictionary();
            dict[typeof(T)] = kindname;
            FreezeNames(dict);
        }
        try
        {
            if (HasMapping<T>(id))
            {
                if (!_kindNames.TryGetValue(kindname, out kind))
                    return false;
            }
        }
        catch (Exception e)
        {
            Sawmill.Error($"Exception while trying to delete prototype:\n{e}");
            return false;
        }
        if (kind is null)
            return false;
        var kindData = _kinds[kind];
        if (kindData.Inheritance is { } tree)
            tree.Remove(id, true);
        var modified = new HashSet<KindData>();
        kindData.UnfrozenInstances ??= kindData.Instances.ToDictionary();
        kindData.UnfrozenInstances.Remove(id);
        kindData.Results.Remove(id);
        kindData.RawResults.Remove(id);
        modified.Add(kindData);
        Freeze(modified);
        return true;
    }
}
