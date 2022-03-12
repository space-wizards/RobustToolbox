using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Shared.Map
{
    /// <summary>
    ///     This manages all of the grids in the world.
    /// </summary>
    public interface IMapManager : IPauseManager
    {
        IEnumerable<IMapGrid> GetAllGrids();

        /// <summary>
        ///     Should the OnTileChanged event be suppressed? This is useful for initially loading the map
        ///     so that you don't spam an event for each of the million station tiles.
        /// </summary>
        bool SuppressOnTileChanged { get; set; }

        /// <summary>
        /// Get the set of grids that have moved on this map in this tick.
        /// </summary>
        HashSet<IMapGrid> GetMovedGrids(MapId mapId);

        /// <summary>
        /// Clear the set of grids that have moved on this map in this tick.
        /// </summary>
        void ClearMovedGrids(MapId mapId);

        /// <summary>
        ///     Starts up the map system.
        /// </summary>
        void Initialize();

        void Shutdown();
        void Startup();

        void Restart();

        /// <summary>
        ///     Creates a new map.
        /// </summary>
        /// <param name="mapId">
        ///     If provided, the new map will use this ID. If not provided, a new ID will be selected automatically.
        /// </param>
        /// <returns>The new map.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Throw if an explicit ID for the map or default grid is passed and a map or grid with the specified ID already exists, respectively.
        /// </exception>
        MapId CreateMap(MapId? mapId = null);

        /// <summary>
        ///     Check whether a map with specified ID exists.
        /// </summary>
        /// <param name="mapId">The map ID to check existence of.</param>
        /// <returns>True if the map exists, false otherwise.</returns>
        bool MapExists(MapId mapId);

        /// <summary>
        /// Creates a new entity, then sets it as the map entity.
        /// </summary>
        /// <returns>Newly created entity.</returns>
        EntityUid CreateNewMapEntity(MapId mapId);

        /// <summary>
        /// Sets the MapEntity(root node) for a given map. If an entity is already set, it will be deleted
        /// before the new one is set.
        /// </summary>
        void SetMapEntity(MapId mapId, EntityUid newMapEntityId);

        /// <summary>
        /// Returns the map entity ID for a given map.
        /// </summary>
        EntityUid GetMapEntityId(MapId mapId);

        /// <summary>
        /// Replaces GetMapEntity()'s throw-on-failure semantics.
        /// </summary>
        EntityUid GetMapEntityIdOrThrow(MapId mapId);

        IEnumerable<MapId> GetAllMapIds();

        void DeleteMap(MapId mapId);

        IMapGrid CreateGrid(MapId currentMapId, GridId? gridId = null, ushort chunkSize = 16);
        IMapGrid GetGrid(GridId gridId);
        bool TryGetGrid(GridId gridId, [NotNullWhen(true)] out IMapGrid? grid);
        bool GridExists(GridId gridId);
        IEnumerable<IMapGrid> GetAllMapGrids(MapId mapId);

        /// <summary>
        /// Attempts to find the map grid under the map location.
        /// </summary>
        /// <remarks>
        /// This method will never return the map's default grid.
        /// </remarks>
        /// <param name="mapId">Map to search.</param>
        /// <param name="worldPos">Location on the map to check for a grid.</param>
        /// <param name="grid">Grid that was found, if any.</param>
        /// <returns>Returns true when a grid was found under the location.</returns>
        bool TryFindGridAt(MapId mapId, Vector2 worldPos, [NotNullWhen(true)] out IMapGrid? grid);

        /// <summary>
        /// Attempts to find the map grid under the map location.
        /// </summary>
        /// <remarks>
        /// This method will never return the map's default grid.
        /// </remarks>
        /// <param name="mapCoordinates">Location on the map to check for a grid.</param>
        /// <param name="grid">Grid that was found, if any.</param>
        /// <returns>Returns true when a grid was found under the location.</returns>
        bool TryFindGridAt(MapCoordinates mapCoordinates, [NotNullWhen(true)] out IMapGrid? grid);

        void FindGridsIntersectingEnumerator(MapId mapId, Box2 worldAabb, out FindGridsEnumerator enumerator, bool approx = false);

        /// <summary>
        /// Returns the grids intersecting this AABB.
        /// </summary>
        /// <param name="mapId">The relevant MapID</param>
        /// <param name="worldAabb">The AABB to intersect</param>
        /// <param name="approx">Set to false if you wish to accurately get the grid bounds per-tile.</param>
        /// <returns></returns>
        IEnumerable<IMapGrid> FindGridsIntersecting(MapId mapId, Box2 worldAabb, bool approx = false);

        /// <summary>
        /// <see cref="FindGridsIntersecting(Robust.Shared.Map.MapId,Robust.Shared.Maths.Box2,bool)"/>
        /// </summary>
        IEnumerable<IMapGrid> FindGridsIntersecting(
            MapId mapId,
            Box2 worldAABB,
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<PhysicsComponent> physicsQuery,
            bool approx = false);

        /// <summary>
        /// Returns the grids intersecting this AABB.
        /// </summary>
        /// <param name="mapId">The relevant MapID</param>
        /// <param name="worldArea">The AABB to intersect</param>
        /// <param name="approx">Set to false if you wish to accurately get the grid bounds per-tile.</param>
        IEnumerable<IMapGrid> FindGridsIntersecting(MapId mapId, Box2Rotated worldArea, bool approx = false);

        void DeleteGrid(GridId gridId);

        /// <summary>
        ///     A tile is being modified.
        /// </summary>
        [Obsolete("Subscribe to TileChangedEvent on the event bus.")]
        event EventHandler<TileChangedEventArgs> TileChanged;

        [Obsolete("Subscribe to GridStartupEvent on the event bus.")]
        event GridEventHandler OnGridCreated;

        [Obsolete("Subscribe to GridRemovalEvent on the event bus.")]
        event GridEventHandler OnGridRemoved;

        /// <summary>
        ///     A Grid was modified.
        /// </summary>
        [Obsolete("Subscribe to GridModifiedEvent on the event bus.")]
        event EventHandler<GridChangedEventArgs> GridChanged;

        /// <summary>
        ///     A new map has been created.
        /// </summary>
        [Obsolete("Subscribe to MapChangedEvent on the event bus, and check if Created is true.")]
        event EventHandler<MapEventArgs> MapCreated;

        /// <summary>
        ///     An existing map has been destroyed.
        /// </summary>
        [Obsolete("Subscribe to MapChangedEvent on the event bus, and check if Destroyed is true.")]
        event EventHandler<MapEventArgs> MapDestroyed;

        bool HasMapEntity(MapId mapId);

        bool IsGrid(EntityUid uid);
        bool IsMap(EntityUid uid);

        MapId NextMapId();
        EntityUid GetGridEuid(GridId id);
        IMapGridComponent GetGridComp(GridId id);
        IMapGridComponent GetGridComp(EntityUid euid);
        IMapGrid GetGrid(EntityUid euid);
        bool TryGetGrid(EntityUid euid, [NotNullWhen(true)] out IMapGrid? grid);
        bool GridExists(EntityUid euid);
        IEnumerable<IMapComponent> GetAllMapComponents();
    }
}
