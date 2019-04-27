using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Server.GameObjects.Components
{
    /// <summary>
    ///     Convenience wrapper to implement <see cref="IMapInit"/>.
    /// </summary>
    public abstract class MapInitComponent : Component, IMapInit
    {
        private bool _hasInitialized;

        [ViewVariables]
        public bool HasInitialized => _hasInitialized;

        void IMapInit.MapInit()
        {
            if (_hasInitialized)
            {
                return;
            }

            _hasInitialized = true;

            MapInit();
        }

        protected abstract void MapInit();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _hasInitialized, "has_initialized", false);
        }
    }
}
