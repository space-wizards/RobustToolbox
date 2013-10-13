using GorgonLibrary;
using SS13_Shared.GO;

namespace ClientInterfaces.GOC
{
    public interface IRenderableComponent
    {
        DrawDepth DrawDepth { get; set; }
        float Bottom { get; }
        void Render(Vector2D topLeft, Vector2D bottomRight);
        void RemoveSlave(IRenderableComponent slave);
    }
}