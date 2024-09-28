using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
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

    public bool UpdateQueued = false;

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
    public readonly List<ChunkEntity> Contents = new();

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

    /// <summary>
    /// The <see cref="Root"/>'s inverse world matrix.
    /// </summary>
    public Matrix3x2 InvWorldMatrix { get; set; }

    // These are only used while populating the chunk. They aren't local variables because the chunks are pooled, so
    // the same chunk can be repopulated more than once.
    private List<HashSet<EntityUid>> _childSets = new();
    private List<HashSet<EntityUid>> _nextChildSets = new();
    private List<ChunkEntity> _lowPriorityChildren = new();
    private List<ChunkEntity> _anchoredChildren = new();

    /// <summary>
    /// Effective "counts" of <see cref="Contents"/> that should be used to limit the number of entities in a chunk that
    /// get sent to players. This can be used to add a crude "level of detail" for distant chunks.
    /// The counts correspond to entities that are:
    /// <list type="bullet">
    /// <item>Directly attached to the <see cref="Root"/> and have the <see cref="MetaDataFlags.PvsPriority"/> flag.</item>
    /// <item>Directly attached to the <see cref="Root"/> and are anchored (or high priority).</item>
    /// <item>Directly attached to the <see cref="Root"/>.</item>
    /// <item>Directly attached to the <see cref="Root"/> and their direct children.</item>
    /// <item>All entities.</item>
    /// </list>
    /// <remarks>
    /// Note that the chunk will not be re-populated if an entity gets (un)anchored, or if their metadata flags changes.
    /// So if somebody anchors an occluder and it starts occluding, it won't become a high priority entity untill
    /// the chunk gets dirtied & rebuilt.
    /// </remarks>
    /// </summary>
    public readonly int[] LodCounts = new int[5];

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
        DebugTools.AssertEqual(Contents.Count, 0);
        DebugTools.AssertEqual(_childSets.Count, 0);
        DebugTools.AssertEqual(_nextChildSets.Count, 0);
        DebugTools.AssertEqual(_anchoredChildren.Count, 0);
        DebugTools.AssertEqual(_lowPriorityChildren.Count, 0);

        Contents.EnsureCapacity(Children.Count);
        _lowPriorityChildren.EnsureCapacity(Children.Count);
        var nextSetTotal = 0;

        // First, we add all high-priority children.
        foreach (var child in Children)
        {
            // TODO ARCH multi-component queries
            if (!meta.TryGetComponent(child, out var childMeta)
                || !xform.TryGetComponent(child, out var childXform)
                || childMeta.EntityLifeStage >= EntityLifeStage.Terminating)
            {
                DebugTools.Assert($"PVS chunk contains a delete or terminating entity: {child}");
                MarkDirty();
                return false;
            }

            childMeta.LastPvsLocation = Location;

            if ((childMeta.Flags & MetaDataFlags.PvsPriority) == MetaDataFlags.PvsPriority)
                Contents.Add(new ChunkEntity(child, childMeta));
            else if (childXform.Anchored)
                _anchoredChildren.Add(new(child, childMeta));
            else
                _lowPriorityChildren.Add(new(child, childMeta));

            var subCount = childXform._children.Count;
            if (subCount == 0)
                continue;

            nextSetTotal += subCount;
            _childSets.Add(childXform._children);
        }

        // Populate LoD counts
        LodCounts[0] = Contents.Count;
        LodCounts[1] = LodCounts[0] + _anchoredChildren.Count;
        LodCounts[2] = LodCounts[1] + _lowPriorityChildren.Count;
        LodCounts[3] = LodCounts[2] + nextSetTotal;

        // Next, add the lower priority children.
        Contents.AddRange(_anchoredChildren);
        Contents.AddRange(_lowPriorityChildren);
        _lowPriorityChildren.Clear();
        _anchoredChildren.Clear();

        // Next, we recursively add all grand-children
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
                        || !xform.TryGetComponent(child, out var childXform)
                        || childMeta.EntityLifeStage >= EntityLifeStage.Terminating)
                    {
                        DebugTools.Assert($"PVS chunk contains a delete or terminating entity: {child}");
                        MarkDirty();
                        return false;
                    }

                    childMeta.LastPvsLocation = Location;
                    Contents.Add(new(child, childMeta));

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

        LodCounts[4] = Contents.Count;
        Dirty = false;
        ValidateChunk(xform);
        return true;
    }

    [Conditional("DEBUG")]
    private void ValidateChunk(EntityQuery<TransformComponent> query)
    {
        DebugTools.Assert(LodCounts[0] <= LodCounts[1]);
        DebugTools.Assert(LodCounts[1] <= LodCounts[2]);
        DebugTools.Assert(LodCounts[2] <= LodCounts[3]);
        DebugTools.Assert(LodCounts[3] <= LodCounts[4]);
        DebugTools.AssertEqual(LodCounts[4], Contents.Count());

        DebugTools.AssertEqual(_childSets.Count, 0);
        DebugTools.AssertEqual(_nextChildSets.Count, 0);
        DebugTools.AssertEqual(_anchoredChildren.Count, 0);
        DebugTools.AssertEqual(_lowPriorityChildren.Count, 0);

        foreach (var c in Children)
        {
            DebugTools.AssertEqual(query.GetComponent(c).ParentUid, Root.Owner,
                "Direct child is not actually directly attached to the root.");
        }

        var set = new HashSet<EntityUid>(Contents.Count);
        set.Add(Root.Owner);
        set.Add(Map.Owner);
        foreach (var child in Contents)
        {
            var parent = query.GetComponent(child.Uid).ParentUid;
            DebugTools.Assert(set.Contains(parent),
                "A child's parent is not in the chunk, or is not listed first.");
            DebugTools.Assert(set.Add(child.Uid), "Child appears more than once in the chunk.");
        }
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

    public readonly struct ChunkEntity(EntityUid uid, MetaDataComponent meta)
    {
        public readonly EntityUid Uid = uid;
        public readonly PvsIndex Ptr = meta.PvsData;
        public readonly MetaDataComponent Meta = meta;
    }
}
