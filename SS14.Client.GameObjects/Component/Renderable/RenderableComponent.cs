using System;
using ClientInterfaces.GOC;
using GameObject;
using GorgonLibrary;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Renderable;

namespace CGO
{
    public class RenderableComponent : Component, IRenderableComponent
    {
        public RenderableComponent()
        {
            Family = ComponentFamily.Renderable;
        }

        public override Type StateType
        {
            get { return typeof (RenderableComponentState); }
        }

        #region IRenderableComponent Members

        public DrawDepth DrawDepth { get; set; }

        public virtual float Bottom
        {
            get { return 0f; }
        }

        public virtual void Render(Vector2D topLeft, Vector2D bottomRight)
        {
        }

        #endregion

        public override void HandleComponentState(dynamic state)
        {
            DrawDepth = state.DrawDepth;
        }
    }
}