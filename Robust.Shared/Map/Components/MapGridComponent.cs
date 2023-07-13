using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Map.Events;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Map.Components
{
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class MapGridComponent : Component
    {
        // This field is used for deserialization internally in the map loader.
        // If you want to remove this, you would have to restructure the map save file.
        [DataField("index")] internal int GridIndex = 0;
        // the grid section now writes the grid's EntityUID. as long as existing maps get updated (just a load+save),
        // this can be removed

        [DataField("chunkSize")] internal ushort ChunkSize = 16;

        [ViewVariables]
        public int ChunkCount => Chunks.Count;

        /// <summary>
        ///     The length of the side of a square tile in world units.
        /// </summary>
        [DataField("tileSize")]
        public ushort TileSize { get; internal set; } = 1;

        public Vector2 TileSizeVector => new(TileSize, TileSize);

        public Vector2 TileSizeHalfVector => new(TileSize / 2f, TileSize / 2f);

        [ViewVariables] internal readonly List<(GameTick tick, Vector2i indices)> ChunkDeletionHistory = new();

        /// <summary>
        ///     Last game tick that the map was modified.
        /// </summary>
        [ViewVariables]
        public GameTick LastTileModifiedTick { get; internal set; }

        /// <summary>
        /// Map DynamicTree proxy to lookup for grid intersection.
        /// </summary>
        internal DynamicTree.Proxy MapProxy = DynamicTree.Proxy.Free;

        /// <summary>
        ///     Grid chunks than make up this grid.
        /// </summary>
        [DataField("chunks")]
        internal readonly Dictionary<Vector2i, MapChunk> Chunks = new();

        [ViewVariables]
        public Box2 LocalAABB { get; internal set; }

        /// <summary>
        /// Set to enable or disable grid splitting.
        /// You must ensure you handle this properly and check for splits afterwards if relevant!
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("canSplit")]
        public bool CanSplit = true;
    }

    /// <summary>
    ///     Serialized state of a <see cref="MapGridComponentState"/>.
    /// </summary>
    [Serializable, NetSerializable]
    internal sealed class MapGridComponentState : ComponentState, IComponentDeltaState
    {
        /// <summary>
        ///     The size of the chunks in the map grid.
        /// </summary>
        public ushort ChunkSize;

        /// <summary>
        /// Networked chunk data.
        /// </summary>
        public List<ChunkDatum>? ChunkData;

        /// <summary>
        /// Networked chunk data containing the full grid state.
        /// </summary>
        public Dictionary<Vector2i, Tile[]>? FullGridData;

        public bool FullState => FullGridData != null;

        /// <summary>
        ///     Constructs a new grid component delta state.
        /// </summary>
        public MapGridComponentState(ushort chunkSize, List<ChunkDatum>? chunkData)
        {
            ChunkSize = chunkSize;
            ChunkData = chunkData;
        }

        /// <summary>
        ///     Constructs a new full component state.
        /// </summary>
        public MapGridComponentState(ushort chunkSize, Dictionary<Vector2i, Tile[]> fullGridData)
        {
            ChunkSize = chunkSize;
            FullGridData = fullGridData;
        }

        public void ApplyToFullState(ComponentState fullState)
        {
            var state = (MapGridComponentState)fullState;
            DebugTools.Assert(!FullState && state.FullState);

            state.ChunkSize = ChunkSize;

            if (ChunkData == null)
                return;

            foreach (var data in ChunkData)
            {
                if (data.IsDeleted())
                    state.FullGridData!.Remove(data.Index);
                else
                    state.FullGridData![data.Index] = data.TileData;
            }
        }

        public ComponentState CreateNewFullState(ComponentState fullState)
        {
            var state = (MapGridComponentState)fullState;
            DebugTools.Assert(!FullState && state.FullState);

            var fullGridData = new Dictionary<Vector2i, Tile[]>(state.FullGridData!.Count);

            foreach (var (key, value) in state.FullGridData)
            {
                var arr = fullGridData[key] = new Tile[value.Length];
                Array.Copy(value, arr, value.Length);
            }

            var newState = new MapGridComponentState(ChunkSize, fullGridData);
            ApplyToFullState(newState);
            return newState;
        }
    }
}
