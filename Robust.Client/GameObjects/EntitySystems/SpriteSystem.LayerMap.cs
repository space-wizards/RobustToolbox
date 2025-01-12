using System;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Robust.Client.GameObjects;

// This partial class contains various public methods for manipulating layer mappings.
public sealed partial class SpriteSystem
{
    /// <summary>
    /// Map an enum to a layer index.
    /// </summary>
    public void LayerMapSet(Entity<SpriteComponent?> sprite, Enum key, int index)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (index < 0 || index >= sprite.Comp.Layers.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        sprite.Comp.LayerMap[key] = index;
    }

    /// <summary>
    /// Map string to a layer index. If possible, it is preferred to use an enum key.
    /// string keys mainly exist to make it easier to define custom layer keys in yaml.
    /// </summary>
    public void LayerMapSet(Entity<SpriteComponent?> sprite, string key, int index)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (index < 0 || index >= sprite.Comp.Layers.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        sprite.Comp.LayerMap[key] = index;
    }

    /// <summary>
    /// Map an enum to a layer index.
    /// </summary>
    public void LayerMapAdd(Entity<SpriteComponent?> sprite, Enum key, int index)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (index < 0 || index >= sprite.Comp.Layers.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        sprite.Comp.LayerMap.Add(key, index);
    }

    /// <summary>
    /// Map a string to a layer index. If possible, it is preferred to use an enum key.
    /// string keys mainly exist to make it easier to define custom layer keys in yaml.
    /// </summary>
    public void LayerMapAdd(Entity<SpriteComponent?> sprite, string key, int index)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (index < 0 || index >= sprite.Comp.Layers.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        sprite.Comp.LayerMap.Add(key, index);
    }

    /// <summary>
    /// Remove an enum mapping.
    /// </summary>
    public bool LayerMapRemove(Entity<SpriteComponent?> sprite, Enum key)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return false;

        return sprite.Comp.LayerMap.Remove(key);
    }

    /// <summary>
    /// Remove a string mapping.
    /// </summary>
    public bool LayerMapRemove(Entity<SpriteComponent?> sprite, string key)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return false;

        return sprite.Comp.LayerMap.Remove(key);
    }

    /// <summary>
    /// Remove an enum mapping.
    /// </summary>
    public bool LayerMapRemove(Entity<SpriteComponent?> sprite, Enum key, out int index)
    {
        if (_query.Resolve(sprite.Owner, ref sprite.Comp))
            return sprite.Comp.LayerMap.Remove(key, out index);

        index = 0;
        return false;
    }

    /// <summary>
    /// Remove a string mapping.
    /// </summary>
    public bool LayerMapRemove(Entity<SpriteComponent?> sprite, string key, out int index)
    {
        if (_query.Resolve(sprite.Owner, ref sprite.Comp))
            return sprite.Comp.LayerMap.Remove(key, out index);

        index = 0;
        return false;
    }

    /// <summary>
    /// Attempt to resolve an enum mapping.
    /// </summary>
    public bool LayerMapTryGet(Entity<SpriteComponent?> sprite, Enum key, out int index, bool logMissing)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp, logMissing))
        {
            index = 0;
            return false;
        }

        if (sprite.Comp.LayerMap.TryGetValue(key, out index))
            return true;

        if (logMissing)
            Log.Error($"Layer with key '{key}' does not exist on entity {ToPrettyString(sprite)}! Trace:\n{Environment.StackTrace}");

        return false;
    }

    /// <summary>
    /// Attempt to resolve a string mapping.
    /// </summary>
    public bool LayerMapTryGet(Entity<SpriteComponent?> sprite, string key, out int index, bool logMissing)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp, logMissing))
        {
            index = 0;
            return false;
        }

        if (sprite.Comp.LayerMap.TryGetValue(key, out index))
            return true;

        if (logMissing)
            Log.Error($"Layer with key '{key}' does not exist on entity {ToPrettyString(sprite)}! Trace:\n{Environment.StackTrace}");

        return false;
    }

    public int LayerMapGet(Entity<SpriteComponent?> sprite, Enum key)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return -1;

        return sprite.Comp.LayerMap[key];
    }

    public int LayerMapGet(Entity<SpriteComponent?> sprite, string key)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return -1;

        return sprite.Comp.LayerMap[key];
    }

    public bool LayerExists(Entity<SpriteComponent?> sprite, string key)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return false;

        return sprite.Comp.LayerMap.TryGetValue(key, out var index)
               && LayerExists(sprite, index);
    }

    public bool LayerExists(Entity<SpriteComponent?> sprite, Enum key)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return false;

        return sprite.Comp.LayerMap.TryGetValue(key, out var index)
               && LayerExists(sprite, index);
    }

    /// <summary>
    /// Create a new blank layer and map the given key to it.
    /// </summary>
    public int LayerMapReserve(Entity<SpriteComponent?> sprite, Enum key)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return -1;

        if (LayerExists(sprite, key))
            throw new Exception("Layer already exists");

        var index = AddBlankLayer(sprite);
        LayerMapSet(sprite, key, index);
        return index;
    }

    /// <summary>
    /// A create a new blank layer and map the given key to it. If possible, it is preferred to use an enum key.
    /// string keys mainly exist to make it easier to define custom layer keys in yaml.
    /// </summary>
    public int LayerMapReserve(Entity<SpriteComponent?> sprite, string key)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return -1;

        if (LayerExists(sprite, key))
            throw new Exception("Layer already exists");

        var index = AddBlankLayer(sprite);
        LayerMapSet(sprite, key, index);
        return index;
    }

    public bool RemoveLayer(Entity<SpriteComponent?> sprite, string key, bool logMissing = true)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp, logMissing))
            return false;

        if (!LayerMapTryGet(sprite, key, out var index, logMissing))
            return false;

        return RemoveLayer(sprite, index, logMissing);
    }

    public bool RemoveLayer(Entity<SpriteComponent?> sprite, Enum key, bool logMissing = true)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp, logMissing))
            return false;

        if (!LayerMapTryGet(sprite, key, out var index, logMissing))
            return false;

        return RemoveLayer(sprite, index, logMissing);
    }

    public bool RemoveLayer(
        Entity<SpriteComponent?> sprite,
        string key,
        [NotNullWhen(true)] out Layer? layer,
        bool logMissing = true)
    {
        layer = null;
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp, logMissing))
            return false;

        if (!LayerMapTryGet(sprite, key, out var index, logMissing))
            return false;

        return RemoveLayer(sprite, index, out layer, logMissing);
    }

    public bool RemoveLayer(
        Entity<SpriteComponent?> sprite,
        Enum key,
        [NotNullWhen(true)] out Layer? layer,
        bool logMissing = true)
    {
        layer = null;
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp, logMissing))
            return false;

        if (!LayerMapTryGet(sprite, key, out var index, logMissing))
            return false;

        return RemoveLayer(sprite, index, out layer, logMissing);
    }
}
