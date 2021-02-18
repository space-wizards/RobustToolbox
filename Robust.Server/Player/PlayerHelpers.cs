using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Robust.Server.Player
{
    public static class PlayerHelpers
    {
        public static IPlayerSession? PlayerSession(this IEntity? entity)
        {
            if (entity == null)
                return null;
            
            if (!entity.TryGetComponent(out IActorComponent? actorComponent))
                return null;

            return actorComponent.playerSession;
        }
    }
}