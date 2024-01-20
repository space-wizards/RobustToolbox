using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;

namespace Robust.Shared.Map
{
    public delegate bool GridCallback(EntityUid uid, MapGridComponent grid);

    public delegate bool GridCallback<TState>(EntityUid uid, MapGridComponent grid, ref TState state);

    /// <summary>
    ///     This manages all of the grids in the world.
    /// </summary>
    public interface IMapManager
    {
        public const bool Approximate = false;
        public const bool IncludeMap = true;

        [Obsolete("Use EntityQuery<MapGridComponent>")]
        IEnumerable<MapGridComponent> GetAllGrids();

        /// <summary>
        ///     Should the OnTileChanged event be suppressed? This is useful for initially loading the map
        ///     so that you don't spam an event for each of the million station tiles.
        /// </summary>
        bool SuppressOnTileChanged { get; set; }

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
        /// <param name="updateChildren">Should we re-parent children from the old map to the new one, or delete them.</param>
        void SetMapEntity(MapId mapId, EntityUid newMapEntityId, bool updateChildren = true);

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
        Entity<MapGridComponent> CreateGridEntity(MapId currentMapId, GridCreateOptions? options = null);

        [Obsolete("Use GetComponent<MapGridComponent>(uid)")]
        MapGridComponent GetGrid(EntityUid gridId);

        [Obsolete("Use TryGetComponent(uid, out MapGridComponent? grid)")]
        bool TryGetGrid([NotNullWhen(true)] EntityUid? euid, [NotNullWhen(true)] out MapGridComponent? grid);

        [Obsolete("Use HasComponent<MapGridComponent>(uid)")]
        bool GridExists([NotNullWhen(true)] EntityUid? euid);

        IEnumerable<MapGridComponent> GetAllMapGrids(MapId mapId);

        IEnumerable<Entity<MapGridComponent>> GetAllGrids(MapId mapId);

        #region MapId

        public void FindGridsIntersecting(MapId mapId, IPhysShape shape, Transform transform,
            ref List<Entity<MapGridComponent>> grids, bool approx = Approximate, bool includeMap = IncludeMap);

        public void FindGridsIntersecting(MapId mapId, PolygonShape shape, Transform transform, GridCallback callback,
            bool approx = Approximate, bool includeMap = IncludeMap);

        public void FindGridsIntersecting(MapId mapId, Box2 worldAABB, GridCallback callback, bool approx = Approximate,
            bool includeMap = IncludeMap);

        public void FindGridsIntersecting<TState>(MapId mapId, Box2 worldAABB, ref TState state,
            GridCallback<TState> callback, bool approx = Approximate, bool includeMap = IncludeMap);

        public void FindGridsIntersecting(MapId mapId, Box2 worldAABB, ref List<Entity<MapGridComponent>> grids,
            bool approx = Approximate, bool includeMap = IncludeMap);

        public void FindGridsIntersecting(MapId mapId, Box2Rotated worldBounds, GridCallback callback,
            bool approx = Approximate,
            bool includeMap = IncludeMap);

        public void FindGridsIntersecting<TState>(MapId mapId, Box2Rotated worldBounds, ref TState state,
            GridCallback<TState> callback,
            bool approx = Approximate, bool includeMap = IncludeMap);

        public void FindGridsIntersecting(MapId mapId, Box2Rotated worldBounds, ref List<Entity<MapGridComponent>> grids,
            bool approx = Approximate, bool includeMap = IncludeMap);

        #endregion

        #region MapEnt

        public void FindGridsIntersecting(EntityUid mapEnt, PolygonShape shape, Transform transform, GridCallback callback,
            bool approx = Approximate, bool includeMap = IncludeMap);

        public void FindGridsIntersecting<TState>(EntityUid mapEnt, IPhysShape shape, Transform transform,
            ref TState state, GridCallback<TState> callback, bool approx = Approximate, bool includeMap = IncludeMap);

        /// <summary>
        /// Returns true if any grids overlap the specified shapes.
        /// </summary>
        public void FindGridsIntersecting(EntityUid mapEnt, List<IPhysShape> shapes, Transform transform,
            ref List<Entity<MapGridComponent>> entities, bool approx = Approximate, bool includeMap = IncludeMap);

        public void FindGridsIntersecting(EntityUid mapEnt, IPhysShape shape, Transform transform,
            ref List<Entity<MapGridComponent>> grids, bool approx = Approximate, bool includeMap = IncludeMap);

        public void FindGridsIntersecting(EntityUid mapEnt, Box2 worldAABB, GridCallback callback,
            bool approx = Approximate, bool includeMap = IncludeMap);

        public void FindGridsIntersecting<TState>(EntityUid mapEnt, Box2 worldAABB, ref TState state,
            GridCallback<TState> callback, bool approx = Approximate, bool includeMap = IncludeMap);

        public void FindGridsIntersecting(EntityUid mapEnt, Box2 worldAABB, ref List<Entity<MapGridComponent>> grids,
            bool approx = Approximate, bool includeMap = IncludeMap);

        public void FindGridsIntersecting(EntityUid mapEnt, Box2Rotated worldBounds, GridCallback callback,
            bool approx = Approximate,
            bool includeMap = IncludeMap);

        public void FindGridsIntersecting<TState>(EntityUid mapEnt, Box2Rotated worldBounds, ref TState state,
            GridCallback<TState> callback,
            bool approx = Approximate, bool includeMap = IncludeMap);

        public void FindGridsIntersecting(EntityUid mapEnt, Box2Rotated worldBounds,
            ref List<Entity<MapGridComponent>> grids,
            bool approx = Approximate, bool includeMap = IncludeMap);

        #endregion

        #region TryFindGridAt

        public bool TryFindGridAt(
            EntityUid mapEnt,
            Vector2 worldPos,
            out EntityUid uid,
            [NotNullWhen(true)] out MapGridComponent? grid);

        /// <summary>
        /// Attempts to find the map grid under the map location.
        /// </summary>
        public bool TryFindGridAt(MapId mapId, Vector2 worldPos, out EntityUid uid,
            [NotNullWhen(true)] out MapGridComponent? grid);

        /// <summary>
        /// Attempts to find the map grid under the map location.
        /// </summary>
        public bool TryFindGridAt(MapCoordinates mapCoordinates, out EntityUid uid,
            [NotNullWhen(true)] out MapGridComponent? grid);

        #endregion

        #region Obsolete

        [Obsolete]
        public bool TryFindGridAt(MapId mapId, Vector2 worldPos, EntityQuery<TransformComponent> query, out EntityUid uid, [NotNullWhen(true)] out MapGridComponent? grid)
        {
            return TryFindGridAt(mapId, worldPos, out uid, out grid);
        }

        [Obsolete]
        public IEnumerable<MapGridComponent> FindGridsIntersecting(MapId mapId, Box2 worldAabb, bool approx = false, bool includeMap = true)
        {
            var grids = new List<Entity<MapGridComponent>>();
            FindGridsIntersecting(mapId, worldAabb, ref grids, approx, includeMap);

            foreach (var grid in grids)
            {
                yield return grid.Comp;
            }
        }

        [Obsolete]
        public IEnumerable<MapGridComponent> FindGridsIntersecting(MapId mapId, Box2Rotated worldArea, bool approx = false, bool includeMap = true)
        {
            var grids = new List<Entity<MapGridComponent>>();
            FindGridsIntersecting(mapId, worldArea, ref grids, approx, includeMap);

            foreach (var grid in grids)
            {
                yield return grid.Comp;
            }
        }

        #endregion

        void DeleteGrid(EntityUid euid);

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

        // TODO rename this to actually be descriptive or just remove it.
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
