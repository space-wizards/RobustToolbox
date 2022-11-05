using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Map.Components;
using System.Linq;

namespace Robust.Shared.GameObjects
{
    [UsedImplicitly]
    public abstract partial class SharedMapSystem : EntitySystem
    {
        [Dependency] protected readonly IMapManager MapManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MapComponent, ComponentAdd>(OnMapAdd);
            SubscribeLocalEvent<MapComponent, ComponentInit>(OnMapInit);
            SubscribeLocalEvent<MapComponent, ComponentShutdown>(OnMapRemoved);
            SubscribeLocalEvent<MapComponent, ComponentHandleState>(OnMapHandleState);
            SubscribeLocalEvent<MapComponent, ComponentGetState>(OnMapGetState);

            SubscribeLocalEvent<MapGridComponent, ComponentAdd>(OnGridAdd);
            SubscribeLocalEvent<MapGridComponent, ComponentInit>(OnGridInit);
            SubscribeLocalEvent<MapGridComponent, ComponentStartup>(OnGridStartup);
            SubscribeLocalEvent<MapGridComponent, ComponentShutdown>(OnGridRemove);

            SubscribeLocalEvent<MapLightComponent, ComponentGetState>(OnMapLightGetState);
            SubscribeLocalEvent<MapLightComponent, ComponentHandleState>(OnMapLightHandleState);
        }

        private void OnMapHandleState(EntityUid uid, MapComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not MapComponentState state)
                return;

            component.WorldMap = state.MapId;
            component.LightingEnabled = state.LightingEnabled;
            var xformQuery = GetEntityQuery<TransformComponent>();

            xformQuery.GetComponent(uid).ChangeMapId(state.MapId, xformQuery);
        }

        private void OnMapGetState(EntityUid uid, MapComponent component, ref ComponentGetState args)
        {
            args.State = new MapComponentState(component.WorldMap, component.LightingEnabled);
        }

        protected abstract void OnMapAdd(EntityUid uid, MapComponent component, ComponentAdd args);

        private void OnMapInit(EntityUid uid, MapComponent component, ComponentInit args)
        {
            var msg = new MapChangedEvent(component.WorldMap, true);
            RaiseLocalEvent(uid, msg, true);
        }

        private void OnMapRemoved(EntityUid uid, MapComponent component, ComponentShutdown args)
        {
            var iMap = (IMapManagerInternal)MapManager;

            iMap.TrueDeleteMap(component.WorldMap);

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
            var msg = new GridInitializeEvent(uid);
            RaiseLocalEvent(uid, msg, true);
        }

        private void OnGridStartup(EntityUid uid, MapGridComponent component, ComponentStartup args)
        {
            var msg = new GridStartupEvent(uid);
            RaiseLocalEvent(uid, msg, true);
        }

        private void OnGridRemove(EntityUid uid, MapGridComponent component, ComponentShutdown args)
        {
            RaiseLocalEvent(uid, new GridRemovalEvent(uid), true);

            if (uid == EntityUid.Invalid)
                return;

            if (!MapManager.GridExists(uid))
                return;

            MapManager.DeleteGrid(uid);
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
        public IMapGrid Grid { get; }

        /// <summary>
        /// Set of tiles that were modified.
        /// </summary>
        public IReadOnlyCollection<(Vector2i position, Tile tile)> Modified { get; }

        /// <summary>
        ///     Creates a new instance of this class.
        /// </summary>
        public GridModifiedEvent(IMapGrid grid, IReadOnlyCollection<(Vector2i position, Tile tile)> modified)
        {
            Grid = grid;
            Modified = modified;
        }
    }
}
