using System.Linq;

namespace Robust.Client.GameObjects;

public sealed partial class SpriteSystem
{
    /// <summary>
    /// Resets the sprite's animated layers to align with a given time (in seconds).
    /// </summary>
    public void SetAutoAnimateSync(SpriteComponent sprite, double time)
    {
        foreach (var layer in sprite.AllLayers)
        {
            if (layer is not SpriteComponent.Layer spriteLayer)
                continue;

            SetAutoAnimateSync(sprite, spriteLayer, time);
        }
    }

    /// <summary>
    /// Resets the layer's animation to align with a given time (in seconds).
    /// </summary>
    public void SetAutoAnimateSync(SpriteComponent sprite, SpriteComponent.Layer layer, double time)
    {
        if (!layer.AutoAnimated)
            return;

        var rsi = layer.RSI ?? sprite.BaseRSI;

        if (rsi == null || !rsi.TryGetState(layer.State, out var state))
        {
            state = GetFallbackState();
        }

        if (!state.IsAnimated)
        {
            return;
        }

        layer.AnimationTimeLeft = (float) -(time % state.TotalDelay);
        layer.AnimationFrame = 0;
    }
}
