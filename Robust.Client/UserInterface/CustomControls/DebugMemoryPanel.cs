using System;
using System.Linq;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Client.UserInterface.CustomControls
{
    internal sealed class DebugMemoryPanel : PanelContainer
    {
        private readonly Label _label;

        private readonly long[] _allocDeltas = new long[60];
        private long _lastAllocated;
        private int _allocDeltaIndex;

        public DebugMemoryPanel()
        {
            SizeFlagsHorizontal = SizeFlags.None;

            AddChild(_label = new Label());

            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#7d41ff8a")
            };

            PanelOverride.SetContentMarginOverride(StyleBox.Margin.All, 4);

            MouseFilter = _label.MouseFilter = MouseFilterMode.Ignore;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (!VisibleInTree)
            {
                return;
            }

            _label.Text = GetMemoryInfo();
        }

        private string GetMemoryInfo()
        {
            var gen0 = GC.CollectionCount(0);
            var gen1 = GC.CollectionCount(1);
            var gen2 = GC.CollectionCount(2);

            var allocated = GC.GetTotalMemory(false);
            LogAllocSize(allocated);
            var info = GC.GetGCMemoryInfo();

            return $@"Total Allocated: {FormatBytes(allocated)}
Total Collections: {gen0} {gen1} {gen2}
Alloc Rate: {FormatBytes(CalculateAllocRate())} / frame
Last GC: {info.Index} Gen: {info.Generation} BGC: {info.Concurrent} C: {info.Compacted}
  Pause: {info.PauseDurations[0].TotalMilliseconds}ms
  Heap: {FormatBytes(info.HeapSizeBytes)}
  Fragmented: {FormatBytes(info.FragmentedBytes)}";
        }

        private static string FormatBytes(long bytes)
        {
            return $"{bytes / 1024} KiB";
        }

        private void LogAllocSize(long allocated)
        {
            var delta = allocated - _lastAllocated;
            _lastAllocated = allocated;

            // delta is < 0 if the GC ran a collection so it dropped.
            // In that case, treat it as a dud by writing write -1.
            _allocDeltas[_allocDeltaIndex++ % _allocDeltas.Length] = Math.Max(-1, delta);
        }

        private long CalculateAllocRate()
        {
            return (long) _allocDeltas.Where(x => x >= 0).Average();
        }
    }
}
