using SS14.Shared.GameObjects;

namespace SS14.Client.Interfaces.GOC
{
    public interface IEntityManagerContainer
    {
        EntityManager EntityManager { get; set; }
    }
}