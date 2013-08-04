using ClientInterfaces.GOC;
using GameObject;

namespace ClientServices.GOC
{
    public class EntityManagerContainer :IEntityManagerContainer
    {
        public EntityManager EntityManager { get; set; }
    }
}
