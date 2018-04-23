using System;
using System.Collections.Generic;
using SS14.Shared.Maths;

namespace SS14.Shared.GameObjects
{
    [Serializable]
    public class SpriteComponentState : ComponentState
    {
        public readonly bool Visible;
        public readonly DrawDepth DrawDepth;
        public readonly Vector2 Scale;
        public readonly Angle Rotation;
        public readonly Vector2 Offset;
        public readonly Color Color;
        public readonly bool Directional;
        public readonly string BaseRsiPath;
        public readonly List<Layer> Layers;

        public SpriteComponentState(
            bool visible,
            DrawDepth drawDepth,
            Vector2 scale,
            Angle rotation,
            Vector2 offset,
            Color color,
            bool directional,
            string baseRsiPath,
            List<Layer> layers)
            : base(NetIDs.SPRITE)
        {
            Visible = visible;
            DrawDepth = drawDepth;
            Scale = scale;
            Rotation = rotation;
            Offset = offset;
            Color = color;
            Directional = directional;
            BaseRsiPath = baseRsiPath;
            Layers = layers;
        }

        [Serializable]
        public struct Layer
        {
        }
    }
}
