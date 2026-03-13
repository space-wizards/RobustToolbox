using System.Diagnostics.Tracing;

namespace Robust.UnitTesting.Pool;

[EventSource(Name = "Robust.UnitTesting.PoolManagerEvents")]
internal sealed class PoolManagerEvents : EventSource
{
    public static readonly PoolManagerEvents Log = new();

    [Event(1)]
    public void PairCreated(string type, int id, string reason, string testName) => WriteEvent(1, type, id, reason, testName);

    [Event(2)]
    public void PairFinishedInit(string type, int id) => WriteEvent(2, type, id);

    [Event(3)]
    public void PairDestroyed(string type, int id) => WriteEvent(3, type, id);

    [Event(4)]
    public void PairCleanReturned(string type, int id) => WriteEvent(4, type, id);

    [Event(5)]
    public void PairDirtyReturned(string type, int id) => WriteEvent(5, type, id);

    [Event(6)]
    public void PairRetrieved(string type, int id, string testName) => WriteEvent(6, type, id, testName);

    [NonEvent]
    private unsafe void WriteEvent(int eventId, string arg1, int arg2, string arg3)
    {
        fixed (char* arg1Ptr = arg1)
        fixed (char* arg3Ptr = arg3)
        {
            var dataDesc = stackalloc EventData[3];

            dataDesc[0].DataPointer = (nint)arg1Ptr;
            dataDesc[0].Size = (arg1.Length + 1) * 2;
            dataDesc[1].DataPointer = (nint)(&arg2);
            dataDesc[1].Size = 4;
            dataDesc[2].DataPointer = (nint)arg3Ptr;
            dataDesc[2].Size = (arg3.Length + 1) * 2;

            WriteEventCore(eventId, 3, dataDesc);
        }
    }

    [NonEvent]
    private unsafe void WriteEvent(int eventId, string arg1, int arg2, string arg3, string arg4)
    {
        fixed (char* arg1Ptr = arg1)
        fixed (char* arg3Ptr = arg3)
        fixed (char* arg4Ptr = arg4)
        {
            var dataDesc = stackalloc EventData[4];

            dataDesc[0].DataPointer = (nint)arg1Ptr;
            dataDesc[0].Size = (arg1.Length + 1) * 2;
            dataDesc[1].DataPointer = (nint)(&arg2);
            dataDesc[1].Size = 4;
            dataDesc[2].DataPointer = (nint)arg3Ptr;
            dataDesc[2].Size = (arg3.Length + 1) * 2;
            dataDesc[3].DataPointer = (nint)arg4Ptr;
            dataDesc[3].Size = (arg4.Length + 1) * 2;

            WriteEventCore(eventId, 4, dataDesc);
        }
    }
}
