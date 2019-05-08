using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects.Components.Map
{
    /// <summary>
    ///     Represents a world map inside the ECS system.
    /// </summary>
    public interface IMapComponent
    {
        IMap WorldMap { get; }
    }

    /// <inheritdoc cref="IMapComponent"/>
    public class MapComponent : Component, IMapComponent
    {
        [Dependency] private readonly IMapManager _mapManager;

        private MapId _mapIndex;

        /// <inheritdoc />
        public override string Name => "Map";

        /// <inheritdoc />
        public IMap WorldMap => _mapManager.GetMap(_mapIndex);
        
        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _mapIndex, "index", MapId.Nullspace);
        }
    }
}
