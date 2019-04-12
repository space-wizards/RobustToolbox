using Robust.Shared.Interfaces.GameObjects;

namespace Robust.Server.Interfaces.GameObjects
{
    public interface IRenderableComponent : IComponent
    {
        bool Visible { get; set; }
    }
}
