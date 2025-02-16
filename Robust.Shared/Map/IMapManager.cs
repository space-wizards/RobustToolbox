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

        [Obsolete("Use MapSystem")]
        MapId CreateMap(MapId? mapId = null);

        /// <summary>
        ///     Check whether a map with specified ID exists.
        /// </summary>
        /// <param name="mapId">The map ID to check existence of.</param>
        /// <returns>True if the map exists, false otherwise.</returns>
        [Obsolete("Use MapSystem")]
        bool MapExists([NotNullWhen(true)] MapId? mapId);

        /// <summary>
        /// Returns the map entity ID for a given map, or an invalid entity Id if the map does not exist.
        /// </summary>
        [Obsolete("Use MapSystem")]
        EntityUid GetMapEntityId(MapId mapId);

        /// <summary>
        /// Replaces GetMapEntity()'s throw-on-failure semantics.
        /// </summary>
        [Obsolete("Use MapSystem")]
        EntityUid GetMapEntityIdOrThrow(MapId mapId);

        [Obsolete("Use MapSystem")]
        IEnumerable<MapId> GetAllMapIds();

        [Obsolete("Use MapSystem")]
        void DeleteMap(MapId mapId);

        // ReSharper disable once MethodOverloadWithOptionalParameter
        MapGridComponent CreateGrid(MapId currentMapId, ushort chunkSize = 16);
        MapGridComponent CreateGrid(MapId currentMapId, in GridCreateOptions options);
        MapGridComponent CreateGrid(MapId currentMapId);
        Entity<MapGridComponent> CreateGridEntity(MapId currentMapId, GridCreateOptions? options = null);
        Entity<MapGridComponent> CreateGridEntity(EntityUid map, GridCreateOptions? options = null);

        IEnumerable<MapGridComponent> GetAllMapGrids(MapId mapId);

        IEnumerable<Entity<MapGridComponent>> GetAllGrids(MapId mapId);

        #region MapId

        public void FindGridsIntersecting<T>(MapId mapId, T shape, Transform transform,
            ref List<Entity<MapGridComponent>> grids, bool approx = Approximate, bool includeMap = IncludeMap) where T : IPhysShape;

        public void FindGridsIntersecting<T>(MapId mapId, T shape, Transform transform, GridCallback callback,
            bool approx = Approximate, bool includeMap = IncludeMap) where T : IPhysShape;

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

        public void FindGridsIntersecting<T>(EntityUid mapEnt, T shape, Transform transform, GridCallback callback,
            bool approx = Approximate, bool includeMap = IncludeMap) where T : IPhysShape;

        public void FindGridsIntersecting<T, TState>(EntityUid mapEnt, T shape, Transform transform,
            ref TState state, GridCallback<TState> callback, bool approx = Approximate, bool includeMap = IncludeMap) where T : IPhysShape;

        /// <summary>
        /// Returns true if any grids overlap the specified shapes.
        /// </summary>
        public void FindGridsIntersecting(EntityUid mapEnt, List<IPhysShape> shapes, Transform transform,
            ref List<Entity<MapGridComponent>> entities, bool approx = Approximate, bool includeMap = IncludeMap);

        public void FindGridsIntersecting<T>(EntityUid mapEnt, T shape, Transform transform,
            ref List<Entity<MapGridComponent>> grids, bool approx = Approximate, bool includeMap = IncludeMap) where T : IPhysShape;

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


        [Obsolete("Just delete the grid entity")]
        void DeleteGrid(EntityUid euid);

        [Obsolete("Use HasComp")]
        bool IsGrid(EntityUid uid);

        [Obsolete("Use HasComp")]
        bool IsMap(EntityUid uid);

        //
        // Pausing functions
        //

        [Obsolete("Use MapSystem")]
        void SetMapPaused(MapId mapId, bool paused);

        [Obsolete("Use MapSystem")]
        void DoMapInitialize(MapId mapId);

        [Obsolete("Use MapSystem")]
        bool IsMapPaused(MapId mapId);

        [Obsolete("Use MapSystem")]
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
