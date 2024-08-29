using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using Robust.Client.ComponentTrees;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Threading;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics.Clyde;

// this partial class contains code specific to querying, processing & sorting sprites.
internal partial class Clyde
{
    [Shared.IoC.Dependency] private readonly IParallelManager _parMan = default!;
    private readonly RefList<SpriteData> _drawingSpriteList = new();
    private const int _spriteProcessingBatchSize = 25;

    private void GetSprites(MapId map, Viewport view, IEye eye, Box2Rotated worldBounds, out int[] indexList)
    {
        ProcessSpriteEntities(map, view, eye, worldBounds, _drawingSpriteList);

        // We use a separate list for indexing sprites so that the sort is faster.
        indexList = ArrayPool<int>.Shared.Rent(_drawingSpriteList.Count);

        // populate index list
        for (var i = 0; i < _drawingSpriteList.Count; i++)
            indexList[i] = i;

        // sort index list
        // TODO better sorting? parallel merge sort?
        Array.Sort(indexList, 0, _drawingSpriteList.Count, new SpriteDrawingOrderComparer(_drawingSpriteList));
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
        var opts = new ParallelOptions { MaxDegreeOfParallelism = _parMan.ParallelProcessCount };

        foreach (var (treeOwner, comp) in _spriteTreeSystem.GetIntersectingTrees(map, worldBounds))
        {
            var treeXform = query.GetComponent(treeOwner);
            var bounds = _transformSystem.GetInvWorldMatrix(treeOwner).TransformBox(worldBounds);
            DebugTools.Assert(treeXform.MapUid == treeXform.ParentUid || !treeXform.ParentUid.IsValid());

            treeData = treeData with
            {
                TreeOwner = treeOwner,
                TreePos = treeXform.LocalPosition,
                TreeRot = treeXform.LocalRotation,
                Sin = MathF.Sin((float)treeXform.LocalRotation),
                Cos = MathF.Cos((float)treeXform.LocalRotation),
            };

            comp.Tree.QueryAabb(ref list,
                static (ref RefList<SpriteData> state, in ComponentTreeEntry<SpriteComponent> value) =>
                {
                    ref var entry = ref state.AllocAdd();
                    entry.Uid = value.Uid;
                    entry.Sprite = value.Component;
                    entry.Xform = value.Transform;
                    return true;
                }, bounds, true);

            // Get bounding boxes & world positions
            added = list.Count - index;
            var batches = added/_spriteProcessingBatchSize;

            // TODO also do sorting here & use a merge sort later on for y-sorting?
            if (batches > 1)
                Parallel.For(0, batches, opts, (i) => ProcessSprites(list, index + i * _spriteProcessingBatchSize, _spriteProcessingBatchSize, treeData));
            else
                batches = 0;

            var remainder = added - _spriteProcessingBatchSize * batches;
            if (remainder > 0)
                ProcessSprites(list, index + batches * _spriteProcessingBatchSize, remainder, treeData);

            index += batches * _spriteProcessingBatchSize + remainder;
        }
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
            // (data.WorldPos, data.WorldRot) = batch.Sys.GetWorldPositionRotation(data.Xform, batch.Query);
            // var spriteWorldBB = data.Sprite.CalculateRotatedBoundingBox(data.WorldPos, data.WorldRot, batch.ViewRotation);
            // data.SpriteScreenBB = Viewport.GetWorldToLocalMatrix().TransformBox(spriteWorldBB);

            var (pos, rot) = batch.Sys.GetRelativePositionRotation(data.Xform, batch.TreeOwner, batch.Query);
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
            data.SpriteScreenBB = TransformCenteredBox(
                data.Sprite.Bounds,
                finalRotation,
                pos + batch.PreScaleViewOffset,
                batch.ViewScale);
        }
    }

    /// <summary>
    /// This is effectively a specialized combination of a <see cref="Matrix3.TransformBox(in Box2Rotated)"/> and <see cref="Box2Rotated.CalcBoundingBox()"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe Box2 TransformCenteredBox(in Box2 box, float angle, in Vector2 offset, in Vector2 scale)
    {
        // This function is for sprites, which flip the y axis, so here we flip the definition of t and b relative to the normal function.
        DebugTools.Assert(scale.Y < 0);

        var boxVec = Unsafe.As<Box2, Vector128<float>>(ref Unsafe.AsRef(in box));

        var sin = Vector128.Create(MathF.Sin(angle));
        var cos = Vector128.Create(MathF.Cos(angle));
        var allX = Vector128.Shuffle(boxVec, Vector128.Create(0, 0, 2, 2));
        var allY = Vector128.Shuffle(boxVec, Vector128.Create(1, 3, 3, 1));
        var modX = allX * cos - allY * sin;
        var modY = allX * sin + allY * cos;

        var offsetVec = Unsafe.As<Vector2, Vector128<float>>(ref Unsafe.AsRef(in offset)); // upper undefined
        var scaleVec = Unsafe.As<Vector2, Vector128<float>>(ref Unsafe.AsRef(in scale)); // upper undefined
        offsetVec = Vector128.Shuffle(offsetVec, Vector128.Create(0, 1, 0, 1));
        scaleVec = Vector128.Shuffle(scaleVec, Vector128.Create(0, 1, 0, 1));

        Vector128<float> lbrt;
        if (Sse.IsSupported)
        {
            var lrlr = SimdHelpers.MinMaxHorizontalSse(modX);
            var btbt = SimdHelpers.MaxMinHorizontalSse(modY);
            lbrt = Sse.UnpackLow(lrlr, btbt);
        }
        else
        {
            var l = SimdHelpers.MinHorizontal128(allX);
            var b = SimdHelpers.MaxHorizontal128(allY);
            var r = SimdHelpers.MaxHorizontal128(allX);
            var t = SimdHelpers.MinHorizontal128(allY);
            lbrt = SimdHelpers.MergeRows128(l, b, r, t);
        }

        // offset and scale box.
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
        public Box2 SpriteScreenBB;
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

    private sealed class SpriteDrawingOrderComparer : IComparer<int>
    {
        private readonly RefList<SpriteData> _drawList;

        public SpriteDrawingOrderComparer(RefList<SpriteData> drawList)
        {
            _drawList = drawList;
        }

        public int Compare(int x, int y)
        {
            var a = _drawList[x];
            var b = _drawList[y];

            var cmp = a.Sprite.DrawDepth.CompareTo(b.Sprite.DrawDepth);
            if (cmp != 0)
                return cmp;

            cmp = a.Sprite.RenderOrder.CompareTo(b.Sprite.RenderOrder);

            if (cmp != 0)
                return cmp;

            // compare the top of the sprite's BB for y-sorting. Because screen coordinates are flipped, the "top" of the BB is actually the "bottom".
            cmp = a.SpriteScreenBB.Top.CompareTo(b.SpriteScreenBB.Top);

            if (cmp != 0)
                return cmp;

            return a.Uid.CompareTo(b.Uid);
        }
    }
}
