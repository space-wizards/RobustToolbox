using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Map.Components;
using System.Linq;
using Robust.Shared.Timing;

namespace Robust.Shared.GameObjects
{
    [UsedImplicitly]
    public abstract partial class SharedMapSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] protected readonly IMapManager MapManager = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        public override void Initialize()
        {
            base.Initialize();

            InitializeMap();
            InitializeGrid();

            SubscribeLocalEvent<MapLightComponent, ComponentGetState>(OnMapLightGetState);
            SubscribeLocalEvent<MapLightComponent, ComponentHandleState>(OnMapLightHandleState);
        }
    }

    /// <summary>
    ///     Arguments for when a map is created or deleted.
    /// </summary>
    public sealed class MapChangedEvent : EntityEventArgs
    {
        /// <summary>
        ///     Creates a new instance of this class.
        /// </summary>
        public MapChangedEvent(MapId map, bool created)
        {
            Map = map;
            Created = created;
        }

        /// <summary>
        ///     Map that is being modified.
        /// </summary>
        public MapId Map { get; }

        /// <summary>
        ///     The map is being created.
        /// </summary>
        public bool Created { get; }

        /// <summary>
        ///     The map is being destroyed (not <see cref="Created"/>).
        /// </summary>
        public bool Destroyed => !Created;
    }

#pragma warning disable CS0618
    public sealed class GridStartupEvent : EntityEventArgs
    {
        public EntityUid EntityUid { get; }

        public GridStartupEvent(EntityUid uid)
        {
            EntityUid = uid;
        }
    }

    public sealed class GridRemovalEvent : EntityEventArgs
    {
        public EntityUid EntityUid { get; }

        public GridRemovalEvent(EntityUid uid)
        {
            EntityUid = uid;
        }
    }

    /// <summary>
    /// Raised whenever a grid is being initialized.
    /// </summary>
    public sealed class GridInitializeEvent : EntityEventArgs
    {
        public EntityUid EntityUid { get; }

        public GridInitializeEvent(EntityUid uid)
        {
            EntityUid = uid;
        }
    }
#pragma warning restore CS0618

    /// <summary>
    /// Raised whenever a grid is Added
    /// </summary>
    public sealed class GridAddEvent : EntityEventArgs
    {
        public EntityUid EntityUid { get; }

        public GridAddEvent(EntityUid uid)
        {
            EntityUid = uid;
        }
    }

    /// <summary>
    ///     Arguments for when a single tile on a grid is changed locally or remotely.
    /// </summary>
    public sealed class TileChangedEvent : EntityEventArgs
    {
        /// <summary>
        ///     Creates a new instance of this class.
        /// </summary>
        public TileChangedEvent(EntityUid uid, TileRef newTile, Tile oldTile)
        {
            Entity = uid;
            NewTile = newTile;
            OldTile = oldTile;
        }

        /// <summary>
        ///     EntityUid of the grid with the tile-change. TileRef stores the GridId.
        /// </summary>
        public EntityUid Entity { get; }

        /// <summary>
        ///     New tile that replaced the old one.
        /// </summary>
        public TileRef NewTile { get; }

        /// <summary>
        ///     Old tile that was replaced.
        /// </summary>
        public Tile OldTile { get; }
    }

    /// <summary>
    ///     Arguments for when a one or more tiles on a grid are modified at once.
    /// </summary>
    public sealed class GridModifiedEvent : EntityEventArgs
    {
        /// <summary>
        ///     Grid being changed.
        /// </summary>
        public MapGridComponent Grid { get; }

        /// <summary>
        /// Set of tiles that were modified.
        /// </summary>
        public IReadOnlyCollection<(Vector2i position, Tile tile)> Modified { get; }

        /// <summary>
        ///     Creates a new instance of this class.
        /// </summary>
        public GridModifiedEvent(MapGridComponent grid, IReadOnlyCollection<(Vector2i position, Tile tile)> modified)
        {
            Grid = grid;
            Modified = modified;
        }
    }
}
