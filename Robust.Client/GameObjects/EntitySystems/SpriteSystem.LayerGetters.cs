using System;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Sprite;
using static Robust.Client.GameObjects.SpriteComponent;
using static Robust.Client.Graphics.RSI;

namespace Robust.Client.GameObjects;

// This partial class contains various public methods for reading a layer's properties
public sealed partial class SpriteSystem
{
    #region RsiState

    /// <summary>
    /// Get the RSI state being used by the current layer. Note that the return value may be an invalid state. E.g.,
    /// this might be a texture layer that does not use RSIs.
    /// </summary>
    public StateId LayerGetRsiState(Entity<SpriteComponent?> sprite, int index)
    {
        return ResolveLayer(sprite, index, out var layer) ? layer.StateId : StateId.Invalid;
    }

    /// <summary>
    /// Get the RSI state being used by the current layer. Note that the return value may be an invalid state. E.g.,
    /// this might be a texture layer that does not use RSIs.
    /// </summary>
    public StateId LayerGetRsiState(Entity<SpriteComponent?> sprite, LayerKey key, StateId state)
    {
        return ResolveLayer(sprite, key, out var layer) ? layer.StateId : StateId.Invalid;
    }

    #endregion

    #region RsiState

    /// <summary>
    /// Returns the RSI being used by the layer to resolve it's RSI state. If the layer does not specify an RSI, this
    /// will just be the base RSI of the owning sprite (<see cref="SpriteComponent.BaseRSI"/>).
    /// </summary>
    public RSI? LayerGetEffectiveRsi(Entity<SpriteComponent?> sprite, int index)
    {
        ResolveLayer(sprite, index, out var layer);
        return layer?.ActualRsi;
    }

    /// <summary>
    /// Returns the RSI being used by the layer to resolve it's RSI state. If the layer does not specify an RSI, this
    /// will just be the base RSI of the owning sprite (<see cref="SpriteComponent.BaseRSI"/>).
    /// </summary>
    public RSI? LayerGetEffectiveRsi(Entity<SpriteComponent?> sprite, LayerKey key, StateId state)
    {
        ResolveLayer(sprite, key, out var layer);
        return layer?.ActualRsi;
    }

    #endregion

    #region Directions

    public RsiDirectionType LayerGetDirections(Entity<SpriteComponent?> sprite, int index)
    {
        return ResolveLayer(sprite, index, out var layer)
            ? LayerGetDirections(layer)
            : RsiDirectionType.Dir1;
    }

    public RsiDirectionType LayerGetDirections(Entity<SpriteComponent?> sprite, LayerKey key)
    {
        return ResolveLayer(sprite, key, out var layer)
            ? LayerGetDirections(layer)
            : RsiDirectionType.Dir1;
    }

    public RsiDirectionType LayerGetDirections(Layer layer)
    {
        if (!layer.StateId.IsValid)
            return RsiDirectionType.Dir1;

        // Pull texture from RSI state instead.
        if (layer.ActualRsi is not {} rsi || !rsi.TryGetState(layer.StateId, out var state))
            return RsiDirectionType.Dir1;

        return state.RsiDirections;
    }

    public int LayerGetDirectionCount(Entity<SpriteComponent?> sprite, int index)
    {
        return ResolveLayer(sprite, index, out var layer) ? LayerGetDirectionCount(layer) : 1;
    }

    public int LayerGetDirectionCount(Entity<SpriteComponent?> sprite, LayerKey key)
    {
        return ResolveLayer(sprite, key, out var layer) ? LayerGetDirectionCount(layer) : 1;
    }

    public int LayerGetDirectionCount(Layer layer)
    {
        return LayerGetDirections(layer) switch
        {
            RsiDirectionType.Dir1 => 1,
            RsiDirectionType.Dir4 => 4,
            RsiDirectionType.Dir8 => 8,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    #endregion
}
