using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Robust.Shared.Profiling;

public static class ProfData
{
    public static ProfValue Int32(int int32)
    {
        return new ProfValue
        {
            Type = ProfValueType.Int32,
            Int32 = int32
        };
    }

    public static ProfValue Int64(long int64)
    {
        return new ProfValue
        {
            Type = ProfValueType.Int64,
            Int64 = int64
        };
    }

    public static ProfValue TimeAlloc(in ProfSampler sampler)
    {
        return new ProfValue
        {
            Type = ProfValueType.TimeAllocSample,
            TimeAllocSample = new TimeAndAllocSample
            {
                Alloc = sampler.ElapsedAlloc,
                Time = (float) sampler.Elapsed.TotalSeconds
            }
        };
    }
}

public struct ProfBuffer
{
    // Both indices are unmasked and need to be masked before indexing.
    public long BufferWriteOffset;
    public long IndexWriteOffset;
    public ProfLog[] Buffer;
    public ProfIndex[] Index;

    public readonly ProfBuffer Snapshot()
    {
        var ret = this;
        ret.Buffer = (ProfLog[]) ret.Buffer.Clone();
        ret.Index = (ProfIndex[]) ret.Index.Clone();
        return ret;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref ProfLog BufferIdx(long idx) => ref Buffer[idx & (Buffer.LongLength - 1)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref ProfIndex IndexIdx(long idx) => ref Index[idx & (Index.LongLength - 1)];
}

public struct ProfIndex
{
    public ProfIndexType Type;
    public long StartPos;
    public long EndPos;
}

public enum ProfIndexType
{
    Invalid = 0,
    TickSet,
    Frame
}

// TODO: Optimize union layouts

[StructLayout(LayoutKind.Explicit)]
public struct ProfLog
{
    [FieldOffset(0)] public ProfLogType Type;

    [FieldOffset(8)] public ProfLogValue Value;
    [FieldOffset(8)] public ProfLogSystemSample SystemSample;
    [FieldOffset(8)] public ProfLogGroupEnd GroupEnd;
}

public struct ProfLogValue
{
    public int StringId;
    public ProfValue Value;
}

public struct ProfLogSystemSample
{
    public int StringId;
    public float Value;
}

public struct ProfLogGroupEnd
{
    public long StartIndex;
    public int StringId;
    public ProfValue Value;
}

public enum ProfLogType
{
    Invalid = 0,
    Sample,
    GroupStart,
    GroupEnd,
}

public enum ProfValueType
{
    Invalid = 0,
    TimeAllocSample,
    Int32,
    Int64
}

[StructLayout(LayoutKind.Explicit)]
public struct ProfValue
{
    [FieldOffset(0)] public ProfValueType Type;

    [FieldOffset(8)] public TimeAndAllocSample TimeAllocSample;
    [FieldOffset(8)] public int Int32;
    [FieldOffset(8)] public long Int64;
}

public struct TimeAndAllocSample
{
    public float Time;
    public long Alloc;
}
