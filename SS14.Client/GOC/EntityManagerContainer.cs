using SS14.Client.Interfaces.GOC;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;

namespace SS14.Client.GOC
{
    [IoCTarget]
    public class EntityManagerContainer : IEntityManagerContainer
    {
        #region IEntityManagerContainer Members

        public EntityManager EntityManager { get; set; }

        #endregion
    }
}
