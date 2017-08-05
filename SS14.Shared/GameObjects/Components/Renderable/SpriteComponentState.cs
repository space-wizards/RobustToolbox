using System;

namespace SS14.Shared.GameObjects
{
    [Serializable]
    public class SpriteComponentState : RenderableComponentState
    {
        public readonly string BaseName;
        public readonly string SpriteKey;
        public readonly bool Visible;

        public SpriteComponentState(bool visible, DrawDepth drawDepth, string spriteKey, string baseName)
            : base(drawDepth, null, NetIDs.SPRITE)
        {
            Visible = visible;
            SpriteKey = spriteKey;
            BaseName = baseName;
        }
    }
}
