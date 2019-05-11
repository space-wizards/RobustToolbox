using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Interfaces.Console;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.GameStates
{
    internal class NetGraphOverlay : Overlay
    {
        public override OverlaySpace Space => OverlaySpace.ScreenSpace;

        private Font _font;

        public NetGraphOverlay() : base(nameof(NetGraphOverlay))
        {
            IoCManager.InjectDependencies(this);
            var cache = IoCManager.Resolve<IResourceCache>();
            _font = new VectorFont(cache.GetResource<FontResource>("/Nano/NotoSans/NotoSans-Regular.ttf"), 10);
        }

        protected override void Draw(DrawingHandle handle)
        {
            //TODO: Make me actually work!
            handle.DrawLine(new Vector2(50,50), new Vector2(100,100), Color.Green);
            DrawString((DrawingHandleScreen)handle, _font, new Vector2(60, 50), "Hello World!");
        }

        private void DrawString(DrawingHandleScreen handle, Font font, Vector2 pos, string str)
        {
            var baseLine = new Vector2(pos.X, font.GetAscent(1) + pos.Y);

            foreach (var chr in str)
            {
                var advance = font.DrawChar(handle, chr, baseLine, 1, Color.White);
                baseLine += new Vector2(advance, 0);
            }
        }

        private class NetShowGraphCommand : IConsoleCommand
        {
            public string Command => "net_graph";
            public string Help => "net_graph <0|1>";
            public string Description => "Toggles the net statistics pannel.";

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

                if(bValue && !overlayMan.HasOverlay(nameof(NetGraphOverlay)))
                {
                    overlayMan.AddOverlay(new NetGraphOverlay());
                    console.AddLine("Enabled network overlay.");
                }
                else if(overlayMan.HasOverlay(nameof(NetGraphOverlay)))
                {
                    overlayMan.RemoveOverlay(nameof(NetGraphOverlay));
                    console.AddLine("Disabled network overlay.");
                }

                return false;
            }
        }
    }
}
