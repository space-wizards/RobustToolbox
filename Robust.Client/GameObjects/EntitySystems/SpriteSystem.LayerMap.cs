using System;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.Sprite;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Robust.Client.GameObjects;

// This partial class contains various public methods for manipulating layer mappings.
public sealed partial class SpriteSystem
{
    /// <summary>
    /// Map a layer key to a layer index.
    /// </summary>
    public void LayerMapSet(Entity<SpriteComponent?> sprite, LayerKey key, int index)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (index < 0 || index >= sprite.Comp.Layers.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        sprite.Comp.LayerMap[key] = index;
    }

    /// <summary>
    /// Map a layer key to a layer index.
    /// </summary>
    public void LayerMapAdd(Entity<SpriteComponent?> sprite, LayerKey key, int index)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (index < 0 || index >= sprite.Comp.Layers.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        sprite.Comp.LayerMap.Add(key, index);
    }

    /// <summary>
    /// Remove a layer key mapping.
    /// </summary>
    public bool LayerMapRemove(Entity<SpriteComponent?> sprite, LayerKey key)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return false;

        return sprite.Comp.LayerMap.Remove(key);
    }

    /// <summary>
    /// Remove a layer key mapping.
    /// </summary>
    public bool LayerMapRemove(Entity<SpriteComponent?> sprite, LayerKey key, out int index)
    {
        if (_query.Resolve(sprite.Owner, ref sprite.Comp))
            return sprite.Comp.LayerMap.Remove(key, out index);

        index = 0;
        return false;
    }

    /// <summary>
    /// Attempt to resolve a layer key mapping.
    /// </summary>
    [Obsolete("Use LayerMapResolve or the override without a bool argument")]
    public bool LayerMapTryGet(Entity<SpriteComponent?> sprite, LayerKey key, out int index, bool logMissing)
    {
        return logMissing
            ? LayerMapResolve(sprite, key, out index)
            : LayerMapTryGet(sprite, key, out index);
    }

    /// <summary>
    /// Attempt to get the layer index corresponding to the given key.
    /// </summary>
    public bool LayerMapTryGet(Entity<SpriteComponent?> sprite, LayerKey key, out int index)
    {
        if (_query.Resolve(sprite.Owner, ref sprite.Comp))
            return sprite.Comp.LayerMap.TryGetValue(key, out index);

        index = 0;
        return false;
    }

    /// <summary>
    /// Attempt to resolve the layer index corresponding to the given key. This will log an error if there is no layer
    /// with the given key
    /// </summary>
    public bool LayerMapResolve(Entity<SpriteComponent?> sprite, LayerKey key, out int index)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
        {
            index = 0;
            return false;
        }

        if (sprite.Comp.LayerMap.TryGetValue(key, out index))
            return true;

        Log.Error($"Layer with key '{key}' does not exist on entity {ToPrettyString(sprite)}! Trace:\n{Environment.StackTrace}");
        return false;
    }

    /// <summary>
    /// Attempt to resolve layer key.
    /// </summary>
    [Obsolete("Use ResolveLayer or the override without a bool argument")]
    public bool TryGetLayer(Entity<SpriteComponent?> sprite, LayerKey key, [NotNullWhen(true)] out Layer? layer, bool logMissing)
    {
        layer = null;
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp, logMissing))
            return false;

        return LayerMapTryGet(sprite, key, out var index, logMissing)
               && TryGetLayer(sprite, index, out layer, logMissing);
    }

    /// <summary>
    /// Attempt to get the layer corresponding to the given key.
    /// </summary>
    public bool TryGetLayer(Entity<SpriteComponent?> sprite, LayerKey key, [NotNullWhen(true)] out Layer? layer)
    {
        layer = null;
        return _query.Resolve(sprite.Owner, ref sprite.Comp)
               && LayerMapTryGet(sprite, key, out var index)
               && TryGetLayer(sprite, index, out layer);
    }

    /// <summary>
    /// Attempt to resolve the layer corresponding to the given key key. This will log an error if there is no layer
    /// with the given key
    /// </summary>
    public bool ResolveLayer(Entity<SpriteComponent?> sprite, LayerKey key, [NotNullWhen(true)] out Layer? layer)
    {
        layer = null;
        return _query.Resolve(sprite.Owner, ref sprite.Comp)
               && LayerMapResolve(sprite, key, out var index)
               && ResolveLayer(sprite, index, out layer);
    }

    public int LayerMapGet(Entity<SpriteComponent?> sprite, LayerKey key)
    {
        return !_query.Resolve(sprite.Owner, ref sprite.Comp) ? -1 : sprite.Comp.LayerMap[key];
    }

    public bool LayerExists(Entity<SpriteComponent?> sprite, LayerKey key)
    {
        return _query.Resolve(sprite.Owner, ref sprite.Comp)
               && sprite.Comp.LayerMap.TryGetValue(key, out var index)
               && LayerExists(sprite, index);
    }

    /// <summary>
    /// Ensures that a layer with the given key exists and return the layer's index.
    /// If the layer does not yet exist, this will create and add a blank layer.
    /// </summary>
    public int LayerMapReserve(Entity<SpriteComponent?> sprite, LayerKey key)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return -1;

        if (LayerMapTryGet(sprite, key, out var layerIndex))
            return layerIndex;

        var layer = AddBlankLayer(sprite!);
        LayerMapSet(sprite, key, layer.Index);
        return layer.Index;
    }

    public bool RemoveLayer(Entity<SpriteComponent?> sprite, LayerKey key, bool logMissing = true)
        => RemoveLayer(sprite, key, out _, logMissing);

    public bool RemoveLayer(
        Entity<SpriteComponent?> sprite,
        LayerKey key,
        [NotNullWhen(true)] out Layer? layer,
        bool logMissing = true)
    {
        layer = null;
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp, logMissing))
            return false;

        int index;
        if (logMissing)
        {
            if (!LayerMapResolve(sprite, key, out index))
                return false;
        }
        else
        {
            if (!LayerMapTryGet(sprite, key, out index))
                return false;
        }

        return RemoveLayer(sprite, index, out layer, logMissing);
    }
}
