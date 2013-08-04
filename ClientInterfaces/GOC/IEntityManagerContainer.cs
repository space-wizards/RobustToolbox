using GameObject;

namespace ClientInterfaces.GOC
{
    public interface IEntityManagerContainer
    {
        EntityManager EntityManager { get; set; }
    }
}