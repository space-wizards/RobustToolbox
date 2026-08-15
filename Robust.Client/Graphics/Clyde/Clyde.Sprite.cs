using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Threading;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics.Clyde;

// this partial class contains code specific to querying, processing & sorting sprites.
internal partial class Clyde
{
    [Shared.IoC.Dependency] private IParallelManager _parMan = default!;
    private readonly RefList<SpriteData> _drawingSpriteList = new();
    private readonly SpriteSortBucket[] _spriteSortBuckets = new SpriteSortBucket[byte.MaxValue + 1];
    private const byte SpriteProcessingBatchSize = 32;

    private void GetSprites(MapId map, Viewport view, IEye eye, Box2Rotated worldBounds, out SpriteSortItem[] sortItems)
    {
        ProcessSpriteEntities(map, view, eye, worldBounds, _drawingSpriteList);

        var count = _drawingSpriteList.Count;
        sortItems = ArrayPool<SpriteSortItem>.Shared.Rent(count);

        using (_prof.Group("Build sprite sort"))
        {
            for (var i = 0; i < count; i++)
            {
                ref var data = ref _drawingSpriteList[i];
                sortItems[i] = new SpriteSortItem(
                    i,
                    data.Sprite.DrawDepth,
                    data.Sprite.RenderOrder,
                    data.SortY,
                    data.Uid);
            }
        }

        using (_prof.Group("Sort sprites"))
        {
            SortSprites(ref sortItems, count);
        }
    }

    private void SortSprites(ref SpriteSortItem[] items, int count)
    {
        Array.Clear(_spriteSortBuckets);

        if (count == 0)
            return;

        for (var i = 0; i < count; i++)
        {
            ref readonly var item = ref items[i];
            _spriteSortBuckets[item.DrawDepth].Count++;
        }

        var offset = 0;
        var highestBucket = 0;

        for (var i = 0; i < _spriteSortBuckets.Length; i++)
        {
            ref var bucket = ref _spriteSortBuckets[i];
            if (bucket.Count == 0)
                continue;

            bucket.Offset = offset;
            bucket.Next = offset;
            offset += bucket.Count;
            highestBucket = Math.Max(i, highestBucket);
        }

        var bucketed = ArrayPool<SpriteSortItem>.Shared.Rent(count);
        try
        {
            for (var i = 0; i < count; i++)
            {
                ref readonly var item = ref items[i];
                ref var bucket = ref _spriteSortBuckets[item.DrawDepth];
                bucketed[bucket.Next++] = item;
            }

            for (var i = 0; i < highestBucket; i++)
            {
                var bucket = _spriteSortBuckets[i];
                if (bucket.Count > 1)
                    Array.Sort(bucketed, bucket.Offset, bucket.Count, SpriteSortItemDepthComparer.Instance);
            }
        }
        catch
        {
            ArrayPool<SpriteSortItem>.Shared.Return(bucketed);
            throw;
        }

        ArrayPool<SpriteSortItem>.Shared.Return(items);
        items = bucketed;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ProcessSpriteEntities(MapId map, Viewport view, IEye eye, Box2Rotated worldBounds, RefList<SpriteData> list)
    {
        var query = _entityManager.GetEntityQuery<TransformComponent>();
        var viewScale = eye.Scale * view.RenderScale * new Vector2(EyeManager.PixelsPerMeter, -EyeManager.PixelsPerMeter);
        var treeData = new BatchData()
        {
            Sys = _entityManager.EntitySysManager.GetEntitySystem<TransformSystem>(),
            Query = query,
            ViewRotation = eye.Rotation,
            ViewScale = viewScale,
            PreScaleViewOffset = view.Size / 2f / viewScale,
            ViewPosition = eye.Position.Position + eye.Offset
        };

        // We need to batch the actual tree query, or alternatively we need just get the list of sprites and then
        // parallelize the rotation & bounding box calculations.
        var index = 0;
        var added = 0;
        foreach (var (treeOwner, comp) in _spriteTreeSystem.GetIntersectingTrees(map, worldBounds))
        {
            var treeXform = query.GetComponent(treeOwner);
            var treePos = treeXform.LocalPosition;
            var bounds = _transformSystem.GetInvWorldMatrix(treeOwner).TransformBox(worldBounds);
            DebugTools.Assert(treeXform.MapUid == treeXform.ParentUid || !treeXform.ParentUid.IsValid());

            treePos += GetPixelSnapOffset(
                treePos,
                treeData.ViewPosition,
                treeData.ViewRotation,
                treeData.ViewScale,
                view.Size);

            treeData = treeData with
            {
                TreeOwner = treeOwner,
                TreePos = treePos,
                TreeRot = treeXform.LocalRotation,
                Sin = MathF.Sin((float)treeXform.LocalRotation),
                Cos = MathF.Cos((float)treeXform.LocalRotation),
            };

            using (_prof.Group("Query sprite tree"))
            {
                comp.Tree.QueryAabb(ref list,
                    static (ref RefList<SpriteData> state, in ComponentTreeEntry<SpriteComponent> value) =>
                    {
                        ref var entry = ref state.AllocAdd();
                        entry.Uid = value.Uid;
                        entry.Sprite = value.Component;
                        entry.Xform = value.Transform;
                        return true;
                    }, bounds, true);
            }

            // Get bounding boxes & world positions
            added = list.Count - index;
            using (_prof.Group("Process sprite bounds"))
            {
                if (added >= 2 * SpriteProcessingBatchSize)
                {
                    _parMan.ProcessNow(new SpriteProcessingJob
                    {
                        Renderer = this,
                        List = list,
                        StartIndex = index,
                        Batch = treeData,
                    }, added);
                }
                else if (added > 0)
                {
                    ProcessSprites(list, index, added, treeData);
                }
            }

            index += added;
        }
    }

    internal static Vector2 GetPixelSnapOffset(
        Vector2 worldPosition,
        Vector2 viewPosition,
        Angle viewRotation,
        Vector2 viewScale,
        Vector2 viewportSize)
    {
        var viewPositionRelative = viewRotation.RotateVec(worldPosition - viewPosition);
        var screenPosition = viewPositionRelative * viewScale + viewportSize / 2f;
        var screenOffset = screenPosition.Rounded() - screenPosition;
        var viewOffset = screenOffset / viewScale;
        return (-viewRotation).RotateVec(viewOffset);
    }

    /// <summary>
    ///     This function computes a sprites world position, rotation, and screen-space bounding box. The position &
    ///     rotation are required in general, but the bounding box is only really needed for y-sorting & if the
    ///     sprite has a post processing shader.
    /// </summary>
    private void ProcessSprites(
        RefList<SpriteData> list,
        int startIndex,
        int count,
        in BatchData batch)
    {
        for (int i = startIndex; i < startIndex + count; i++)
        {
            ref var data = ref list[i];
            DebugTools.Assert(data.Sprite.Visible);

            // To help explain the remainder of this function, it should be functionally equivalent to the following
            // three lines of code, but has been expanded & simplified to speed up the calculation:
            //
            // (data.WorldPos, data.WorldRot) = batch.Sys.GetWorldPositionRotation(data.Xform);
            // var spriteWorldBB = data.Sprite.CalculateRotatedBoundingBox(data.WorldPos, data.WorldRot, batch.ViewRotation);
            // data.SpriteScreenBB = Viewport.GetWorldToLocalMatrix().TransformBox(spriteWorldBB);

            var (pos, rot) = batch.Sys.GetRelativePositionRotation(data.Xform, batch.TreeOwner);
            pos = new Vector2(
                batch.TreePos.X + batch.Cos * pos.X - batch.Sin * pos.Y,
                batch.TreePos.Y + batch.Sin * pos.X + batch.Cos * pos.Y);

            rot += batch.TreeRot;
            data.WorldRot = rot;
            data.WorldPos = pos;

            var finalRotation = (float) (data.Sprite.NoRotation
                ? data.Sprite.Rotation
                : data.Sprite.Rotation + rot + batch.ViewRotation);

            // false for 99.9% of sprites
            if (data.Sprite.Offset != Vector2.Zero)
            {
                pos += data.Sprite.NoRotation
                    ? (-batch.ViewRotation).RotateVec(data.Sprite.Offset)
                    : rot.RotateVec(data.Sprite.Offset);
            }

            pos = batch.ViewRotation.RotateVec(pos - batch.ViewPosition);

            // special casing angle = n*pi/2 to avoid box rotation & bounding calculations doesn't seem to give significant speedups.
            data.SortY = TransformCenteredBox(
                _spriteSystem.GetLocalBounds((data.Uid, data.Sprite)),
                finalRotation,
                pos + batch.PreScaleViewOffset,
                batch.ViewScale).Top;
        }
    }

    /// <summary>
    /// This is effectively a specialized combination of a <see cref="Matrix3Helpers.TransformBox(Matrix3x2, in Box2)"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe Box2 TransformCenteredBox(in Box2 box, float angle, in Vector2 offset, in Vector2 scale)
    {
        var boxVec = Unsafe.As<Box2, Vector128<float>>(ref Unsafe.AsRef(in box));
        var (sinValue, cosValue) = MathF.SinCos(angle);
        var sin = Vector128.Create(sinValue);
        var cos = Vector128.Create(cosValue);
        var boxX = Vector128.Shuffle(boxVec, Vector128.Create(0, 0, 2, 2));
        var boxY = Vector128.Shuffle(boxVec, Vector128.Create(1, 3, 3, 1));

        var x = boxX * cos - boxY * sin;
        var y = boxX * sin + boxY * cos;
        var lbrt = SimdHelpers.GetAABB(x, y);

        // This function is for sprites, which flip the y-axis via the scale, so we need to flip t & b.
        DebugTools.Assert(scale.Y < 0);
        lbrt = Vector128.Shuffle(lbrt, Vector128.Create(0,3,2,1));

        var offsetVec = Unsafe.As<Vector2, Vector128<float>>(ref Unsafe.AsRef(in offset)); // upper undefined
        var scaleVec = Unsafe.As<Vector2, Vector128<float>>(ref Unsafe.AsRef(in scale)); // upper undefined
        offsetVec = Vector128.Shuffle(offsetVec, Vector128.Create(0, 1, 0, 1));
        scaleVec = Vector128.Shuffle(scaleVec, Vector128.Create(0, 1, 0, 1));

        // offset and scale box.
        // note that the scaling here is scaling the whole space, not jut the box. I.e., the centre of the box is changing
        lbrt = (lbrt + offsetVec) * scaleVec;
        return Unsafe.As<Vector128<float>, Box2>(ref lbrt);
    }

    private struct SpriteData
    {
        public EntityUid Uid;
        public SpriteComponent Sprite;
        public TransformComponent Xform;
        public Vector2 WorldPos;
        public Angle WorldRot;
        public float SortY;
    }

    private readonly struct SpriteProcessingJob : IParallelBulkRobustJob
    {
        public required Clyde Renderer { get; init; }
        public required RefList<SpriteData> List { get; init; }
        public required int StartIndex { get; init; }
        public required BatchData Batch { get; init; }

        public int BatchSize => SpriteProcessingBatchSize;
        public int MinimumBatchParallel => 1;

        public void ExecuteRange(int startIndex, int endIndex)
        {
            Renderer.ProcessSprites(List, StartIndex + startIndex, endIndex - startIndex, Batch);
        }
    }

    private readonly struct BatchData
    {
        public TransformSystem Sys { get; init; }
        public EntityQuery<TransformComponent> Query { get; init; }
        public Angle ViewRotation { get; init; }
        public Vector2 ViewScale { get; init; }
        public Vector2 PreScaleViewOffset { get; init; }
        public Vector2 ViewPosition { get; init; }
        public EntityUid TreeOwner { get; init; }
        public Vector2 TreePos { get; init; }
        public Angle TreeRot { get; init; }
        public float Sin { get; init; }
        public float Cos { get;  init; }
    }

    private struct SpriteSortBucket
    {
        public int Count;
        public int Offset;
        public int Next;
    }

    private sealed class SpriteSortItemDepthComparer : IComparer<SpriteSortItem>
    {
        public static readonly SpriteSortItemDepthComparer Instance = new();

        public int Compare(SpriteSortItem x, SpriteSortItem y)
        {
            var comparison = x.RenderOrder.CompareTo(y.RenderOrder);
            if (comparison != 0)
                return comparison;

            comparison = x.YSort.CompareTo(y.YSort);
            return comparison != 0 ? comparison : x.Uid.CompareTo(y.Uid);
        }
    }

    private readonly struct SpriteSortItem : IComparable<SpriteSortItem>
    {
        public readonly int Index;
        private readonly byte _drawDepth;
        private readonly uint _renderOrder;
        private readonly float _ySort;
        private readonly EntityUid _uid;

        public SpriteSortItem(int index, byte drawDepth, uint renderOrder, float ySort, EntityUid uid)
        {
            Index = index;
            _drawDepth = drawDepth;
            _renderOrder = renderOrder;
            _ySort = ySort;
            _uid = uid;
        }

        public byte DrawDepth => _drawDepth;
        public uint RenderOrder => _renderOrder;
        public float YSort => _ySort;
        public EntityUid Uid => _uid;

        public int CompareTo(SpriteSortItem other)
        {
            var cmp = _drawDepth.CompareTo(other._drawDepth);
            if (cmp != 0)
                return cmp;

            cmp = _renderOrder.CompareTo(other._renderOrder);

            if (cmp != 0)
                return cmp;

            // compare the top of the sprite's BB for y-sorting. Because screen coordinates are flipped, the "top" of the BB is actually the "bottom".
            cmp = _ySort.CompareTo(other._ySort);

            if (cmp != 0)
                return cmp;

            return _uid.CompareTo(other._uid);
        }
    }
}
