using Robust.Shared.GameObjects;

namespace Robust.Shared.Player
{
    /// <summary>
    ///     EntitySystem used for any Filters that require different behaviors on the server and client.
    /// </summary>
    internal abstract class SharedFilterSystem : EntitySystem
    {
        /// <summary>
        ///     Adds all players attached to the given entities to the given filter, then returns it.
        /// </summary>
        public abstract Filter FromEntities(Filter filter, params EntityUid[] entities);
    }
}
