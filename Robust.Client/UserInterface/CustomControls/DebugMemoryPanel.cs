using System;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.CustomControls
{
    internal sealed class DebugMemoryPanel : PanelContainer
    {
        private readonly Label _label;

        public DebugMemoryPanel()
        {
            // Disable this panel outside .NET Core since it's useless there.
#if !NETCOREAPP
            Visible = false;
#endif

            SizeFlagsHorizontal = SizeFlags.None;

            AddChild(_label = new Label
            {
                MarginTop = 5,
                MarginLeft = 5
            });

            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#7d41ff8a")
            };

            PanelOverride.SetContentMarginOverride(StyleBox.Margin.All, 4);

            MouseFilter = _label.MouseFilter = MouseFilterMode.Ignore;
        }

        protected override void FrameUpdate(RenderFrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (!VisibleInTree)
            {
                return;
            }

            _label.Text = GetMemoryInfo();
        }

        private static string GetMemoryInfo()
        {
#if NETCOREAPP
            var info = GC.GetGCMemoryInfo();
            return $@"Heap Size: {FormatBytes(info.HeapSizeBytes)}
Total Allocated: {FormatBytes(GC.GetTotalMemory(false))}";
#else
            return "Memory information needs .NET Core"
#endif
        }

        private static string FormatBytes(long bytes)
        {
            return $"{bytes / 1024} KiB";
        }
    }
}
