using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects.Components.Map
{
    /// <summary>
    ///     Represents a world map inside the ECS system.
    /// </summary>
    public interface IMapComponent
    {
        MapId WorldMap { get; }
    }

    /// <inheritdoc cref="IMapComponent"/>
    public class MapComponent : Component, IMapComponent
    {
        [ViewVariables(VVAccess.ReadOnly)]
        private MapId _mapIndex;

        /// <inheritdoc />
        public override string Name => "Map";

        /// <inheritdoc />
        public MapId WorldMap => _mapIndex;
        
        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _mapIndex, "index", MapId.Nullspace);
        }
    }
}
