using System;
using System.Diagnostics.CodeAnalysis;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Robust.Client.GameObjects;

// This partial class contains various public methods for managing a sprite's layers.
// This setter methods for modifying a layer's properties are in a separate file.
public sealed partial class SpriteSystem
{
    public bool LayerExists(Entity<SpriteComponent?> sprite, int index)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return false;

        return index > 0 && index < sprite.Comp.Layers.Count;
    }

    public bool TryGetLayer(
        Entity<SpriteComponent?> sprite,
        int index,
        [NotNullWhen(true)] out Layer? layer,
        bool logMissing)
    {
        layer = null;

        if (!_query.Resolve(sprite.Owner, ref sprite.Comp, logMissing))
            return false;

        if (index >= 0 && index < sprite.Comp.Layers.Count)
        {
            layer = sprite.Comp.Layers[index];
            DebugTools.AssertEqual(layer.Owner, sprite!);
            DebugTools.AssertEqual(layer.Index, index);
            return true;
        }

        if (logMissing)
            Log.Error($"Layer index '{index}' on entity {ToPrettyString(sprite)} does not exist! Trace:\n{Environment.StackTrace}");

        return false;
    }

    public bool RemoveLayer(Entity<SpriteComponent?> sprite, int index, bool logMissing = true)
    {
        return RemoveLayer(sprite.Owner, index, out _, logMissing);
    }

    public bool RemoveLayer(
        Entity<SpriteComponent?> sprite,
        int index,
        [NotNullWhen(true)] out Layer? layer,
        bool logMissing = true)
    {
        layer = null;
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp, logMissing))
            return false;

        if (!TryGetLayer(sprite, index, out layer, logMissing))
            return false;

        sprite.Comp.Layers.RemoveAt(index);

        foreach (var otherLayer in sprite.Comp.Layers)
        {
            if (index < otherLayer.Index)
                otherLayer.Index--;

            // Reverse for loop to allow element removal
            for (var i = otherLayer.ChildLayers.Count - 1; i >= 0; i--)
            {
                if (index == otherLayer.ChildLayers[i])
                {
                    otherLayer.ChildLayers.RemoveAt(i);
                    continue;
                }

                if (index < otherLayer.ChildLayers[i])
                    otherLayer.ChildLayers[i]--;
            }

            if (index == otherLayer.ParentLayer)
                otherLayer.ParentLayer = null;
            else if (index < otherLayer.ParentLayer)
                otherLayer.ParentLayer--;
        }

        // TODO SPRITE track inverse-mapping?
        foreach (var (key, value) in sprite.Comp.LayerMap)
        {
            if (value == index)
                sprite.Comp.LayerMap.Remove(key);
            else if (value > index)
            {
                sprite.Comp.LayerMap[key]--;
            }
        }

        layer.Owner = default;
        layer.Index = -1;

#if DEBUG
        foreach (var otherLayer in sprite.Comp.Layers)
        {
            DebugTools.AssertEqual(otherLayer, sprite.Comp.Layers[otherLayer.Index]);
        }
#endif

        sprite.Comp.BoundsDirty = true;
        _tree.QueueTreeUpdate(sprite!);
        QueueUpdateIsInert(sprite!);
        return true;
    }

    #region AddLayer

    /// <summary>
    /// Add the given sprite layer. If an index is specified, this will insert the layer with the given index, resulting
    /// in all other layers being reshuffled.
    /// </summary>
    public int AddLayer(Entity<SpriteComponent?> sprite, Layer layer, int? index = null, int? parentLayer = null)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
        {
            layer.Index = -1;
            layer.Owner = default;
            return -1;
        }

        layer.Owner = sprite!;

        if (index is { } i && i != sprite.Comp.Layers.Count)
        {
            foreach (var otherLayer in sprite.Comp.Layers)
            {
                if (i <= otherLayer.Index)
                    otherLayer.Index++;

                for (var j = 0; j < otherLayer.ChildLayers.Count; j++)
                {
                    var childIndex = otherLayer.ChildLayers[j];
                    if (i <= childIndex)
                        otherLayer.ChildLayers[j]++;
                }

                if (i <= otherLayer.ParentLayer)
                    otherLayer.ParentLayer++;
            }

            // TODO SPRITE track inverse-mapping?
            sprite.Comp.Layers.Insert(i, layer);
            layer.Index = i;

            foreach (var (key, value) in sprite.Comp.LayerMap)
            {
                if (value >= i)
                    sprite.Comp.LayerMap[key]++;
            }

            if (parentLayer != null && i <= parentLayer)
                parentLayer++;
        }
        else
        {
            layer.Index = sprite.Comp.Layers.Count;
            sprite.Comp.Layers.Add(layer);
        }

        if (parentLayer != null)
        {
            if (parentLayer >= sprite.Comp.Layers.Count || parentLayer < 0)
            {
                Log.Error($"Attempted to set a parent layer index '{index}' on entity {ToPrettyString(sprite)} that is out of bounds! Trace:\n{Environment.StackTrace}");
            }
            else
            {
                layer.ParentLayer = parentLayer;
                sprite.Comp.Layers[parentLayer.Value].ChildLayers.Add(layer.Index);
            }
        }

#if DEBUG
        foreach (var otherLayer in sprite.Comp.Layers)
        {
            DebugTools.AssertEqual(otherLayer, sprite.Comp.Layers[otherLayer.Index]);
        }
#endif

        layer.BoundsDirty = true;
        if (!layer.Blank)
        {
            sprite.Comp.BoundsDirty = true;
            _tree.QueueTreeUpdate(sprite!);
            QueueUpdateIsInert(sprite!);
        }
        return layer.Index;
    }

    /// <summary>
    /// Add a layer corresponding to the given RSI state.
    /// </summary>
    /// <param name="sprite">The sprite</param>
    /// <param name="stateId">The RSI state</param>
    /// <param name="rsi">The RSI to use. If not specified, it will default to using <see cref="SpriteComponent.BaseRSI"/></param>
    /// <param name="index">The layer index to use for the new sprite.</param>
    /// <param name="parentLayer">The layer index that this layer should be a child of.</param>
    /// <returns></returns>
    public int AddRsiLayer(Entity<SpriteComponent?> sprite, RSI.StateId stateId, RSI? rsi = null, int? index = null, int? parentLayer = null)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return -1;

        var layer = AddBlankLayer(sprite!, index, parentLayer);

        if (rsi != null)
            LayerSetRsi(layer, rsi, stateId);
        else
            LayerSetRsiState(layer, stateId);

        return layer.Index;
    }

    /// <summary>
    /// Add a layer corresponding to the given RSI state.
    /// </summary>
    /// <param name="sprite">The sprite</param>
    /// <param name="state">The RSI state</param>
    /// <param name="path">The path to the RSI.</param>
    /// <param name="index">The layer index to use for the new sprite.</param>
    /// <param name="parentLayer">The layer index that this layer should be a child of.</param>
    /// <returns></returns>
    public int AddRsiLayer(Entity<SpriteComponent?> sprite, RSI.StateId state, ResPath path, int? index = null, int? parentLayer = null)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return -1;

        if (!_resourceCache.TryGetResource<RSIResource>(TextureRoot / path, out var res))
            Log.Error($"Unable to load RSI '{path}'. Trace:\n{Environment.StackTrace}");

        if (path.Extension != "rsi")
            Log.Error($"Expected rsi path but got '{path}'?");

        return AddRsiLayer(sprite, state, res?.RSI, index, parentLayer);
    }

    public int AddTextureLayer(Entity<SpriteComponent?> sprite, ResPath path, int? index = null, int? parentLayer = null)
    {
        if (_resourceCache.TryGetResource<TextureResource>(TextureRoot / path, out var texture))
            return AddTextureLayer(sprite, texture?.Texture, index, parentLayer);

        if (path.Extension == "rsi")
            Log.Error($"Expected texture but got rsi '{path}', did you mean 'sprite:' instead of 'texture:'?");

        Log.Error($"Unable to load texture '{path}'. Trace:\n{Environment.StackTrace}");
        return AddTextureLayer(sprite, texture?.Texture, index, parentLayer);
    }

    public int AddTextureLayer(Entity<SpriteComponent?> sprite, Texture? texture, int? index = null, int? parentLayer = null)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return -1;

        var layer = new Layer {Texture = texture};
        return AddLayer(sprite, layer, index, parentLayer);
    }

    public int AddLayer(Entity<SpriteComponent?> sprite, SpriteSpecifier specifier, int? newIndex = null, int? parentLayer = null)
    {
        return specifier switch
        {
            SpriteSpecifier.Texture tex => AddTextureLayer(sprite, tex.TexturePath, newIndex, parentLayer),
            SpriteSpecifier.Rsi rsi => AddRsiLayer(sprite, rsi.RsiState, rsi.RsiPath, newIndex, parentLayer),
            _ => throw new NotImplementedException()
        };
    }

    /// <summary>
    /// Add a new sprite layer and populate it using the provided layer data.
    /// </summary>
    public int AddLayer(Entity<SpriteComponent?> sprite, PrototypeLayerData layerDatum, int? index, int? parentLayer = null)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return -1;

        var layer = AddBlankLayer(sprite!, index, parentLayer);
        LayerSetData(layer, layerDatum);
        return layer.Index;
    }

    /// <summary>
    /// Add a blank sprite layer.
    /// </summary>
    public Layer AddBlankLayer(Entity<SpriteComponent> sprite, int? index = null, int? parentLayer = null)
    {
        var layer = new Layer();
        AddLayer(sprite!, layer, index, parentLayer);
        return layer;
    }

    #endregion

    /// <summary>
    /// Sets a layer as the parent of another layer.
    /// If the child already had a parent, that old parent becomes the new parent of the provided layer,
    /// effectively inserting the provided layer in between the render order.
    /// </summary>
    public void SetAsParent(Entity<SpriteComponent> sprite, int layerIndex, int childLayerIndex)
    {
        if (layerIndex == childLayerIndex || !TryGetLayer(sprite.AsNullable(), layerIndex, out var layer, true) || !TryGetLayer(sprite.AsNullable(), childLayerIndex, out var childLayer, true))
            return;

        if (childLayer.ParentLayer != null)
        {
            var parentLayer = sprite.Comp.Layers[childLayer.ParentLayer.Value];
            var pos = parentLayer.ChildLayers.FindIndex(i => i == childLayerIndex);
            parentLayer.ChildLayers[pos] = layerIndex;
            layer.ParentLayer = childLayer.ParentLayer;
        }

        childLayer.ParentLayer = layerIndex;
        layer.ChildLayers.Add(childLayerIndex);
    }
}
