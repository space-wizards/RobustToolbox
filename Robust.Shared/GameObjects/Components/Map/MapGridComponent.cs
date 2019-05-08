using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects.Components.Map
{
    /// <summary>
    ///     Represents a map grid inside the ECS system.
    /// </summary>
    internal interface IMapGridComponent : IComponent
    {
        GridId GridIndex { get; }
        IMapGrid Grid { get; }
    }

    /// <inheritdoc cref="IMapGridComponent"/>
    public class MapGridComponent : Component, IMapGridComponent
    {
        [Dependency] private readonly IMapManager _mapManager;

        [ViewVariables(VVAccess.ReadOnly)]
        private GridId _gridIndex;

        /// <inheritdoc />
        public override string Name => "MapGrid";

        /// <inheritdoc />
        public GridId GridIndex
        {
            get => _gridIndex;
            internal set => _gridIndex = value;
        }

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
