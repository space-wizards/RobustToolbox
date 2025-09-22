using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;

namespace Robust.Client.Player
{
    internal sealed class FilterSystem : SharedFilterSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override Filter FromEntities(Filter filter, params EntityUid[] entities)
        {
            if (_playerManager.LocalEntity is not {Valid: true} attachedUid)
                return filter;

            foreach (var uid in entities)
            {
                if (uid == attachedUid)
                    filter.AddPlayer(_playerManager.LocalSession!);
            }

            return filter;
        }
    }
}
