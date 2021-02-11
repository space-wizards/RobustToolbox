using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.GameObjects
{
    public interface ISpriteLayer
    {
        SpriteComponent.DirectionOffset DirOffset { get; set; }

        RSI? Rsi { get; set; }
        RSI.StateId RsiState { get; set; }
        RSI? ActualRsi { get; }

        Texture? Texture { get; set; }

        Angle Rotation { get; set; }
        Vector2 Scale { get; set; }

        bool Visible { get; set; }
        Color Color { get; set; }

        float AnimationTime { get; set; }
        int AnimationFrame { get; }
        bool AutoAnimated { get; set; }

        RSI.State.Direction EffectiveDirection(Angle worldRotation);

        Vector2 LocalToLayer(Vector2 localPos);
    }
}
