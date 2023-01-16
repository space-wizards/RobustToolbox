using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Client.ComponentTrees;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using static Robust.Shared.GameObjects.SharedSpriteComponent;

namespace Robust.Client.GameObjects
{
    /// <summary>
    /// Updates the layer animation for every visible sprite.
    /// </summary>
    [UsedImplicitly]
    public sealed partial class SpriteSystem : EntitySystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly SpriteTreeSystem _treeSystem = default!;
        [Dependency] private readonly TransformSystem _transform = default!;

        private readonly Queue<SpriteComponent> _inertUpdateQueue = new();
        private HashSet<SpriteComponent> _manualUpdate = new();

        public override void Initialize()
        {
            base.Initialize();

            UpdatesAfter.Add(typeof(SpriteTreeSystem));

            _proto.PrototypesReloaded += OnPrototypesReloaded;
            SubscribeLocalEvent<SpriteComponent, SpriteUpdateInertEvent>(QueueUpdateInert);
            SubscribeLocalEvent<SpriteComponent, ComponentInit>(OnCompInit);
            _cfg.OnValueChanged(CVars.RenderSpriteDirectionBias, OnBiasChanged, true);
        }

        private void OnCompInit(EntityUid uid, SpriteComponent component, ComponentInit args)
        {
            InitializeSprite(uid, component, component.Sprite, component.Texture, component.State, component._layerDatums);
        }

        internal void InitializeSprite(
            EntityUid uid,
            SpriteComponent sprite,
            string? rsi,
            string? texture,
            string? state,
            IReadOnlyList<PrototypeLayerData>? layers)
        {
            sprite.UpdateLocalMatrix();

            // TODO this should just handled via a custom type serializer for RSI. But that currently results in a ton of
            // IoC resolves.
            if (rsi != null)
            {
                var rsiPath = TextureRoot / rsi;
                if (_resourceCache.TryGetResource(rsiPath, out RSIResource? resource))
                    sprite.BaseRSI = resource.RSI;
                else
                    Logger.ErrorS(SpriteComponent.LogCategory, "Unable to load RSI '{0}'.", rsiPath);
            }
            else
            {
                sprite.BaseRSI = null;
            }

            // TODO this should be done via a custom type serializer for layer data
            if (layers != null && layers.Count() > 0)
            {
                sprite.SetLayerData(layers);
                return;
            }

            sprite.Layers.Clear();
            sprite.LayerMap.Clear();
            _treeSystem.QueueTreeUpdate(uid, sprite);
            QueueUpdateInert(uid, sprite);

            if (state == null && texture == null)
                return;

            sprite.AddLayer(new PrototypeLayerData
            {
                TexturePath = texture,
                State = state,
                Color = Color.White,
                Scale = Vector2.One,
                Visible = true,
            });
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
            => QueueUpdateInert(uid, sprite);

        private void QueueUpdateInert(EntityUid uid, SpriteComponent sprite)
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

            var xforms = EntityManager.GetEntityQuery<TransformComponent>();
            var spriteState = (frameTime, _manualUpdate);

            _treeSystem.QueryAabb( ref spriteState, static (ref (float frameTime,
                    HashSet<SpriteComponent> _manualUpdate) tuple, in ComponentTreeEntry<SpriteComponent> value) =>
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
        public void ForceUpdate(SpriteComponent sprite)
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
