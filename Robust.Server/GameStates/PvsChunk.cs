using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

internal sealed class PvsChunk
{
    /// <summary>
    /// The root of this chunk. This should either be a map or a grid.
    /// </summary>
    public Entity<MetaDataComponent> Root;

    /// <summary>
    /// The map that this grid is on. Tis might be the same entity as <see cref="Root"/>.
    /// </summary>
    public Entity<MetaDataComponent> Map;

    /// <summary>
    /// If true, then some entity was added or removed from this chunks and has to be reconstructed
    /// </summary>
    public bool Dirty { get; private set; } = true;

    /// <summary>
    /// Set of entities that are directly parented to this grid.
    /// </summary>
    public HashSet<EntityUid> Children = new();

    /// <summary>
    /// Sorted list of all entities on this chunk. The list is sorted based on their "proximity" to the root entity in
    /// the transform hierarchy. I.e., it will list all entities that are directly parented to the grid before listing
    /// any entities that are parented to those entities and so on.
    /// </summary>
    /// <remarks>
    /// This already includes <see cref="Map"/>, <see cref="Root"/>, and <see cref="Children"/>
    /// </remarks>
    public readonly List<Entity<MetaDataComponent>> Contents = new();

    /// <summary>
    /// The unique location identifier for this chunk.
    /// </summary>
    public PvsChunkLocation Location;

    /// <summary>
    /// The location of the centre of this chunk, relative to the <see cref="Root"/>
    /// </summary>
    public Vector2 Centre;

    /// <summary>
    /// The map position of the chunk's centre during the last PVS update.
    /// </summary>
    public MapCoordinates Position;

    // These are only used while populating the chunk. They aren't local variables because the chunks are pooled, so
    // the same chunk can be repopulated more than once.
    private List<HashSet<EntityUid>> _childSets = new();
    private List<HashSet<EntityUid>> _nextChildSets = new();

    /// <summary>
    /// Effective "count" of <see cref="Contents"/> that should be used to limit the entities in a chunk to a lower
    /// "level of detail". If this is used, then the chunk will effectively only contain entities that are directly
    /// parented to the grid.
    /// </summary>
    public int LowLodCount;

    public void Initialize(PvsChunkLocation location,
        EntityQuery<MetaDataComponent> meta,
        EntityQuery<TransformComponent> xform)
    {
        DebugTools.Assert(Dirty);

        if (Root != default)
            throw new InvalidOperationException($"Chunk has not been cleared.");

        Location = location;
        var root = Location.Uid;

        if (!meta.TryGetComponent(root, out var rootMeta)
            || !xform.TryGetComponent(root, out var rootXform))
        {
            Wipe();
            throw new InvalidOperationException($"Root {root} does not exist");
        }
        Root = (root, rootMeta);

        if (!meta.TryGetComponent(rootXform.MapUid, out var mapMeta))
        {
            var rep = new EntityStringRepresentation(Root);
            Wipe();
            throw new InvalidOperationException($"Root {rep} does not exist on nay map!");
        }
        Map = new(rootXform.MapUid.Value, mapMeta);

        Centre = (Location.Indices + new Vector2(0.5f)) * PvsSystem.ChunkSize;
    }

    /// <summary>
    /// Populates the contents of this chunk. Returns false if some error occurs (e.g., contains deleted entities).
    /// </summary>
    public bool PopulateContents(EntityQuery<MetaDataComponent> meta, EntityQuery<TransformComponent> xform)
    {
#if DEBUG
        // Initial set of children should all be parented directly to the root entity.
        foreach (var c in Children)
        {
            DebugTools.AssertEqual(xform.GetComponent(c).ParentUid, Root.Owner);
        }
        DebugTools.Assert(Contents.Count == 0);
#endif

        // Recursively add all children
        var nextSetTotal = Children.Count;
        _childSets.Add(Children);
        while (nextSetTotal > 0)
        {
            Contents.EnsureCapacity(Contents.Count + nextSetTotal);
            nextSetTotal = 0;
            foreach (var childSet in _childSets)
            {
                foreach (var child in childSet)
                {
                    // TODO ARCH multi-component queries
                    if (!meta.TryGetComponent(child, out var childMeta)
                        || !xform.TryGetComponent(child, out var childXform))
                    {
                        DebugTools.Assert($"PVS chunk contains a deleted entity: {child}");
                        MarkDirty();
                        return false;
                    }

                    Contents.Add((child, childMeta));
                    childMeta.LastPvsLocation = Location;

                    var subCount = childXform._children.Count;
                    if (subCount == 0)
                        continue;

                    nextSetTotal += subCount;
                    _nextChildSets.Add(childXform._children);
                }
            }
            _childSets.Clear();
            (_childSets, _nextChildSets) = (_nextChildSets, _childSets);
        }
        ;
#if DEBUG
        var set = new HashSet<Entity<MetaDataComponent>>(Contents);
        DebugTools.Assert(set.Count == Contents.Count);
#endif
        _nextChildSets.Clear();
        Dirty = false;
        LowLodCount = Children.Count;

        return true;
    }

    public void MarkDirty()
    {
        if (Dirty)
            return;

        Dirty = true;
        Contents.Clear();
        _nextChildSets.Clear();
        _childSets.Clear();
    }

    public void Wipe()
    {
        Root = default;
        Map = default;
        Location = default;
        Children.Clear();
        MarkDirty();
    }

    public override string ToString()
    {
        return Map.Owner == Root.Owner
            ? $"map-{Root.Owner}-{Location.Indices}"
            : $"grid-{Root.Owner}-{Location.Indices}";
    }
}
