using Robust.Client.Interfaces.GameStates;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.GameObjects
{
    public static class EntityManagerExt
    {
        public static void RaisePredictiveEvent<T>(this IEntityManager entityManager, T msg)
            where T : EntitySystemMessage
        {
            var sequence = IoCManager.Resolve<IClientGameStateManager>().SystemMessageDispatched(msg);
            entityManager.EntityNetManager.SendSystemNetworkMessage(msg, sequence);

            var eventArgs = new EntitySessionEventArgs(IoCManager.Resolve<IPlayerManager>().LocalPlayer.Session);

            entityManager.EventBus.RaiseEvent(EventSource.Local, msg);
            entityManager.EventBus.RaiseEvent(EventSource.Local, new EntitySessionMessage<T>(eventArgs, msg));
        }
    }
}
