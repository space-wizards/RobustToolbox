using SS14.Shared.GameObjects;

namespace SS14.Server.Interfaces.GOC
{
    public interface IComponentFactory
    {
        Component GetComponent(string componentType);
    }
}