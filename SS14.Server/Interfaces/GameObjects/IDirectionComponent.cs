using SS14.Shared;
using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Server.Interfaces.GameObjects
{
    public interface IDirectionComponent : IComponent
    {
        Direction Direction { get; set; }
    }
}
