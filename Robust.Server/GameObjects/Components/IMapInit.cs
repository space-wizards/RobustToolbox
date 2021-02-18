using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects
{
    /// <summary>
    ///     Defines a component that has "map initialization" behavior.
    ///     Basically irreversible behavior that moves the map from "map editor" to playable,
    ///     like spawning preset objects.
    /// </summary>
    public interface IMapInit
    {
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
