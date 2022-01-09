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

        public static void RunMapInit(this EntityUid entity)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var meta = entMan.GetComponent<MetaDataComponent>(entity);

            if (meta.EntityLifeStage == EntityLifeStage.MapInitialized)
                return; // Already map initialized, do nothing.

            DebugTools.Assert(meta.EntityLifeStage == EntityLifeStage.Initialized, $"Expected entity {entMan.ToPrettyString(entity)} to be initialized, was {meta.EntityLifeStage}");
            meta.EntityLifeStage = EntityLifeStage.MapInitialized;

            entMan.EventBus.RaiseLocalEvent(entity, MapInit, false);
            foreach (var init in entMan.GetComponents<IMapInit>(entity))
            {
                init.MapInit();
            }
        }
    }
}
