using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics
{
    internal interface IRenderHandle
    {
        DrawingHandleScreen DrawingHandleScreen { get; }
        DrawingHandleWorld DrawingHandleWorld { get; }

        void RenderInRenderTarget(IRenderTarget target, Action a, Color clearColor=default);

        void SetScissor(UIBox2i? scissorBox);
        void DrawEntity(EntityUid entity, Vector2 position, Vector2 scale, Direction? overrideDirection);
    }
}
