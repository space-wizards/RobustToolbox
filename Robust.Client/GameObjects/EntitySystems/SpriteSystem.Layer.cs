using System;
using System.Diagnostics.CodeAnalysis;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Robust.Client.GameObjects;

// This partial class contains various public methods for manipulating layers.
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

        if (index < 0 || index >= sprite.Comp.Layers.Count)
        {
            if (logMissing)
                Log.Error($"Layer index '{index}' on entity {ToPrettyString(sprite)} does not exist! Trace:\n{Environment.StackTrace}");
            return false;
        }

        layer = sprite.Comp.Layers[index];
        sprite.Comp.Layers.RemoveAt(index);

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

        RebuildBounds(sprite!);
        sprite.Comp.QueueUpdateIsInert();
        return true;
    }

    #region AddLayer

    /// <summary>
    /// Add the given sprite layer. If an index is specified, this will insert the layer with the given index, resulting
    /// in all other layers being reshuffled.
    /// </summary>
    public int AddLayer(Entity<SpriteComponent?> sprite, Layer layer, int? index = null)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return -1;

        if (index is { } i && i != sprite.Comp.Layers.Count)
        {
            // TODO SPRITE track inverse-mapping?
            sprite.Comp.Layers.Insert(i, layer);
            foreach (var (key, value) in sprite.Comp.LayerMap)
            {
                if (value >= i)
                    sprite.Comp.LayerMap[key]++;
            }
        }
        else
        {
            index = sprite.Comp.Layers.Count;
            sprite.Comp.Layers.Add(layer);
        }

        sprite.Comp.RebuildBounds();
        sprite.Comp.QueueUpdateIsInert();
        return index.Value;
    }

    /// <summary>
    /// Add a layer corresponding to the given RSI state.
    /// </summary>
    /// <param name="sprite">The sprite</param>
    /// <param name="stateId">The RSI state</param>
    /// <param name="rsi">The RSI to use. If not specified, it will default to using <see cref="SpriteComponent.BaseRSI"/></param>
    /// <param name="index">The layer index to use for the new sprite.</param>
    /// <returns></returns>
    public int AddRsiLayer(Entity<SpriteComponent?> sprite, RSI.StateId stateId, RSI? rsi = null, int? index = null)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return -1;

        var layer = new Layer(sprite.Comp) {State = stateId, RSI = rsi};
        rsi ??= sprite.Comp._baseRsi;

        if (rsi != null && rsi.TryGetState(stateId, out var state))
            layer.AnimationTimeLeft = state.GetDelay(0);
        else
            Log.Error($"State does not exist in RSI: '{stateId}'. Trace:\n{Environment.StackTrace}");

        return AddLayer(sprite, layer, index);
    }

    /// <summary>
    /// Add a layer corresponding to the given RSI state.
    /// </summary>
    /// <param name="sprite">The sprite</param>
    /// <param name="state">The RSI state</param>
    /// <param name="path">The path to the RSI.</param>
    /// <param name="index">The layer index to use for the new sprite.</param>
    /// <returns></returns>
    public int AddRsiLayer(Entity<SpriteComponent?> sprite, RSI.StateId state, ResPath path, int? index = null)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return -1;

        if (!_resourceCache.TryGetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / path, out var res))
            Log.Error($"Unable to load RSI '{path}'. Trace:\n{Environment.StackTrace}");

        if (path.Extension != "rsi")
            Log.Error($"Expected rsi path but got '{path}'?");

        return AddRsiLayer(sprite, state, res?.RSI, index);
    }

    public int AddTextureLayer(Entity<SpriteComponent?> sprite, ResPath path, int? index = null)
    {
        if (_resourceCache.TryGetResource<TextureResource>(SpriteSpecifierSerializer.TextureRoot / path, out var texture))
            return AddTextureLayer(sprite, texture?.Texture, index);

        if (path.Extension == "rsi")
            Log.Error($"Expected texture but got rsi '{path}', did you mean 'sprite:' instead of 'texture:'?");

        Log.Error($"Unable to load texture '{path}'. Trace:\n{Environment.StackTrace}");
        return AddTextureLayer(sprite, texture?.Texture, index);
    }

    public int AddTextureLayer(Entity<SpriteComponent?> sprite, Texture? texture, int? index = null)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return -1;

        var layer = new Layer(sprite.Comp) {Texture = texture};
        return AddLayer(sprite, layer, index);
    }

    public int AddLayer(Entity<SpriteComponent?> sprite, SpriteSpecifier specifier, int? newIndex = null)
    {
        return specifier switch
        {
            SpriteSpecifier.Texture tex => AddTextureLayer(sprite, tex.TexturePath, newIndex),
            SpriteSpecifier.Rsi rsi => AddRsiLayer(sprite, rsi.RsiState, rsi.RsiPath, newIndex),
            _ => throw new NotImplementedException()
        };
    }

    /// <summary>
    /// Add a new sprite layer and populate it using the provided layer data.
    /// </summary>
    public int AddLayer(Entity<SpriteComponent?> sprite, PrototypeLayerData layerDatum, int? index)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return -1;

        index = AddBlankLayer(sprite, index);
        sprite.Comp.LayerSetData(index, layerDatum);
        return index.Value;
    }

    /// <summary>
    /// Add a blank sprite layer.
    /// </summary>
    public int AddBlankLayer(Entity<SpriteComponent?> sprite, int? index = null)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return -1;

        var layer = new Layer(sprite.Comp);
        return AddLayer(sprite, layer, index);
    }

    #endregion
}
