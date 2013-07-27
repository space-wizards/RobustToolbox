using ClientInterfaces.GOC;
using SS13_Shared.GO;
using GorgonLibrary;
using SS13_Shared.GO.Component.Renderable;

namespace CGO
{
    public class RenderableComponent : GameObjectComponent, IRenderableComponent
    {
        public DrawDepth DrawDepth { get; set; }
        public virtual float Bottom
        {
            get { return 0f; }
        }
        
        public RenderableComponent():base()
        {
            Family = ComponentFamily.Renderable;
        }

        public virtual void Render(Vector2D topLeft, Vector2D bottomRight)
        {

        }

        public override System.Type StateType
        {
            get
            {
                return typeof(RenderableComponentState);
            }
        }

        public override void HandleComponentState(dynamic state)
        {
            DrawDepth = state.DrawDepth;
        }
    }
}
