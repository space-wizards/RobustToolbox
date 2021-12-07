using Robust.Shared.Enums;
using Robust.Client.Graphics;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Robust.Client.GameStates
{
    internal class NetInterpOverlay : Overlay
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        private readonly ShaderInstance _shader;

        public NetInterpOverlay()
        {
            IoCManager.InjectDependencies(this);
            _shader = _prototypeManager.Index<ShaderPrototype>("unshaded").Instance();
        }

        protected internal override void Draw(in OverlayDrawArgs args)
        {
            var handle = args.DrawingHandle;
            handle.UseShader(_shader);
            var worldHandle = (DrawingHandleWorld) handle;
            var viewport = _eyeManager.GetWorldViewport();
            foreach (var boundingBox in _entityManager.EntityQuery<IPhysBody>(true))
            {
                // all entities have a TransformComponent
                var transform = _entityManager.GetComponent<TransformComponent>(boundingBox.Owner);

                // if not on the same map, continue
                if (transform.MapID != _eyeManager.CurrentMap || boundingBox.Owner.IsInContainer())
                    continue;

                // This entity isn't lerping, no need to draw debug info for it
                if(transform.LerpDestination == null)
                    continue;

                var aabb = boundingBox.GetWorldAABB();

                // if not on screen, or too small, continue
                if (!aabb.Translated(transform.WorldPosition).Intersects(viewport) || aabb.IsEmpty())
                    continue;

                var timing = IoCManager.Resolve<IGameTiming>();
                timing.InSimulation = true;

                var boxOffset = transform.LerpDestination.Value - transform.LocalPosition;
                var boxPosWorld = transform.WorldPosition + boxOffset;

                timing.InSimulation = false;

                worldHandle.DrawLine(transform.WorldPosition, boxPosWorld, Color.Yellow);
                worldHandle.DrawRect(aabb.Translated(boxPosWorld), Color.Yellow.WithAlpha(0.5f), false);

            }
        }

        private class NetShowInterpCommand : IConsoleCommand
        {
            public string Command => "net_draw_interp";
            public string Help => "net_draw_interp <0|1>";
            public string Description => "Toggles the debug drawing of the network interpolation.";

            public void Execute(IConsoleShell shell, string argStr, string[] args)
            {
                if (args.Length != 1)
                {
                    shell.WriteError("Invalid argument amount. Expected 2 arguments.");
                    return;
                }

                if (!byte.TryParse(args[0], out var iValue))
                {
                    shell.WriteLine("Invalid argument: Needs to be 0 or 1.");
                    return;
                }

                var bValue = iValue > 0;
                var overlayMan = IoCManager.Resolve<IOverlayManager>();

                if (bValue && !overlayMan.HasOverlay<NetInterpOverlay>())
                {
                    overlayMan.AddOverlay(new NetInterpOverlay());
                    shell.WriteLine("Enabled network interp overlay.");
                }
                else if (overlayMan.HasOverlay<NetInterpOverlay>())
                {
                    overlayMan.RemoveOverlay<NetInterpOverlay>();
                    shell.WriteLine("Disabled network interp overlay.");
                }
            }
        }
    }
}
