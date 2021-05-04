using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics
{
    internal interface IRenderHandle
    {
        DrawingHandleScreen DrawingHandleScreen { get; }
        DrawingHandleWorld DrawingHandleWorld { get; }

        void RenderInRenderTarget(IRenderTarget target, Action a);

        void SetScissor(UIBox2i? scissorBox);
        void DrawEntity(IEntity entity, Vector2 position, Vector2 scale, Direction? overrideDirection);
    }
}
