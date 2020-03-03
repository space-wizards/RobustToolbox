using Robust.Client.Interfaces.GameStates;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.GameObjects
{
    public static class EntityManagerExt
    {
        public static void RaisePredictiveEvent(this IEntityManager entityManager, EntitySystemMessage msg)
        {
            var sequence = IoCManager.Resolve<IClientGameStateManager>().SystemMessageDispatched(msg);
            entityManager.EntityNetManager.SendSystemNetworkMessage(msg, sequence);

            entityManager.EventBus.RaiseEvent(EventSource.Local, msg);
        }
    }
}
