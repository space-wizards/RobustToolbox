using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects.Components.Map
{
    /// <summary>
    ///     Represents a map grid inside the ECS system.
    /// </summary>
    internal interface IMapGridComponent
    {
        IMapGrid Grid { get; }
    }

    /// <inheritdoc cref="IMapGridComponent"/>
    public class MapGridComponent : Component, IMapGridComponent
    {
        [Dependency] private readonly IMapManager _mapManager;

        private GridId _gridIndex;

        /// <inheritdoc />
        public override string Name => "MapGrid";

        /// <inheritdoc />
        public IMapGrid Grid => _mapManager.GetGrid(_gridIndex);

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _gridIndex, "index", GridId.Nullspace);
        }
    }
}
