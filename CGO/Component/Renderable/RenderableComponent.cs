using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CGO
{
    public class RenderableComponent : GameObjectComponent, IGameObjectComponent, IRenderableComponent
    {
        public RenderableComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.Renderable;
        }

        public virtual void Render()
        {

        }
    }
}
