using System.Collections.Generic;
using JetBrains.Annotations;
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

            SubscribeLocalEvent<MapGridComponent, ComponentAdd>(OnGridAdd);
            SubscribeLocalEvent<MapGridComponent, ComponentInit>(OnGridInit);
            SubscribeLocalEvent<MapGridComponent, ComponentStartup>(OnGridStartup);
            SubscribeLocalEvent<MapGridComponent, ComponentShutdown>(OnGridRemove);
        }

        private void OnGridAdd(EntityUid uid, MapGridComponent component, ComponentAdd args)
        {
            // GridID is not set yet so we don't include it.
            var msg = new GridAddEvent(uid);
            EntityManager.EventBus.RaiseLocalEvent(uid, msg);
        }

        private void OnGridInit(EntityUid uid, MapGridComponent component, ComponentInit args)
        {
            var msg = new GridInitializeEvent(uid, component.GridIndex);
            EntityManager.EventBus.RaiseLocalEvent(uid, msg);
        }

        private void OnGridStartup(EntityUid uid, MapGridComponent component, ComponentStartup args)
        {
            var msg = new GridStartupEvent(uid, component.GridIndex);
            EntityManager.EventBus.RaiseLocalEvent(uid, msg);
        }

        private void OnGridRemove(EntityUid uid, MapGridComponent component, ComponentShutdown args)
        {
            EntityManager.EventBus.RaiseLocalEvent(uid, new GridRemovalEvent(uid, component.GridIndex));
            MapManager.OnComponentRemoved(component);
        }
    }

    public sealed class GridStartupEvent : EntityEventArgs
    {
        public EntityUid EntityUid { get; }
        public GridId GridId { get; }

        public GridStartupEvent(EntityUid uid, GridId gridId)
        {
            EntityUid = uid;
            GridId = gridId;
        }
    }

    public sealed class GridRemovalEvent : EntityEventArgs
    {
        public EntityUid EntityUid { get; }
        public GridId GridId { get; }

        public GridRemovalEvent(EntityUid uid, GridId gridId)
        {
            EntityUid = uid;
            GridId = gridId;
        }
    }

    /// <summary>
    /// Raised whenever a grid is being initialized.
    /// </summary>
    public sealed class GridInitializeEvent : EntityEventArgs
    {
        public EntityUid EntityUid { get; }
        public GridId GridId { get; }

        public GridInitializeEvent(EntityUid uid, GridId gridId)
        {
            EntityUid = uid;
            GridId = gridId;
        }
    }

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
        public TileChangedEvent(TileRef newTile, Tile oldTile)
        {
            NewTile = newTile;
            OldTile = oldTile;
        }

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
