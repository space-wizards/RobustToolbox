using ClientInterfaces.GOC;
using GameObject;

namespace ClientServices.GOC
{
    public class EntityManagerContainer : IEntityManagerContainer
    {
        #region IEntityManagerContainer Members

        public EntityManager EntityManager { get; set; }

        #endregion
    }
}