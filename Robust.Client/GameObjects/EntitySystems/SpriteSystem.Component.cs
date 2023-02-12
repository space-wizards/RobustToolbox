using System.Linq;

namespace Robust.Client.GameObjects;

public sealed partial class SpriteSystem
{
    /// <summary>
    /// Resets the sprite's animated layers to align with realtime.
    /// </summary>
    public void SetAutoAnimateSync(SpriteComponent sprite)
    {
        foreach (var layer in sprite.AllLayers)
        {
            if (!layer.AutoAnimated || layer is not SpriteComponent.Layer spriteLayer)
                continue;

            SetAutoAnimateSync(sprite, spriteLayer);
        }
    }

    /// <summary>
    /// Resets the layer's animation to align with realtime.
    /// </summary>
    public void SetAutoAnimateSync(SpriteComponent sprite, SpriteComponent.Layer layer)
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

        var animationDuration = state.GetDelays().Sum();
        var curTime = _timing.RealTime;

        layer.AnimationTimeLeft = (float) -(curTime.TotalSeconds % animationDuration);
        layer.AnimationFrame = 0;
    }
}
