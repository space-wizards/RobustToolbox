using System;

namespace SS14.Shared.GameObjects.Components.Renderable
{
    [Serializable]
    public class SpriteComponentState : RenderableComponentState
    {
        public string BaseName;
        public string SpriteKey;
        public bool Visible;

        public SpriteComponentState(bool visible, DrawDepth drawDepth, string spriteKey, string baseName)
            : base(drawDepth, null)
        {
            Visible = visible;
            SpriteKey = spriteKey;
            BaseName = baseName;
        }
    }
}
