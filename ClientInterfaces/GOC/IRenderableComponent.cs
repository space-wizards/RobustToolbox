using GorgonLibrary;
using SS13_Shared.GO;

namespace ClientInterfaces.GOC
{
    public interface IRenderableComponent
    {
        void Render(Vector2D topLeft, Vector2D bottomRight);
        DrawDepth DrawDepth { get; set; }
        float Bottom { get; }
    }
}
