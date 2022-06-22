using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace Robust.Client.GameObjects
{
    /// <summary>
    /// Updates the layer animation for every visible sprite.
    /// </summary>
    [UsedImplicitly]
    public sealed partial class SpriteSystem : EntitySystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly RenderingTreeSystem _treeSystem = default!;

        private readonly Queue<SpriteComponent> _inertUpdateQueue = new();
        private HashSet<ISpriteComponent> _manualUpdate = new();

        public override void Initialize()
        {
            base.Initialize();

            _proto.PrototypesReloaded += OnPrototypesReloaded;
            SubscribeLocalEvent<SpriteUpdateInertEvent>(QueueUpdateInert);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _proto.PrototypesReloaded -= OnPrototypesReloaded;
        }

        private void QueueUpdateInert(SpriteUpdateInertEvent ev)
        {
            _inertUpdateQueue.Enqueue(ev.Sprite);
        }

        /// <inheritdoc />
        public override void FrameUpdate(float frameTime)
        {
            while (_inertUpdateQueue.TryDequeue(out var sprite))
            {
                sprite.DoUpdateIsInert();
            }

            foreach (var sprite in _manualUpdate)
            {
                if (!sprite.Deleted && !sprite.IsInert)
                    sprite.FrameUpdate(frameTime);
            }

            var pvsBounds = _eyeManager.GetWorldViewbounds();

            var currentMap = _eyeManager.CurrentMap;
            if (currentMap == MapId.Nullspace)
            {
                return;
            }

            var xforms = EntityManager.GetEntityQuery<TransformComponent>();

            foreach (var comp in _treeSystem.GetRenderTrees(currentMap, pvsBounds))
            {
                var bounds = xforms.GetComponent(comp.Owner).InvWorldMatrix.TransformBox(pvsBounds);

                comp.SpriteTree.QueryAabb(ref frameTime, (ref float state, in ComponentTreeEntry<SpriteComponent> value) =>
                {
                    if (value.Component.IsInert)
                    {
                        return true;
                    }

                    if (!_manualUpdate.Contains(value.Component))
                        value.Component.FrameUpdate(state);
                    return true;
                }, bounds, true);
            }

            _manualUpdate.Clear();
        }

        /// <summary>
        ///     Force update of the sprite component next frame
        /// </summary>
        public void ForceUpdate(ISpriteComponent sprite)
        {
            _manualUpdate.Add(sprite);
        }

        /// <summary>
        ///     Function to set a sprite's shader. Raises a cancellable event, as currently sprites only support a
        ///     single shader at a time. Prevents a situation where one system's shader gets overwritten by another
        ///     (e.g., interaction outlines).
        /// </summary>
        public void SetPostShader(EntityUid uid, ShaderInstance? shader, SpriteComponent? sprite = null, bool force = false)
        {
            if (!Resolve(uid, ref sprite))
                return;

            if (force)
            {
                sprite.PostShader = shader;
                return;
            }

            var ev = new SetShaderAttemptEvent(shader);

            RaiseLocalEvent(uid, ev, false);
            if (!ev.Cancelled)
                sprite.PostShader = shader;
        }
    }

    /// <summary>
    ///     This event gets raised when something attempts to modify a sprite's post-shader. If canceled, the shader
    ///     will not get updated.
    /// </summary>
    public sealed class SetShaderAttemptEvent : CancellableEntityEventArgs
    {
        public readonly ShaderInstance? Shader;

        public SetShaderAttemptEvent(ShaderInstance? shader)
        {
            Shader = shader;
        }
    }

    /// <summary>
    ///     This event gets raised before a sprite gets drawn using it's post-shader.
    /// </summary>
    public sealed class BeforePostShaderRenderEvent : EntityEventArgs
    {
        public readonly SpriteComponent Sprite;
        public readonly IClydeViewport Viewport;

        public BeforePostShaderRenderEvent(SpriteComponent sprite, IClydeViewport viewport)
        {
            Sprite = sprite;
            Viewport = viewport;
        }
    }
}
