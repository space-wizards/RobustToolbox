using System;
using SS14.Shared.Maths;

namespace SS14.Shared.GameObjects
{
    [Serializable]
    public class SpriteComponentState : RenderableComponentState
    {
        public readonly string BaseName;
        public readonly string SpriteKey;
        public readonly bool Visible;
        public readonly Vector2 Offset;

        public SpriteComponentState(bool visible, DrawDepth drawDepth, string spriteKey, string baseName, Vector2 offset)
            : base(drawDepth, null, NetIDs.SPRITE)
        {
            Visible = visible;
            SpriteKey = spriteKey;
            BaseName = baseName;
            Offset = offset;
        }
    }
}
