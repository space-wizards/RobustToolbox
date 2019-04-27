using Robust.Server.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;

namespace Robust.Server.Interfaces.GameObjects
{
    /// <summary>
    ///     Defines a component that has "map initialization" behavior.
    ///     Basically irreversible behavior that moves the map from "map editor" to playable,
    ///     like spawning preset objects.
    /// </summary>
    /// <seealso cref="MapInitComponent"/>
    public interface IMapInit
    {
        /// <summary>
        ///     Get whether this entity has already had MapInit ran on it.
        ///     This should of course be inherited through map saving.
        /// </summary>
        bool HasInitialized { get; }

        void MapInit();
    }

    public static class MapInitExt
    {
        public static void RunMapInit(this IEntity entity)
        {
            foreach (var init in entity.GetAllComponents<IMapInit>())
            {
                init.MapInit();
            }
        }
    }
}
