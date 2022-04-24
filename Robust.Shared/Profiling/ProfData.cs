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

// TODO: Optimize union layouts

[StructLayout(LayoutKind.Explicit)]
public struct ProfCmd
{
    [FieldOffset(0)] public ProfCmdType Type;

    [FieldOffset(4)] public ProfCmdValue Value;
    [FieldOffset(4)] public ProfCmdSystemSample SystemSample;
    [FieldOffset(4)] public ProfCmdGroupEnd GroupEnd;
}

public struct ProfCmdValue
{
    public int StringId;
    public ProfValue Value;
}

public struct ProfCmdSystemSample
{
    public int StringId;
    public float Value;
}

public struct ProfCmdGroupEnd
{
    public int StartIndex;
    public int StringId;
    public ProfValue Value;
}

public enum ProfCmdType
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
    Int32
}

[StructLayout(LayoutKind.Explicit)]
public struct ProfValue
{
    [FieldOffset(0)] public ProfValueType Type;

    [FieldOffset(8)] public TimeAndAllocSample TimeAllocSample;
    [FieldOffset(8)] public int Int32;
}

public struct TimeAndAllocSample
{
    public float Time;
    public long Alloc;
}
