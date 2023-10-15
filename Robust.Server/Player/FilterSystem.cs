using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using ActorComponent = Robust.Shared.GameObjects.ActorComponent;

namespace Robust.Server.Player
{
    internal sealed class FilterSystem : SharedFilterSystem
    {
        public override Filter FromEntities(Filter filter, params EntityUid[] entities)
        {
            foreach (var uid in entities)
            {
                if (EntityManager.TryGetComponent(uid, out ActorComponent? actor))
                    filter.AddPlayer(actor.Session);
            }

            return filter;
        }
    }
}
