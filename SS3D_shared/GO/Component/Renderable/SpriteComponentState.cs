using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS13_Shared.GO.Component.Renderable
{
    [Serializable]
    public class SpriteComponentState : RenderableComponentState
    {
        public bool Visible;
        public string SpriteKey;
        public string BaseName;

        public SpriteComponentState(bool visible, DrawDepth drawDepth, string spriteKey, string baseName)
            :base(drawDepth)
        {
            Visible = visible;
            SpriteKey = spriteKey;
            BaseName = baseName;
        }
    }
}
