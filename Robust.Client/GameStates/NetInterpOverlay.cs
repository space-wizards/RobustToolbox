using Robust.Shared.Enums;
using Robust.Client.Graphics;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Robust.Client.GameStates
{
    internal sealed class NetInterpOverlay : Overlay
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        private readonly EntityLookupSystem _lookup;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        private readonly ShaderInstance _shader;

        public NetInterpOverlay(EntityLookupSystem lookup)
        {
            IoCManager.InjectDependencies(this);
            _lookup = lookup;
            _shader = _prototypeManager.Index<ShaderPrototype>("unshaded").Instance();
        }

        protected internal override void Draw(in OverlayDrawArgs args)
        {
            var handle = args.DrawingHandle;
            handle.UseShader(_shader);
            var worldHandle = (DrawingHandleWorld) handle;
            var viewport = args.WorldAABB;

            foreach (var physics in _entityManager.EntityQuery<PhysicsComponent>(true))
            {
                // all entities have a TransformComponent
                var transform = _entityManager.GetComponent<TransformComponent>(physics.Owner);

                // if not on the same map, continue
                if (transform.MapID != _eyeManager.CurrentMap || physics.Owner.IsInContainer(_entityManager))
                    continue;

                // This entity isn't lerping, no need to draw debug info for it
                if(transform.NextPosition == null)
                    continue;

                var aabb = _lookup.GetWorldAABB(physics.Owner);

                // if not on screen, or too small, continue
                if (!aabb.Intersects(viewport) || aabb.IsEmpty())
                    continue;

                var boxOffset = transform.NextPosition.Value - transform.LocalPosition;
                var boxPosWorld = transform.WorldPosition + boxOffset;

                worldHandle.DrawLine(transform.WorldPosition, boxPosWorld, Color.Yellow);
                worldHandle.DrawRect(aabb.Translated(boxOffset), Color.Yellow.WithAlpha(0.5f), false);
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
