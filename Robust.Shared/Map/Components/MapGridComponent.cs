using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Map.Components;

/// <summary>
/// Component marking a grid entity, which are the "floor" in space other entities can be placed on.
/// Grids can have their own physics and move around in space while players or other entities move on them.
/// Examples for grids are the station, shuttles or asteroids.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MapGridComponent : Component
{
    // This field is used for deserialization internally in the map loader.
    // If you want to remove this, you would have to restructure the map save file.
    [DataField("index")]
    internal int GridIndex;
    // the grid section now writes the grid's EntityUID. as long as existing maps get updated (just a load+save),
    // this can be removed

    [DataField]
    internal ushort ChunkSize = 16;

    [ViewVariables]
    public int ChunkCount => Chunks.Count;

    /// <summary>
    /// The length of the side of a square tile in world units.
    /// </summary>
    [DataField]
    public ushort TileSize { get; internal set; } = 1;

    /// <summary>
    /// A square the size of one tile.
    /// </summary>
    public Vector2 TileSizeVector => new(TileSize, TileSize);

    /// <summary>
    /// A square the size of a quarter tile.
    /// </summary>
    public Vector2 TileSizeHalfVector => new(TileSize / 2f, TileSize / 2f);

    [ViewVariables]
    internal readonly List<(GameTick tick, Vector2i indices)> ChunkDeletionHistory = new();

    /// <summary>
    /// Last game tick that the map was modified.
    /// </summary>
    [ViewVariables]
    public GameTick LastTileModifiedTick { get; internal set; }

    /// <summary>
    /// Map DynamicTree proxy to lookup for grid intersection.
    /// </summary>
    internal DynamicTree.Proxy MapProxy = DynamicTree.Proxy.Free;

    /// <summary>
    /// Grid chunks than make up this grid.
    /// </summary>
    [DataField]
    internal Dictionary<Vector2i, MapChunk> Chunks = new();

    [ViewVariables]
    public Box2 LocalAABB { get; internal set; }

    /// <summary>
    /// Set to enable or disable grid splitting.
    /// You must ensure you handle this properly and check for splits afterwards if relevant!
    /// </summary>
    [DataField]
    public bool CanSplit = true;

    /// <returns>True if the specified chunk exists on this grid.</returns>
    [Pure]
    public bool HasChunk(Vector2i indices)
    {
        return Chunks.ContainsKey(indices);
    }
}

/// <summary>
/// Serialized state of a <see cref="MapGridComponentState"/>.
/// </summary>
[Serializable, NetSerializable]
internal sealed class MapGridComponentState(ushort chunkSize, Dictionary<Vector2i, ChunkDatum> fullGridData, GameTick lastTileModifiedTick) : ComponentState
{
    /// <summary>
    /// The size of the chunks in the map grid.
    /// </summary>
    public ushort ChunkSize = chunkSize;

    /// <summary>
    /// Networked chunk data containing the full grid state.
    /// </summary>
    public Dictionary<Vector2i, ChunkDatum> FullGridData = fullGridData;

    /// <summary>
    /// Last game tick that the tile on the grid was modified.
    /// </summary>
    public GameTick LastTileModifiedTick = lastTileModifiedTick;
}

/// <summary>
/// Serialized state of a <see cref="MapGridComponentState"/>.
/// </summary>
[Serializable, NetSerializable]
internal sealed class MapGridComponentDeltaState(ushort chunkSize, Dictionary<Vector2i, ChunkDatum>? chunkData, GameTick lastTileModifiedTick)
    : ComponentState, IComponentDeltaState<MapGridComponentState>
{
    /// <summary>
    /// The size of the chunks in the map grid.
    /// </summary>
    public readonly ushort ChunkSize = chunkSize;

    /// <summary>
    /// Networked chunk data.
    /// </summary>
    public readonly Dictionary<Vector2i, ChunkDatum>? ChunkData = chunkData;

    /// <summary>
    /// Last game tick that the tile on the grid was modified.
    /// </summary>
    public GameTick LastTileModifiedTick = lastTileModifiedTick;

    public void ApplyToFullState(MapGridComponentState state)
    {
        state.ChunkSize = ChunkSize;

        if (ChunkData == null)
            return;

        foreach (var (index, data) in ChunkData)
        {
            if (data.IsDeleted())
                state.FullGridData.Remove(index);
            else
                state.FullGridData[index] = data;
        }

        state.LastTileModifiedTick = LastTileModifiedTick;
    }

    public MapGridComponentState CreateNewFullState(MapGridComponentState state)
    {
        if (ChunkData == null)
            return new(ChunkSize, state.FullGridData, state.LastTileModifiedTick);

        var newState = new MapGridComponentState(ChunkSize, state.FullGridData.ShallowClone(), LastTileModifiedTick);
        ApplyToFullState(newState);
        return newState;
    }
}
