using System.Linq;
using Robust.Server.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;

namespace Robust.Server.Interfaces.GameObjects
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
            foreach (var init in entity.GetAllComponents<IMapInit>().ToList())
            {
                init.MapInit();
            }
        }
    }
}
