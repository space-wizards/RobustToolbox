using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects;

public sealed partial class SpriteSystem
{
    private readonly List<SpriteComponent.PostShaderEntry> _postShaderSortEntries = new();
    private readonly List<TopologicalSort.GraphNode<SpriteComponent.PostShaderEntry>> _postShaderSortNodes = new();
    private readonly Dictionary<string, TopologicalSort.GraphNode<SpriteComponent.PostShaderEntry>> _postShaderSortNodeById = new();

    /// <summary>
    /// Resets the sprite's animated layers to align with a given time (in seconds).
    /// </summary>
    public void SetAutoAnimateSync(SpriteComponent sprite, double time)
    {
        foreach (var layer in sprite.AllLayers)
        {
            if (layer is not SpriteComponent.Layer spriteLayer)
                continue;

            SetAutoAnimateSync(sprite, spriteLayer, time);
        }
    }

    /// <summary>
    /// Resets the layer's animation to align with a given time (in seconds).
    /// </summary>
    public void SetAutoAnimateSync(SpriteComponent sprite, SpriteComponent.Layer layer, double time)
    {
        if (!layer.AutoAnimated)
            return;

        var rsi = layer.RSI ?? sprite.BaseRSI;

        if (rsi == null || !rsi.TryGetState(layer.State, out var state))
        {
            state = GetFallbackState();
        }

        if (!state.IsAnimated)
        {
            return;
        }

        layer.AnimationTimeLeft = (float) -(time % state.TotalDelay);
        layer.AnimationFrame = 0;
    }

    public void CopySprite(Entity<SpriteComponent?> source, Entity<SpriteComponent?> target)
    {
        if (!Resolve(source.Owner, ref source.Comp))
            return;

        if (!Resolve(target.Owner, ref target.Comp))
            return;

        target.Comp._baseRsi = source.Comp._baseRsi;
        target.Comp._bounds = source.Comp._bounds;
        target.Comp._visible = source.Comp._visible;
        target.Comp.color = source.Comp.color;
        target.Comp.offset = source.Comp.offset;
        target.Comp.rotation = source.Comp.rotation;
        target.Comp.scale = source.Comp.scale;
        target.Comp.LocalMatrix = Matrix3Helpers.CreateTransform(
            in target.Comp.offset,
            in target.Comp.rotation,
            in target
            .Comp.scale);

        target.Comp.drawDepth = source.Comp.drawDepth;
        target.Comp.NoRotation = source.Comp.NoRotation;
        target.Comp.DirectionOverride = source.Comp.DirectionOverride;
        target.Comp.EnableDirectionOverride = source.Comp.EnableDirectionOverride;
        target.Comp.Layers = new List<SpriteComponent.Layer>(source.Comp.Layers.Count);
        foreach (var otherLayer in source.Comp.Layers)
        {
            var layer = new SpriteComponent.Layer(otherLayer, target.Comp);
            layer.Index = target.Comp.Layers.Count;
            layer.Owner = target!;
            target.Comp.Layers.Add(layer);
        }

        target.Comp.IsInert = source.Comp.IsInert;
        target.Comp.LayerMap = source.Comp.LayerMap.ShallowClone();
        target.Comp.PostShaders = new List<SpriteComponent.PostShaderEntry>(source.Comp.PostShaders.Count);
        foreach (var postShader in source.Comp.PostShaders)
        {
            target.Comp.PostShaders.Add(new SpriteComponent.PostShaderEntry(postShader));
        }

        target.Comp.PostShaderOrderDirty = source.Comp.PostShaderOrderDirty;

        target.Comp.RenderOrder = source.Comp.RenderOrder;
        target.Comp.GranularLayersRendering = source.Comp.GranularLayersRendering;
        target.Comp.Loop = source.Comp.Loop;

        DirtyBounds(target!);
        _tree.QueueTreeUpdate(target!);
    }

    /// <summary>
    /// Adds a sprite to a queue that will update <see cref="SpriteComponent.IsInert"/> next frame.
    /// </summary>
    public void QueueUpdateIsInert(Entity<SpriteComponent> sprite)
    {
        if (sprite.Comp._inertUpdateQueued)
            return;

        sprite.Comp._inertUpdateQueued = true;
        _inertUpdateQueue.Enqueue(sprite);
    }

    [Obsolete("Use QueueUpdateIsInert")]
    public void QueueUpdateInert(EntityUid uid, SpriteComponent sprite) => QueueUpdateIsInert(new (uid, sprite));

    /// <summary>
    ///     Gets the sprite's ordered post-shader list, resolving and sorting it if required.
    /// </summary>
    public IReadOnlyList<SpriteComponent.PostShaderEntry> GetPostShaders(Entity<SpriteComponent?> sprite)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp, false))
            return Array.Empty<SpriteComponent.PostShaderEntry>();

        return GetPostShaders(sprite.Comp);
    }

    /// <summary>
    ///     Gets the sprite's ordered post-shader list, sorting it if required.
    /// </summary>
    public IReadOnlyList<SpriteComponent.PostShaderEntry> GetPostShaders(SpriteComponent sprite)
    {
        // Keep sorting lazy so repeatedly setting multiple shader properties during one update only pays once.
        if (sprite.PostShaderOrderDirty)
            SortPostShaders(sprite);

        return sprite.PostShaders;
    }

    /// <summary>
    ///     Returns whether the sprite has a post-shader with the given id.
    /// </summary>
    public bool HasPostShader(Entity<SpriteComponent?> sprite, string id)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp, false))
            return false;

        return HasPostShader(sprite.Comp, id);
    }

    /// <summary>
    ///     Returns whether the sprite has a post-shader with the given id.
    /// </summary>
    public bool HasPostShader(SpriteComponent sprite, string id)
    {
        // Could use sorted-dictionary but not expected to have a lot of these so this should be cheaper.
        foreach (var shader in sprite.PostShaders)
        {
            if (shader.Id == id)
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Tries to get a post-shader by id.
    /// </summary>
    public bool TryGetPostShader(Entity<SpriteComponent?> sprite, string id, out SpriteComponent.PostShaderEntry entry)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp, false))
        {
            entry = default!;
            return false;
        }

        return TryGetPostShader(sprite.Comp, id, out entry);
    }

    /// <summary>
    ///     Tries to get a post-shader by id.
    /// </summary>
    public bool TryGetPostShader(SpriteComponent sprite, string id, out SpriteComponent.PostShaderEntry entry)
    {
        foreach (var shader in sprite.PostShaders)
        {
            if (shader.Id != id)
                continue;

            entry = shader;
            return true;
        }

        entry = default!;
        return false;
    }

    /// <summary>
    ///     Adds or replaces a post-shader on the sprite.
    /// </summary>
    public void SetPostShader(
        Entity<SpriteComponent?> sprite,
        SpriteComponent.PostShaderArgs args)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp, false))
            return;

        SetPostShader(sprite.Comp, args);
    }

    /// <summary>
    ///     Adds or replaces a post-shader on the sprite.
    /// </summary>
    public void SetPostShader(
        SpriteComponent sprite,
        SpriteComponent.PostShaderArgs args)
    {
        var beforeArray = ToDependencyArray(args.Before);
        var afterArray = ToDependencyArray(args.After);

        // IDs are stable handles for systems to replace/remove their own post-shader without disturbing others.
        for (var i = 0; i < sprite.PostShaders.Count; i++)
        {
            var existing = sprite.PostShaders[i];
            if (existing.Id != args.Id)
                continue;

            existing.Shader = args.Shader;
            existing.GetScreenTexture = args.GetScreenTexture;
            existing.RaiseShaderEvent = args.RaiseShaderEvent;
            existing.Before = beforeArray;
            existing.After = afterArray;
            sprite.PostShaderOrderDirty = true;
            return;
        }

        sprite.PostShaders.Add(new SpriteComponent.PostShaderEntry(
            args.Id,
            args.Shader,
            args.GetScreenTexture,
            args.RaiseShaderEvent,
            beforeArray,
            afterArray)
        {
            InsertionIndex = sprite.PostShaders.Count,
        });
        sprite.PostShaderOrderDirty = true;
    }

    /// <summary>
    ///     Removes a post-shader from the sprite by id.
    /// </summary>
    public void RemovePostShader(Entity<SpriteComponent?> sprite, string id)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp, false))
            return;

        RemovePostShader(sprite.Comp, id);
    }

    /// <summary>
    ///     Removes a post-shader from the sprite by id.
    /// </summary>
    public void RemovePostShader(SpriteComponent sprite, string id)
    {
        for (var i = 0; i < sprite.PostShaders.Count; i++)
        {
            if (sprite.PostShaders[i].Id != id)
                continue;

            sprite.PostShaders.RemoveAt(i);
            sprite.PostShaderOrderDirty = true;
            return;
        }
    }

    /// <summary>
    ///     Removes all post-shaders from the sprite.
    /// </summary>
    public void ClearPostShaders(Entity<SpriteComponent?> sprite)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp, false))
            return;

        ClearPostShaders(sprite.Comp);
    }

    /// <summary>
    ///     Removes all post-shaders from the sprite.
    /// </summary>
    public void ClearPostShaders(SpriteComponent sprite)
    {
        sprite.PostShaders.Clear();
        sprite.PostShaderOrderDirty = false;
    }

    private void SortPostShaders(SpriteComponent sprite)
    {
        sprite.PostShaderOrderDirty = false;

        var shaders = sprite.PostShaders;
        if (shaders.Count < 2)
            return;

        _postShaderSortEntries.Clear();
        _postShaderSortNodes.Clear();
        _postShaderSortNodeById.Clear();

        foreach (var shader in shaders)
        {
            _postShaderSortEntries.Add(shader);
        }

        _postShaderSortEntries.Sort(PostShaderInsertionComparison);

        foreach (var shader in _postShaderSortEntries)
        {
            var node = new TopologicalSort.GraphNode<SpriteComponent.PostShaderEntry>(shader);
            _postShaderSortNodes.Add(node);
            _postShaderSortNodeById[shader.Id] = node;
        }

        foreach (var node in _postShaderSortNodes)
        {
            foreach (var before in node.Value.Before)
            {
                if (_postShaderSortNodeById.TryGetValue(before, out var dependency))
                    AddPostShaderDependency(node, dependency);
            }

            foreach (var after in node.Value.After)
            {
                if (_postShaderSortNodeById.TryGetValue(after, out var dependency))
                    AddPostShaderDependency(dependency, node);
            }
        }

        try
        {
            var sorted = TopologicalSort.Sort(_postShaderSortNodes).ToArray();

            shaders.Clear();
            foreach (var shader in sorted)
            {
                shaders.Add(shader);
            }
        }
        catch (InvalidOperationException)
        {
            _sawmill.Warning("Post-shader dependency cycle detected on sprite {0}; keeping insertion order.", sprite.Owner);
            shaders.Sort(PostShaderInsertionComparison);
        }
    }

    private static void AddPostShaderDependency(
        TopologicalSort.GraphNode<SpriteComponent.PostShaderEntry> before,
        TopologicalSort.GraphNode<SpriteComponent.PostShaderEntry> after)
    {
        if (before == after || before.Dependant.Contains(after))
            return;

        before.Dependant.Add(after);
    }

    private static int PostShaderInsertionComparison(SpriteComponent.PostShaderEntry x, SpriteComponent.PostShaderEntry y)
    {
        var cmp = x.InsertionIndex.CompareTo(y.InsertionIndex);
        return cmp != 0 ? cmp : string.CompareOrdinal(x.Id, y.Id);
    }

    private static string[] ToDependencyArray(IEnumerable<string>? dependencies)
    {
        if (dependencies == null)
            return Array.Empty<string>();

        if (dependencies is string[] array)
            return array;

        return new List<string>(dependencies).ToArray();
    }
}
