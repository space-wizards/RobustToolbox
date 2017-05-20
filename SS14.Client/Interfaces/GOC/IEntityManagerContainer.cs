using SS14.Shared.GameObjects;
using SS14.Shared.IoC;

namespace SS14.Client.Interfaces.GOC
{
    public interface IEntityManagerContainer : IIoCInterface
    {
        EntityManager EntityManager { get; set; }
    }
}
