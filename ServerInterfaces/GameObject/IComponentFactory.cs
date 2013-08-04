using GameObject;

namespace ServerInterfaces.GOC
{
    public interface IComponentFactory
    {
        Component GetComponent(string componentType);
    }
}