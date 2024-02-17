using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Client.ComponentTrees;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Robust.Client.GameObjects
{
    /// <summary>
    /// Updates the layer animation for every visible sprite.
    /// </summary>
    [UsedImplicitly]
    public sealed partial class SpriteSystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly ILogManager _logManager = default!;

        private readonly Queue<SpriteComponent> _inertUpdateQueue = new();

        /// <summary>
        ///     Entities that require a sprite frame update.
        /// </summary>
        private readonly HashSet<EntityUid> _queuedFrameUpdate = new();

        private ISawmill _sawmill = default!;

        internal void Render(EntityUid uid, SpriteComponent sprite, DrawingHandleWorld drawingHandle, Angle eyeRotation, in Angle worldRotation, in Vector2 worldPosition)
        {
            if (!sprite.IsInert)
                _queuedFrameUpdate.Add(uid);

            sprite.RenderInternal(drawingHandle, eyeRotation, worldRotation, worldPosition, sprite.EnableDirectionOverride ? sprite.DirectionOverride : null);
        }

        internal void RenderLayer(Entity<SpriteComponent> ent, DrawingHandleWorld drawingHandleWorld, Angle eyeRotation, in Angle worldRotation, in Vector2 worldPosition, Layer layer)
        {
            var sprite = ent.Comp;
            if (!sprite.IsInert)
                _queuedFrameUpdate.Add(ent);

            Direction? overrideDirection = sprite.EnableDirectionOverride ? sprite.DirectionOverride : null;

            var angle = worldRotation + eyeRotation; // angle on-screen. Used to decide the direction of 4/8 directional RSIs
            angle = angle.Reduced().FlipPositive();  // Reduce the angles to fix math shenanigans

            var cardinal = Angle.Zero;

            // If we have a 1-directional sprite then snap it to try and always face it south if applicable.
            if (!sprite.NoRotation && sprite.SnapCardinals)
            {
                cardinal = angle.GetCardinalDir().ToAngle();
            }

            // worldRotation + eyeRotation should be the angle of the entity on-screen. If no-rot is enabled this is just set to zero.
            // However, at some point later the eye-matrix is applied separately, so we subtract -eye rotation for now:
            var entityMatrix = Matrix3.CreateTransform(worldPosition, sprite.NoRotation ? -eyeRotation : worldRotation - cardinal);
            var localMatrix = sprite.GetLocalMatrix();

            Matrix3.Multiply(in localMatrix, in entityMatrix, out var transformSprite);

            //Default rendering
            entityMatrix = Matrix3.CreateTransform(worldPosition, worldRotation);
            Matrix3.Multiply(in localMatrix, in entityMatrix, out var transformDefault);
            //Snap to cardinals
            entityMatrix = Matrix3.CreateTransform(worldPosition, worldRotation - angle.GetCardinalDir().ToAngle());
            Matrix3.Multiply(in localMatrix, in entityMatrix, out var transformSnap);
            //No rotation
            entityMatrix = Matrix3.CreateTransform(worldPosition, -eyeRotation);
            Matrix3.Multiply(in localMatrix, in entityMatrix, out var transformNoRot);

            switch (layer.RenderingStrategy)
            {
                case LayerRenderingStrategy.NoRotation:
                    layer.Render(drawingHandleWorld, ref transformNoRot, angle, overrideDirection);
                    break;
                case LayerRenderingStrategy.SnapToCardinals:
                    layer.Render(drawingHandleWorld, ref transformSnap, angle, overrideDirection);
                    break;
                case LayerRenderingStrategy.Default:
                    layer.Render(drawingHandleWorld, ref transformDefault, angle, overrideDirection);
                    break;
                case LayerRenderingStrategy.UseSpriteStrategy:
                    layer.Render(drawingHandleWorld, ref transformSprite, angle, overrideDirection);
                    break;
                default:
                    Log.Error($"Tried to render a layer with unknown rendering strategy: {layer.RenderingStrategy}");
                    break;
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            UpdatesAfter.Add(typeof(SpriteTreeSystem));

            SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
            SubscribeLocalEvent<SpriteComponent, SpriteUpdateInertEvent>(QueueUpdateInert);
            SubscribeLocalEvent<SpriteComponent, ComponentInit>(OnInit);

            Subs.CVar(_cfg, CVars.RenderSpriteDirectionBias, OnBiasChanged, true);
            _sawmill = _logManager.GetSawmill("sprite");
        }

        private void OnInit(EntityUid uid, SpriteComponent component, ComponentInit args)
        {
            // I'm not 100% this is needed, but I CBF with this ATM. Somebody kill server sprite component please.
            QueueUpdateInert(uid, component);
        }

        private void OnBiasChanged(double value)
        {
            SpriteComponent.DirectionBias = value;
        }

        private void QueueUpdateInert(EntityUid uid, SpriteComponent sprite, ref SpriteUpdateInertEvent ev)
            => QueueUpdateInert(uid, sprite);

        public void QueueUpdateInert(EntityUid uid, SpriteComponent sprite)
        {
            if (sprite._inertUpdateQueued)
                return;

            sprite._inertUpdateQueued = true;
            _inertUpdateQueue.Enqueue(sprite);
        }

        private void DoUpdateIsInert(SpriteComponent component)
        {
            component._inertUpdateQueued = false;
            component.IsInert = true;

            foreach (var layer in component.Layers)
            {
                // Since StateId is a struct, we can't null-check it directly.
                if (!layer.State.IsValid || !layer.Visible || !layer.AutoAnimated || layer.Blank)
                {
                    continue;
                }

                var rsi = layer.RSI ?? component.BaseRSI;
                if (rsi == null || !rsi.TryGetState(layer.State, out var state))
                {
                    state = GetFallbackState();
                }

                if (state.IsAnimated)
                {
                    component.IsInert = false;
                    break;
                }
            }
        }

        /// <inheritdoc />
        public override void FrameUpdate(float frameTime)
        {
            while (_inertUpdateQueue.TryDequeue(out var sprite))
            {
                DoUpdateIsInert(sprite);
            }

            var realtime = _timing.RealTime.TotalSeconds;
            var spriteQuery = GetEntityQuery<SpriteComponent>();
            var syncQuery = GetEntityQuery<SyncSpriteComponent>();
            var metaQuery = GetEntityQuery<MetaDataComponent>();

            foreach (var uid in _queuedFrameUpdate)
            {
                if (!spriteQuery.TryGetComponent(uid, out var sprite) ||
                    metaQuery.GetComponent(uid).EntityPaused)
                {
                    continue;
                }

                if (sprite.IsInert)
                    continue;

                var sync = syncQuery.HasComponent(uid);

                foreach (var layer in sprite.Layers)
                {
                    if (!layer.State.IsValid || !layer.Visible || !layer.AutoAnimated)
                        continue;

                    var rsi = layer.RSI ?? sprite.BaseRSI;
                    if (rsi == null || !rsi.TryGetState(layer.State, out var state))
                        state = GetFallbackState();

                    if (!state.IsAnimated)
                        continue;

                    if (sync)
                    {
                        layer.AnimationTime = (float)(realtime % state.TotalDelay);
                        layer.AnimationTimeLeft = -layer.AnimationTime;
                        layer.AnimationFrame = 0;
                    }
                    else
                    {
                        layer.AnimationTime += frameTime;
                        layer.AnimationTimeLeft -= frameTime;
                    }

                    layer.AdvanceFrameAnimation(state);
                }
            }

            _queuedFrameUpdate.Clear();
        }

        /// <summary>
        ///     Force update of the sprite component next frame
        /// </summary>
        public void ForceUpdate(EntityUid uid)
        {
            _queuedFrameUpdate.Add(uid);
        }

        /// <summary>
        /// Gets the specified frame for this sprite at the specified time.
        /// </summary>
        public Texture GetFrame(SpriteSpecifier spriteSpec, TimeSpan curTime)
        {
            Texture? sprite = null;

            switch (spriteSpec)
            {
                case SpriteSpecifier.Rsi rsi:
                    var rsiActual = _resourceCache.GetResource<RSIResource>(rsi.RsiPath).RSI;
                    rsiActual.TryGetState(rsi.RsiState, out var state);
                    var frames = state!.GetFrames(RsiDirection.South);
                    var delays = state.GetDelays();
                    var totalDelay = delays.Sum();
                    var time = curTime.TotalSeconds % totalDelay;
                    var delaySum = 0f;

                    for (var i = 0; i < delays.Length; i++)
                    {
                        var delay = delays[i];
                        delaySum += delay;

                        if (time > delaySum)
                            continue;

                        sprite = frames[i];
                        break;
                    }

                    sprite ??= Frame0(spriteSpec);
                    break;
                case SpriteSpecifier.Texture texture:
                    sprite = texture.GetTexture(_resourceCache);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return sprite;
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
