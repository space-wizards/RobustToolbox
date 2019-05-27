using System;
using Robust.Client.GameObjects;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Console;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Robust.Client.GameStates
{
    internal class NetInterpOverlay : Overlay
    {
#pragma warning disable 0649
        [Dependency] private readonly IComponentManager _componentManager;
        [Dependency] private readonly IEyeManager _eyeManager;
        [Dependency] private readonly IPrototypeManager _prototypeManager;
#pragma warning restore 0649

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
