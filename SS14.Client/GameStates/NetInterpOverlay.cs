using System;
using SS14.Client.GameObjects;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Graphics.Overlays;
using SS14.Client.Graphics.Shaders;
using SS14.Client.Interfaces.Console;
using SS14.Client.Interfaces.Graphics.ClientEye;
using SS14.Client.Interfaces.Graphics.Overlays;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Prototypes;

namespace SS14.Client.GameStates
{
    internal class NetInterpOverlay : Overlay
    {
        [Dependency] private readonly IComponentManager _componentManager;
        [Dependency] private readonly IEyeManager _eyeManager;
        [Dependency] private readonly IPrototypeManager _prototypeManager;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        public NetInterpOverlay() : base(nameof(NetInterpOverlay))
        {
            IoCManager.InjectDependencies(this);
            Shader = _prototypeManager.Index<ShaderPrototype>("unshaded").Instance();
        }
        protected override void Draw(DrawingHandle handle)
        {
            var worldHandle = (DrawingHandleWorld) handle;
            var viewport = _eyeManager.GetWorldViewport();
            foreach (var boundingBox in _componentManager.GetAllComponents<ClientBoundingBoxComponent>())
            {
                // all entities have a TransformComponent
                var transform = boundingBox.Owner.Transform;

                // if not on the same map, continue
                if (transform.MapID != _eyeManager.CurrentMap || !transform.IsMapTransform)
                    continue;

                // This entity isn't lerping, no need to draw debug info for it
                if(transform.LocalPosition == transform.LerpDestination)
                    continue;

                var aabb = boundingBox.AABB;

                // if not on screen, or too small, continue
                if (!aabb.Translated(transform.WorldPosition).Intersects(viewport) || aabb.IsEmpty())
                    continue;
                
                var timing = IoCManager.Resolve<IGameTiming>();
                timing.InSimulation = true;
                
                var boxOffset = transform.LerpDestination - transform.LocalPosition;
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

            public bool Execute(IDebugConsole console, params string[] args)
            {
                if (args.Length != 1)
                {
                    console.AddLine("Invalid argument amount. Expected 2 arguments.", Color.Red);
                    return false;
                }

                if (!byte.TryParse(args[0], out var iValue))
                {
                    console.AddLine("Invalid argument: Needs to be 0 or 1.");
                    return false;
                }

                var bValue = iValue > 0;
                var overlayMan = IoCManager.Resolve<IOverlayManager>();

                if (bValue && !overlayMan.HasOverlay(nameof(NetInterpOverlay)))
                {
                    overlayMan.AddOverlay(new NetInterpOverlay());
                    console.AddLine("Enabled network interp overlay.");
                }
                else if (overlayMan.HasOverlay(nameof(NetInterpOverlay)))
                {
                    overlayMan.RemoveOverlay(nameof(NetInterpOverlay));
                    console.AddLine("Disabled network interp overlay.");
                }

                return false;
            }
        }
    }
}
