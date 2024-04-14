using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Robust.Shared.GameObjects
{
    public abstract partial class SharedMapSystem : EntitySystem
    {
        [Dependency] private readonly ITileDefinitionManager _tileMan = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] protected readonly IMapManager MapManager = default!;
        [Dependency] private readonly IMapManagerInternal _mapInternal = default!;
        [Dependency] private readonly INetManager _netManager = default!;
        [Dependency] private readonly FixtureSystem _fixtures = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        private EntityQuery<MapComponent> _mapQuery;
        private EntityQuery<MapGridComponent> _gridQuery;
        private EntityQuery<TransformComponent> _xformQuery;

        public override void Initialize()
        {
            base.Initialize();

            _mapQuery = GetEntityQuery<MapComponent>();
            _gridQuery = GetEntityQuery<MapGridComponent>();
            _xformQuery = GetEntityQuery<TransformComponent>();

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
        public EntityUid Uid;

        /// <summary>
        ///     Creates a new instance of this class.
        /// </summary>
        public MapChangedEvent(EntityUid uid, MapId map, bool created)
        {
            Uid = uid;
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
    [ByRefEvent]
    public readonly record struct TileChangedEvent
    {
        /// <summary>
        ///     Creates a new instance of this class.
        /// </summary>
        public TileChangedEvent(EntityUid uid, TileRef newTile, Tile oldTile, Vector2i chunkIndex)
        {
            Entity = uid;
            NewTile = newTile;
            OldTile = oldTile;
            ChunkIndex = chunkIndex;
        }

        /// <summary>
        ///     EntityUid of the grid with the tile-change. TileRef stores the GridId.
        /// </summary>
        public readonly EntityUid Entity;

        /// <summary>
        ///     New tile that replaced the old one.
        /// </summary>
        public readonly TileRef NewTile;

        /// <summary>
        ///     Old tile that was replaced.
        /// </summary>
        public readonly Tile OldTile;

        /// <summary>
        ///     The index of the grid-chunk that this tile belongs to.
        /// </summary>
        public readonly Vector2i ChunkIndex;
    }
}
