using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects
{
    [UsedImplicitly]
    internal abstract class SharedMapSystem : EntitySystem
    {
        [Dependency] protected readonly IMapManagerInternal MapManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MapComponent, ComponentInit>(OnMapInit);
            SubscribeLocalEvent<MapComponent, ComponentShutdown>(OnMapRemoved);

            SubscribeLocalEvent<MapGridComponent, ComponentAdd>(OnGridAdd);
            SubscribeLocalEvent<MapGridComponent, ComponentInit>(OnGridInit);
            SubscribeLocalEvent<MapGridComponent, ComponentStartup>(OnGridStartup);
            SubscribeLocalEvent<MapGridComponent, ComponentShutdown>(OnGridRemove);
            SubscribeLocalEvent<MapGridComponent, ComponentGetState>(OnGridGetState);
            SubscribeLocalEvent<MapGridComponent, ComponentHandleState>(OnGridHandleState);
        }

        private void OnGridGetState(EntityUid uid, MapGridComponent component, ref ComponentGetState args)
        {
            var chunkData = MapGridComponent.GetDeltaChunkData(component, args.FromTick);
            args.State = new MapGridComponentState(component.GridIndex, component.ChunkSize, chunkData);
        }

        private void OnGridHandleState(EntityUid uid, MapGridComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not MapGridComponentState state)
                return;

            component.GridIndex = state.GridIndex;
            component.ChunkSize = state.ChunkSize;

            if(state.ChunkDatums is not null)
                MapGridComponent.ApplyMapGridState(MapManager, component, state.ChunkDatums);
        }

        private void OnMapInit(EntityUid uid, MapComponent component, ComponentInit args)
        {
            var msg = new MapChangedEvent(component.WorldMap, true);
            RaiseLocalEvent(uid, msg, true);
        }

        private void OnMapRemoved(EntityUid uid, MapComponent component, ComponentShutdown args)
        {
            var msg = new MapChangedEvent(component.WorldMap, false);
            RaiseLocalEvent(uid, msg, true);
        }

        private void OnGridAdd(EntityUid uid, MapGridComponent component, ComponentAdd args)
        {
            // GridID is not set yet so we don't include it.
            var msg = new GridAddEvent(uid);
            RaiseLocalEvent(uid, msg, true);
        }

        private void OnGridInit(EntityUid uid, MapGridComponent component, ComponentInit args)
        {
#pragma warning disable CS0618
            var msg = new GridInitializeEvent(uid);
            RaiseLocalEvent(uid, msg, true);
        }

        private void OnGridStartup(EntityUid uid, MapGridComponent component, ComponentStartup args)
        {
#pragma warning disable CS0618
            var msg = new GridStartupEvent(uid);
            RaiseLocalEvent(uid, msg, true);
        }

        private void OnGridRemove(EntityUid uid, MapGridComponent component, ComponentShutdown args)
        {
            RaiseLocalEvent(uid, new GridRemovalEvent(uid), true);
            MapManager.OnComponentRemoved(component);
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
