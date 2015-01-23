using SS14.Client.Interfaces.GOC;
using SS14.Shared.GameObjects;

namespace SS14.Client.Services.GOC
{
    public class EntityManagerContainer : IEntityManagerContainer
    {
        #region IEntityManagerContainer Members

        public EntityManager EntityManager { get; set; }

        #endregion
    }
}