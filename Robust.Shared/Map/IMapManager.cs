using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Robust.Shared.Map
{
    public delegate bool GridCallback(MapGridComponent grid);

    public delegate bool GridCallback<TState>(MapGridComponent grid, ref TState state);

    /// <summary>
    ///     This manages all of the grids in the world.
    /// </summary>
    public interface IMapManager
    {
        IEnumerable<MapGridComponent> GetAllGrids();

        /// <summary>
        ///     Should the OnTileChanged event be suppressed? This is useful for initially loading the map
        ///     so that you don't spam an event for each of the million station tiles.
        /// </summary>
        bool SuppressOnTileChanged { get; set; }

        /// <summary>
        /// Get the set of grids that have moved on this map in this tick.
        /// </summary>
        HashSet<MapGridComponent> GetMovedGrids(MapId mapId);

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

        // ReSharper disable once MethodOverloadWithOptionalParameter
        MapGridComponent CreateGrid(MapId currentMapId, ushort chunkSize = 16);
        MapGridComponent CreateGrid(MapId currentMapId, in GridCreateOptions options);
        MapGridComponent CreateGrid(MapId currentMapId);
        MapGridComponent GetGrid(EntityUid gridId);
        bool TryGetGrid([NotNullWhen(true)] EntityUid? euid, [NotNullWhen(true)] out MapGridComponent? grid);
        bool GridExists([NotNullWhen(true)] EntityUid? euid);
        IEnumerable<MapGridComponent> GetAllMapGrids(MapId mapId);

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
        bool TryFindGridAt(MapId mapId, Vector2 worldPos, [NotNullWhen(true)] out MapGridComponent? grid);

        /// <summary>
        /// Attempts to find the map grid under the map location.
        /// </summary>
        /// <remarks>
        /// This method will never return the map's default grid.
        /// </remarks>
        /// <param name="mapCoordinates">Location on the map to check for a grid.</param>
        /// <param name="grid">Grid that was found, if any.</param>
        /// <returns>Returns true when a grid was found under the location.</returns>
        bool TryFindGridAt(MapCoordinates mapCoordinates, [NotNullWhen(true)] out MapGridComponent? grid);

        void FindGridsIntersectingApprox(MapId mapId, Box2 worldAABB, GridCallback callback);

        void FindGridsIntersectingApprox<TState>(MapId mapId, Box2 worldAABB, ref TState state, GridCallback<TState> callback);

        /// <summary>
        /// Returns the grids intersecting this AABB.
        /// </summary>
        /// <param name="mapId">The relevant MapID</param>
        /// <param name="worldAabb">The AABB to intersect</param>
        /// <param name="approx">Set to false if you wish to accurately get the grid bounds per-tile.</param>
        /// <returns></returns>
        IEnumerable<MapGridComponent> FindGridsIntersecting(MapId mapId, Box2 worldAabb, bool approx = false);

        /// <summary>
        /// Returns the grids intersecting this AABB.
        /// </summary>
        /// <param name="mapId">The relevant MapID</param>
        /// <param name="worldArea">The AABB to intersect</param>
        /// <param name="approx">Set to false if you wish to accurately get the grid bounds per-tile.</param>
        IEnumerable<MapGridComponent> FindGridsIntersecting(MapId mapId, Box2Rotated worldArea, bool approx = false);

        void DeleteGrid(EntityUid euid);

        /// <summary>
        ///     A tile is being modified.
        /// </summary>
        [Obsolete("Subscribe to TileChangedEvent on the event bus.")]
        event EventHandler<TileChangedEventArgs> TileChanged;

        bool HasMapEntity(MapId mapId);

        bool IsGrid(EntityUid uid);
        bool IsMap(EntityUid uid);

        [Obsolete("Whatever this is used for, it is a terrible idea. Create a new map and get it's MapId.")]
        MapId NextMapId();
        MapGridComponent GetGridComp(EntityUid euid);

        //
        // Pausing functions
        //

        void SetMapPaused(MapId mapId, bool paused);

        void DoMapInitialize(MapId mapId);

        void AddUninitializedMap(MapId mapId);

        [Pure]
        bool IsMapPaused(MapId mapId);

        [Pure]
        bool IsMapInitialized(MapId mapId);
    }

    public struct GridCreateOptions
    {
        public static readonly GridCreateOptions Default = new()
        {
            ChunkSize = 16
        };

        public ushort ChunkSize;
    }
}
