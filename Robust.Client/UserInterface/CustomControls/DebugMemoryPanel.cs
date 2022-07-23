using System;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls
{
    internal sealed class DebugMemoryPanel : PanelContainer
    {
        private readonly Label _label;

        private readonly char[] _textBuffer = new char[512];
        private readonly long[] _allocDeltas = new long[60];
        private long _lastAllocated;
        private int _allocDeltaIndex;

        public DebugMemoryPanel()
        {
            HorizontalAlignment = HAlignment.Left;

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

            _label.TextMemory = GetMemoryInfo();
        }

        private ReadOnlyMemory<char> GetMemoryInfo()
        {
            var gen0 = GC.CollectionCount(0);
            var gen1 = GC.CollectionCount(1);
            var gen2 = GC.CollectionCount(2);

            var allocated = GC.GetTotalMemory(false);
            LogAllocSize(allocated);
            var info = GC.GetGCMemoryInfo();

            return FormatHelpers.FormatIntoMem(
                _textBuffer,
                $@"Total Allocated: {allocated / 1024:N0} KiB
Total Collections: {gen0} {gen1} {gen2}
Alloc Rate: {CalculateAllocRate() / 1024:N0} KiB / frame
Last GC: {info.Index} Gen: {info.Generation} BGC: {info.Concurrent} C: {info.Compacted}
  Pause: {info.PauseDurations[0].TotalMilliseconds}ms
  Heap: {info.HeapSizeBytes / 1024:N0} KiB
  Fragmented: {info.FragmentedBytes / 1024:N0} KiB");
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
            var sum = 0L;
            var count = 0;
            foreach (var val in _allocDeltas)
            {
                if (val >= 0)
                {
                    sum += val;
                    count += 1;
                }
            }

            return count == 0 ? 0 : sum / count;
        }
    }
}
