using System.Collections.Generic;
using Robust.Shared.IoC;
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
        private static readonly MapInitEvent MapInit = new MapInitEvent();

        public static void RunMapInit(this IEntity entity)
        {
            DebugTools.Assert((!IoCManager.Resolve<IEntityManager>().EntityExists(entity) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(entity).EntityLifeStage) == EntityLifeStage.Initialized);
            IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(entity).EntityLifeStage = EntityLifeStage.MapInitialized;

            IoCManager.Resolve<IEntityManager>().EventBus.RaiseLocalEvent(entity, MapInit, false);
            foreach (var init in IoCManager.Resolve<IEntityManager>().GetComponents<IMapInit>(entity))
            {
                init.MapInit();
            }
        }
    }
}
