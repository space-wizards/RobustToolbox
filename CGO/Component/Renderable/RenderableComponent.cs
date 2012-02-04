using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CGO
{
    public class RenderableComponent : GameObjectComponent, IGameObjectComponent, IRenderableComponent
    {
        private SS13_Shared.GO.DrawDepth m_drawDepth;
        public SS13_Shared.GO.DrawDepth DrawDepth
        {
            get { return m_drawDepth; }
            set { m_drawDepth = value; }
        }
        public RenderableComponent()
        {
            family = SS13_Shared.GO.ComponentFamily.Renderable; 
        }

        public virtual void Render()
        {

        }
    }
}
