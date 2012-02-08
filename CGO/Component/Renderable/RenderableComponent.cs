using SS13_Shared.GO;

namespace CGO
{
    public class RenderableComponent : GameObjectComponent, IRenderableComponent
    {
        public DrawDepth DrawDepth { get; set; }

        public RenderableComponent()
        {
            family = ComponentFamily.Renderable; 
        }

        public virtual void Render()
        {

        }
    }
}
