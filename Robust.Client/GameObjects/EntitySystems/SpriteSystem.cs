using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Client.ComponentTrees;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Robust.Client.GameObjects
{
    /// <summary>
    /// Updates the layer animation for every visible sprite.
    /// </summary>
    [UsedImplicitly]
    public sealed partial class SpriteSystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly SpriteTreeSystem _treeSystem = default!;

        private readonly Queue<SpriteComponent> _inertUpdateQueue = new();
        private readonly HashSet<ISpriteComponent> _manualUpdate = new();

        public override void Initialize()
        {
            base.Initialize();

            UpdatesAfter.Add(typeof(SpriteTreeSystem));

            _proto.PrototypesReloaded += OnPrototypesReloaded;
            SubscribeLocalEvent<SpriteComponent, SpriteUpdateInertEvent>(QueueUpdateInert);
            SubscribeLocalEvent<SyncSpriteComponent, ComponentStartup>(OnSyncSpriteStartup);
            SubscribeLocalEvent<SyncSpriteComponent, EntParentChangedMessage>(OnSyncSpriteParent);
            SubscribeLocalEvent<SyncSpriteComponent, EntityUnpausedEvent>(OnSyncSpriteUnpaused);
            _cfg.OnValueChanged(CVars.RenderSpriteDirectionBias, OnBiasChanged, true);
        }

        private void OnSyncSpriteUnpaused(EntityUid uid, SyncSpriteComponent component, ref EntityUnpausedEvent args)
        {
            if (!TryComp<SpriteComponent>(uid, out var sprite))
            {
                return;
            }

            SetAutoAnimateSync(sprite);
        }

        private void OnSyncSpriteStartup(EntityUid uid, SyncSpriteComponent component, ComponentStartup args)
        {
            if (!TryComp<SpriteComponent>(uid, out var sprite))
            {
                return;
            }

            SetAutoAnimateSync(sprite);
        }

        private void OnSyncSpriteParent(EntityUid uid, SyncSpriteComponent component, ref EntParentChangedMessage args)
        {
            // Only update if it came out of pvs.
            if (args.OldMapId == args.Transform.MapID ||
                args.OldMapId != MapId.Nullspace ||
                !TryComp<SpriteComponent>(uid, out var sprite))
            {
                return;
            }

            SetAutoAnimateSync(sprite);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _proto.PrototypesReloaded -= OnPrototypesReloaded;
            _cfg.UnsubValueChanged(CVars.RenderSpriteDirectionBias, OnBiasChanged);
        }

        private void OnBiasChanged(double value)
        {
            SpriteComponent.DirectionBias = value;
        }

        private void QueueUpdateInert(EntityUid uid, SpriteComponent sprite, ref SpriteUpdateInertEvent ev)
        {
            if (sprite._inertUpdateQueued)
                return;

            sprite._inertUpdateQueued = true;
            _inertUpdateQueue.Enqueue(sprite);
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

            var spriteState = (frameTime, _manualUpdate);

            _treeSystem.QueryAabb( ref spriteState, static (ref (float frameTime,
                    HashSet<ISpriteComponent> _manualUpdate) tuple, in ComponentTreeEntry<SpriteComponent> value) =>
                {
                    if (value.Component.IsInert)
                        return true;

                    if (!tuple._manualUpdate.Contains(value.Component))
                        value.Component.FrameUpdate(tuple.frameTime);

                    return true;
                }, currentMap, pvsBounds, true);

            _manualUpdate.Clear();
        }

        /// <summary>
        ///     Force update of the sprite component next frame
        /// </summary>
        public void ForceUpdate(ISpriteComponent sprite)
        {
            _manualUpdate.Add(sprite);
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
