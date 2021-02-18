using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects
{
    public interface IRenderableComponent : IComponent
    {
        bool Visible { get; set; }
    }
}
