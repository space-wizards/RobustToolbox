using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

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

        /// <summary>
        /// Converts the specified index to a bitmask with the specified chunksize.
        /// </summary>
        [Pure]
        public static ulong ToBitmask(Vector2i index, byte chunkSize = 8)
        {
            DebugTools.Assert(chunkSize <= 8);
            DebugTools.Assert((index.X + index.Y * chunkSize) < 64);

            return (ulong) 1 << (index.X + index.Y * chunkSize);
        }

        /// <returns>True if the specified bitflag is set for this index.</returns>
        [Pure]
        public static bool FromBitmask(Vector2i index, ulong bitmask, byte chunkSize = 8)
        {
            var flag = ToBitmask(index, chunkSize);

            return (flag & bitmask) == flag;
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
    /// Raised directed at the grid when tiles are changed locally or remotely.
    /// </summary>
    [ByRefEvent]
    public readonly record struct TileChangedEvent
    {
        /// <inheritdoc cref="TileChangedEvent(Entity{MapGridComponent}, Tile, Tile, Vector2i, Vector2i)"/>
        public TileChangedEvent(Entity<MapGridComponent> entity, TileRef newTile, Tile oldTile, Vector2i chunkIndex)
            : this(entity, newTile.Tile, oldTile, chunkIndex, newTile.GridIndices) { }

        /// <summary>
        /// Creates a new instance of this event for a single changed tile.
        /// </summary>
        /// <param name="entity">The grid entity containing the changed tile(s)</param>
        /// <param name="newTile">New tile that replaced the old one.</param>
        /// <param name="oldTile">Old tile that was replaced.</param>
        /// <param name="chunkIndex">The index of the grid-chunk that this tile belongs to.</param>
        /// <param name="gridIndices">The positional indices of this tile on the grid.</param>
        public TileChangedEvent(Entity<MapGridComponent> entity, Tile newTile, Tile oldTile, Vector2i chunkIndex, Vector2i gridIndices)
        {
            Entity = entity;
            Changes = [new TileChangedEntry(newTile, oldTile, chunkIndex, gridIndices)];
        }

        /// <summary>
        /// Creates a new instance of this event for multiple changed tiles.
        /// </summary>
        public TileChangedEvent(Entity<MapGridComponent> entity, TileChangedEntry[] changes)
        {
            Entity = entity;
            Changes = changes;
        }

        /// <summary>
        /// Entity of the grid with the tile-change. TileRef stores the GridId.
        /// </summary>
        public readonly Entity<MapGridComponent> Entity;

        /// <summary>
        /// An array of all the tiles that were changed.
        /// </summary>
        public readonly TileChangedEntry[] Changes;
    }

    /// <summary>
    /// Data about a single tile that was changed as part of a <see cref="TileChangedEvent"/>.
    /// </summary>
    /// <param name="NewTile">New tile that replaced the old one.</param>
    /// <param name="OldTile">Old tile that was replaced.</param>
    /// <param name="ChunkIndex">The index of the grid-chunk that this tile belongs to.</param>
    /// <param name="GridIndices">The positional indices of this tile on the grid.</param>
    public readonly record struct TileChangedEntry(Tile NewTile, Tile OldTile, Vector2i ChunkIndex, Vector2i GridIndices)
    {
        /// <summary>
        /// Was the tile previously empty or is it now empty.
        /// </summary>
        public bool EmptyChanged => OldTile.IsEmpty != NewTile.IsEmpty;
    }
}
