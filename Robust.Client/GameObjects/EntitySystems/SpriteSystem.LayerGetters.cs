using System;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
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
        if (TryGetLayer(sprite, index, out var layer, true))
            return layer.StateId;

        return StateId.Invalid;
    }

    /// <summary>
    /// Get the RSI state being used by the current layer. Note that the return value may be an invalid state. E.g.,
    /// this might be a texture layer that does not use RSIs.
    /// </summary>
    public StateId LayerGetRsiState(Entity<SpriteComponent?> sprite, string key, StateId state)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            return layer.StateId;

        return StateId.Invalid;
    }

    /// <summary>
    /// Get the RSI state being used by the current layer. Note that the return value may be an invalid state. E.g.,
    /// this might be a texture layer that does not use RSIs.
    /// </summary>
    public StateId LayerGetRsiState(Entity<SpriteComponent?> sprite, Enum key, StateId state)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            return layer.StateId;

        return StateId.Invalid;
    }

    #endregion

    #region RsiState

    /// <summary>
    /// Returns the RSI being used by the layer to resolve it's RSI state. If the layer does not specify an RSI, this
    /// will just be the base RSI of the owning sprite (<see cref="SpriteComponent.BaseRSI"/>).
    /// </summary>
    public RSI? LayerGetEffectiveRsi(Entity<SpriteComponent?> sprite, int index)
    {
        TryGetLayer(sprite, index, out var layer, true);
        return layer?.ActualRsi;
    }

    /// <summary>
    /// Returns the RSI being used by the layer to resolve it's RSI state. If the layer does not specify an RSI, this
    /// will just be the base RSI of the owning sprite (<see cref="SpriteComponent.BaseRSI"/>).
    /// </summary>
    public RSI? LayerGetEffectiveRsi(Entity<SpriteComponent?> sprite, string key, StateId state)
    {
        TryGetLayer(sprite, key, out var layer, true);
        return layer?.ActualRsi;
    }

    /// <summary>
    /// Returns the RSI being used by the layer to resolve it's RSI state. If the layer does not specify an RSI, this
    /// will just be the base RSI of the owning sprite (<see cref="SpriteComponent.BaseRSI"/>).
    /// </summary>
    public RSI? LayerGetEffectiveRsi(Entity<SpriteComponent?> sprite, Enum key, StateId state)
    {
        TryGetLayer(sprite, key, out var layer, true);
        return layer?.ActualRsi;
    }

    #endregion
}
