using ClientInterfaces.GOC;

namespace CGO
{
    public interface IRenderableComponent
    {
        void Render();
        SS13_Shared.GO.DrawDepth DrawDepth { get; set; }
        IEntity Owner { get; set; }
    }
}
