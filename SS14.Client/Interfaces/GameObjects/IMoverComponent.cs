using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Client.Interfaces.GameObjects
{
    // Does nothing except ensure uniqueness between mover components.
    // There can only be one.
    public interface IMoverComponent : IComponent
    {
    }
}
