using System;
using Robust.Shared.Enums;
using Robust.Client.Graphics;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Robust.Client.GameStates
{
    internal sealed class NetInterpOverlay : Overlay
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        private readonly EntityLookupSystem _lookup;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        private readonly ShaderInstance _shader;
        private readonly SharedContainerSystem _container;
        private readonly SharedTransformSystem _xform;

        /// <summary>
        /// When an entity stops lerping the overlay will continue to draw a box around the entity for this amount of time.
        /// </summary>
        public static readonly TimeSpan Delay = TimeSpan.FromSeconds(2f);

        public NetInterpOverlay(EntityLookupSystem lookup)
        {
            IoCManager.InjectDependencies(this);
            _lookup = lookup;
            _shader = _prototypeManager.Index<ShaderPrototype>("unshaded").Instance();
            _container = _entityManager.System<SharedContainerSystem>();
            _xform = _entityManager.System<SharedTransformSystem>();
        }

        protected internal override void Draw(in OverlayDrawArgs args)
        {
            var handle = args.DrawingHandle;
            handle.UseShader(_shader);
            var worldHandle = (DrawingHandleWorld) handle;
            var viewport = args.WorldAABB;

            var query = _entityManager.AllEntityQueryEnumerator<TransformComponent>();
            while (query.MoveNext(out var uid, out var transform))
            {
                // if not on the same map, continue
                if (transform.MapID != args.MapId || _container.IsEntityInContainer(uid))
                    continue;

                if (transform.GridUid == uid)
                    continue;

                var delta = (_timing.CurTick.Value - transform.LastLerp.Value) * _timing.TickPeriod;
                if(!transform.ActivelyLerping && delta > Delay)
                    continue;

                var aabb = _lookup.GetWorldAABB(uid);

                // if not on screen, or too small, continue
                if (!aabb.Intersects(viewport) || aabb.IsEmpty())
                    continue;

                var (pos, rot) = _xform.GetWorldPositionRotation(transform, _entityManager.GetEntityQuery<TransformComponent>());
                var boxOffset = transform.NextPosition != null
                    ? transform.NextPosition.Value - transform.LocalPosition
                    : default;
                var worldOffset = (rot - transform.LocalRotation).RotateVec(boxOffset);

                var nextPos = pos + worldOffset;
                worldHandle.DrawLine(pos, nextPos, Color.Yellow);

                var nextAabb = aabb.Translated(worldOffset);

                Angle nextRot = rot;
                if (transform.NextRotation.HasValue)
                    nextRot += transform.NextRotation.Value - transform.LocalRotation;
                var nextBox = new Box2Rotated(nextAabb, nextRot, nextAabb.Center);
                worldHandle.DrawRect(nextBox, Color.Green.WithAlpha(0.1f), true);
                worldHandle.DrawRect(nextBox, Color.Green, false);

                var box = new Box2Rotated(aabb, rot, aabb.Center);
                worldHandle.DrawRect(box, Color.Yellow.WithAlpha(0.1f), true);
                worldHandle.DrawRect(box, Color.Yellow, false);
            }
        }

        private sealed class NetShowInterpCommand : LocalizedCommands
        {
            [Dependency] private readonly IEntityManager _entManager = default!;
            [Dependency] private readonly IOverlayManager _overlay = default!;

            public override string Command => "net_draw_interp";

            public override void Execute(IConsoleShell shell, string argStr, string[] args)
            {
                if (!_overlay.HasOverlay<NetInterpOverlay>())
                {
                    _overlay.AddOverlay(new NetInterpOverlay(_entManager.System<EntityLookupSystem>()));
                    shell.WriteLine("Enabled network interp overlay.");
                }
                else
                {
                    _overlay.RemoveOverlay<NetInterpOverlay>();
                    shell.WriteLine("Disabled network interp overlay.");
                }
            }
        }
    }
}
