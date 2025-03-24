using System;
using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Physics;
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
        [Dependency] private readonly MetaDataSystem _meta = default!;

        private EntityQuery<FixturesComponent> _fixturesQuery;
        private EntityQuery<MapComponent> _mapQuery;
        private EntityQuery<MapGridComponent> _gridQuery;
        private EntityQuery<MetaDataComponent> _metaQuery;
        private EntityQuery<TransformComponent> _xformQuery;

        internal Dictionary<MapId, EntityUid> Maps { get; } = new();

        /// <summary>
        /// This hashset is used to try prevent MapId re-use. This is mainly for auto-assigned map ids.
        /// Loading a map with a specific id (e.g., the various mapping commands) may still result in an id being
        /// reused.
        /// </summary>
        protected HashSet<MapId> UsedIds = new();

        public override void Initialize()
        {
            base.Initialize();

            _fixturesQuery = GetEntityQuery<FixturesComponent>();
            _mapQuery = GetEntityQuery<MapComponent>();
            _gridQuery = GetEntityQuery<MapGridComponent>();
            _metaQuery = GetEntityQuery<MetaDataComponent>();
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
    [Obsolete("Use map creation or deletion events")]
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

    /// <summary>
    ///     Event raised whenever a map is created.
    /// </summary>
    public readonly record struct MapCreatedEvent(EntityUid Uid, MapId MapId);

    /// <summary>
    ///     Event raised whenever a map is removed.
    /// </summary>
    public readonly record struct MapRemovedEvent(EntityUid Uid, MapId MapId);

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
        public TileChangedEvent(Entity<MapGridComponent> entity, TileRef newTile, Tile oldTile, Vector2i chunkIndex)
        {
            Entity = entity;
            NewTile = newTile;
            OldTile = oldTile;
            ChunkIndex = chunkIndex;
        }

        /// <summary>
        /// Was the tile previously empty or is it now empty.
        /// </summary>
        public bool EmptyChanged => OldTile.IsEmpty != NewTile.Tile.IsEmpty;

        /// <summary>
        ///     Entity of the grid with the tile-change. TileRef stores the GridId.
        /// </summary>
        public readonly Entity<MapGridComponent> Entity;

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
