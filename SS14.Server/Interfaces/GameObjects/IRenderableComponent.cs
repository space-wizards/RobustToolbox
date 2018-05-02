using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Server.Interfaces.GameObjects
{
    public interface IRenderableComponent : IComponent
    {
        bool Visible { get; set; }
    }
}
