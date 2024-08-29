﻿using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Graphics;
using Robust.Shared.Graphics.RSI;
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

        RsiDirection EffectiveDirection(Angle worldRotation);

        /// <summary>
        ///     Layer size in pixels.
        ///     Don't account layer scale or sprite world transform.
        /// </summary>
        Vector2i PixelSize { get; }

        /// <summary>
        ///     Calculate layer bounding box in sprite local-space coordinates.
        /// </summary>
        /// <returns>Bounding box in sprite local-space coordinates.</returns>
        Box2 CalculateBoundingBox();
    }
}
