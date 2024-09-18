using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Robust.Shared.Prototypes;

// This partial class handles entity prototype categories
public abstract partial class PrototypeManager : IPrototypeManagerInternal
{
    /// <summary>
    /// Cached array of components with the <see cref="EntityCategoryAttribute"/>
    /// </summary>
    private (string, EntityCategoryAttribute)[]? _autoComps;

    public FrozenDictionary<ProtoId<EntityCategoryPrototype>, IReadOnlyList<EntityPrototype>> Categories { get; private set; }
        = FrozenDictionary<ProtoId<EntityCategoryPrototype>, IReadOnlyList<EntityPrototype>>.Empty;

    private void UpdateCategories()
    {
        // Update automatically categorized prototypes
        var autoCategories = GetAutomaticCategories();

        var entityCount = Count<EntityPrototype>();
        var cache = new Dictionary<EntProtoId, IReadOnlySet<EntityCategoryPrototype>>(entityCount);

        var categoryCount = Count<EntityCategoryPrototype>();
        var categories = new Dictionary<ProtoId<EntityCategoryPrototype>, List<EntityPrototype>>(categoryCount);

        foreach (var proto in EnumeratePrototypes<EntityPrototype>())
        {
            UpdateCategories(proto, cache, autoCategories, categories);
        }

        // Ensure all categories have an entry in the dictionary, even if it is empty.
        foreach (var category in EnumeratePrototypes<EntityCategoryPrototype>())
        {
            categories.GetOrNew(category.ID);
        }

        DebugTools.Assert(categories.Values.All(x => x.ToHashSet().Count == x.Count));
        Categories = categories.ToFrozenDictionary(x => x.Key, x => (IReadOnlyList<EntityPrototype>)x.Value);
    }

    private Dictionary<string, HashSet<EntityCategoryPrototype>> GetAutomaticCategories()
    {
        var dict = new Dictionary<string, HashSet<EntityCategoryPrototype>>();
        foreach (var category in EnumeratePrototypes<EntityCategoryPrototype>())
        {
            if (category.Components == null)
                continue;

            foreach (var comp in category.Components)
            {
                dict.GetOrNew(comp).Add(category);
            }
        }

        _autoComps ??= _factory.GetAllRegistrations()
            .Where(x => x.Type.HasCustomAttribute<EntityCategoryAttribute>())
            .Select(x => (x.Name, x.Type.GetCustomAttribute<EntityCategoryAttribute>()!))
            .ToArray();

        foreach (var (name, attr) in _autoComps)
        {
            foreach (var categoryId in attr.Categories)
            {
                if (TryIndex(categoryId, out EntityCategoryPrototype? category))
                    dict.GetOrNew(name).Add(category);
                else
                    Sawmill.Error($"Component {name} has invalid {nameof(EntityCategoryAttribute)} argument: {categoryId}");
            }
        }

        return dict;
    }

    private IReadOnlySet<EntityCategoryPrototype> UpdateCategories(EntProtoId id,
        Dictionary<EntProtoId, IReadOnlySet<EntityCategoryPrototype>> cache,
        Dictionary<string, HashSet<EntityCategoryPrototype>> autoCategories,
        Dictionary<ProtoId<EntityCategoryPrototype>, List<EntityPrototype>> categories)
    {
        if (cache.TryGetValue(id, out var existing))
            return existing;  // Already Updated

        var set = new HashSet<EntityCategoryPrototype>();

        // Get explicitly specified categories
        if (!TryGetMapping(typeof(EntityPrototype), id, out var mapping))
            throw new UnknownPrototypeException(id, typeof(EntityPrototype));

        // Have to rely on the mapping instead of the instance's data-field to support categories being added
        // to abstract prototypes
        if (mapping.TryGet("categories", out SequenceDataNode? node))
        {
            foreach (var dataNode in node)
            {
                var categoryId = ((ValueDataNode) dataNode).Value;
                if (TryIndex(categoryId, out EntityCategoryPrototype? categoryInstance))
                    set.Add(categoryInstance);
                else
                    Sawmill.Error($"Entity prototype {id} specifies an invalid {nameof(EntityCategoryPrototype)}: {categoryId}");
            }
        }

        DebugTools.Assert(!TryIndex(id, out var instance, false)
                          || instance.CategoriesInternal == null
                          || instance.CategoriesInternal.All(x =>
                              set.Any(y => y.ID == x)));

        // Get inherited categories
        foreach (var (parentId, _) in EnumerateAllParents<EntityPrototype>(id))
        {
            var parentCategories = UpdateCategories(parentId, cache, autoCategories, categories);
            foreach (var category in parentCategories)
            {
                if (category.Inheritable)
                    set.Add(category);
            }
        }

        if (!TryIndex(id, out var protoInstance, false))
        {
            // Prototype is abstract
            cache.Add(id, set);
            return set;
        }

        // Get automated categories inferred from components
        foreach (var comp in protoInstance.Components.Keys)
        {
            if (autoCategories.TryGetValue(comp, out var autoCats))
                set.UnionWith(autoCats);
        }

        cache.Add(id, set);
        protoInstance.Categories = set;

        foreach (var category in set)
        {
            if (category.HideSpawnMenu)
                protoInstance.HideSpawnMenu = true;

            categories.GetOrNew(category).Add(protoInstance);
        }

        return set;
    }
}
