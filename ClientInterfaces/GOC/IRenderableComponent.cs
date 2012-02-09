using SS13_Shared.GO;

namespace ClientInterfaces.GOC
{
    public interface IRenderableComponent
    {
        void Render();
        DrawDepth DrawDepth { get; set; }
        IEntity Owner { get; set; }
    }
}
