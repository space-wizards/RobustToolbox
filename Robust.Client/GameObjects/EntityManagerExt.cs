using Robust.Client.GameStates;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
{
    public static class EntityManagerExt
    {
        public static void RaisePredictiveEvent<T>(this IEntityManager entityManager, T msg)
            where T : EntitySystemMessage
        {
            var localPlayer = IoCManager.Resolve<IPlayerManager>().LocalPlayer;
            DebugTools.AssertNotNull(localPlayer);

            var sequence = IoCManager.Resolve<IClientGameStateManager>().SystemMessageDispatched(msg);
            entityManager.EntityNetManager.SendSystemNetworkMessage(msg, sequence);

            var eventArgs = new EntitySessionEventArgs(localPlayer!.Session);

            entityManager.EventBus.RaiseEvent(EventSource.Local, msg);
            entityManager.EventBus.RaiseEvent(EventSource.Local, new EntitySessionMessage<T>(eventArgs, msg));
        }
    }
}
