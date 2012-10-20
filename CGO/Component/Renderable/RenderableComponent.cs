using ClientInterfaces.GOC;
using SS13_Shared.GO;
using GorgonLibrary;

namespace CGO
{
    public class RenderableComponent : GameObjectComponent, IRenderableComponent
    {
        public DrawDepth DrawDepth { get; set; }
        public virtual float Bottom
        {
            get { return 0f; }
        }
        

        public override ComponentFamily Family
        {
            get { return ComponentFamily.Renderable; }
        }

        public virtual void Render(Vector2D topLeft, Vector2D bottomRight)
        {

        }
    }
}
