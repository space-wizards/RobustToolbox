using System.Diagnostics.Tracing;

namespace Robust.Shared.Replays;

internal abstract partial class SharedReplayRecordingManager
{
    [EventSource(Name = "Robust.ReplayRecording")]
    public sealed class RecordingEventSource : EventSource
    {
        public static RecordingEventSource Log { get; } = new();

        [Event(1)]
        public void WriteTaskStart(int task) => WriteEvent(1, task);

        [Event(2)]
        public void WriteTaskStop(int task) => WriteEvent(2, task);

        [Event(3)]
        public void WriteBatchStart(int index) => WriteEvent(3, index);

        [Event(4)]
        public void WriteBatchStop(int index) => WriteEvent(4, index);

        [Event(5)]
        public void WriteQueueBlocked() => WriteEvent(5);
    }
}
