using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    /// Defines a component that has "map initialization" behavior.
    /// Basically irreversible behavior that moves the map from "map editor" to playable,
    /// like spawning preset objects.
    /// </summary>
    public interface IMapInit
    {
        void MapInit();
    }

    /// <summary>
    ///     Raised directed on an entity when the map is initialized.
    /// </summary>
    public class MapInitEvent : EntityEventArgs
    {
    }

    public static class MapInitExt
    {
        public static void RunMapInit(this IEntity entity)
        {
            DebugTools.Assert(entity.LifeStage == EntityLifeStage.Initialized);
            entity.LifeStage = EntityLifeStage.MapInitialized;

            entity.EntityManager.EventBus.RaiseLocalEvent(entity.Uid, new MapInitEvent(), false);
            foreach (var init in entity.GetAllComponents<IMapInit>())
            {
                init.MapInit();
            }
        }
    }
}
