using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Robust.Shared.Profiling;

// Contains data definitions for all data stored and buffered by the profiling system.

//
// An overview of how the profiling system works:
// All recorded profiling data is stored as a bunch of log entries (ProfLog) into a ring buffer (ProfBuffer).
// To snapshot, we simply memory copy these buffers entirely.
//
// No state is kept on the profile manager except the buffers to write into.
// Groups transparently support nesting, you can write anything at any time and have it grouped sanely.
// Visualization tools manually parse the buffer to discern groups, samples, etc.
// This is done so that there is minimal logic when actually recording, thus avoiding profiler overhead.
//
// We store an absolute int64 write index into the ring buffer, and only ever increment it.
// The ring buffer must be a power-of-two (POT) size, so that we can simply bitmask this absolute index
// to get a relative index into the buffer. This makes writing code very simple.
//
// To avoid scanning over the *whole* log buffer for visualization, we keep a separate index (ProfIndex) ring buffer.
// These indices store absolute indices into the main ProfLog ring buffer. Because they are absolute (see above)
// we can compare to check whether the index is still "valid" and not overwritten by a new frame yet:
// simply check whether the absolute index is < WriteOffset - Buffer.Length
// This is taken from Casey Muratori's refterm line index strategy: https://www.youtube.com/watch?v=hNZF81VYfQo
// This line index buffer *also* uses absolute indices, and must also be POT.
//
// The log and index buffer are separate sizes.
// This means the max valid frames tracked in a snapshot is limited by the log OR index buffer size.
// On the main menu, less events are generated, so the index buffer usually runs out first.
// In game, the log buffer runs out first instead.
//
// To make sure the log events are 100% unmanaged structs, ProfManager keeps a int <-> string tree to resolve strings.
// Because of this, we cannot currently store dynamic strings in the log buffer, to avoid memory leaks.
//
// Future improvements:
// Support variable length log entries so we can store strings. Might be complicated.
//   * Would require a double-mapped ring buffer to be sanely implemented.


/// <summary>
/// Static helper class to help with profiling data values.
/// </summary>
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

/// <summary>
/// A buffer containing a set of profiling data.
/// </summary>
public struct ProfBuffer
{
    // Both indices are unmasked and need to be & (buffer.LongLength - 1) before indexing.
    public long LogWriteOffset;
    public long IndexWriteOffset;
    public ProfLog[] LogBuffer;
    public ProfIndex[] IndexBuffer;

    /// <summary>
    /// Create a deep clone of this snapshot.
    /// </summary>
    /// <returns></returns>
    public readonly ProfBuffer Snapshot()
    {
        var ret = this;
        ret.LogBuffer = (ProfLog[]) ret.LogBuffer.Clone();
        ret.IndexBuffer = (ProfIndex[]) ret.IndexBuffer.Clone();
        return ret;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref ProfLog Log(long idx) => ref LogBuffer[idx & (LogBuffer.LongLength - 1)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref ProfIndex Index(long idx) => ref IndexBuffer[idx & (IndexBuffer.LongLength - 1)];
}

/// <summary>
/// An index into the profiling log buffer.
/// </summary>
public struct ProfIndex
{
    /// <summary>
    /// What type of data this index covers.
    /// </summary>
    public ProfIndexType Type;

    /// <summary>
    /// The position immediately after the last log event covered by the index.
    /// </summary>
    public long StartPos;

    /// <summary>
    /// The position immediately after the last log event covered by the index.
    /// </summary>
    public long EndPos;
}

/// <summary>
/// Types of profile indices.
/// </summary>
public enum ProfIndexType
{
    Invalid = 0,

    /// <summary>
    /// Index covers a full game loop iteration/render frame.
    /// </summary>
    Frame
}

// TODO: Optimize union layouts

//
// LOG EVENT TYPES:
// Value: A single stored ProfValue with a string name.
// GroupStart: Indicates the start of a new nested group. Does not contain any data.
// GroupEnd: Indicates the end of a new nested group.
//           Stores group name, group ProfValue, and index of matching GroupStart.
//

/// <summary>
/// A single log entry in the profiling system.
/// </summary>
/// <remarks>
/// This is a tagged union, use it appropriately.
/// </remarks>
[StructLayout(LayoutKind.Explicit)]
public struct ProfLog
{
    [FieldOffset(0)] public ProfLogType Type;

    [FieldOffset(8)] public ProfLogValue Value;
    [FieldOffset(8)] public ProfLogGroupEnd GroupEnd;
}

/// <summary>
/// Data for value log events.
/// </summary>
public struct ProfLogValue
{
    public int StringId;
    public ProfValue Value;
}

/// <summary>
/// Data for group end log events.
/// </summary>
public struct ProfLogGroupEnd
{
    public long StartIndex;
    public int StringId;
    public ProfValue Value;
}

/// <summary>
/// Types of profile log entries.
/// </summary>
public enum ProfLogType
{
    Invalid = 0,
    Value,
    GroupStart,
    GroupEnd,
}

/// <summary>
/// A single logged value in the profiling system.
/// </summary>
/// <remarks>
/// This is a tagged union, use it appropriately.
/// </remarks>
[StructLayout(LayoutKind.Explicit)]
public struct ProfValue
{
    [FieldOffset(0)] public ProfValueType Type;

    [FieldOffset(8)] public TimeAndAllocSample TimeAllocSample;
    [FieldOffset(8)] public int Int32;
    [FieldOffset(8)] public long Int64;
}

/// <summary>
/// Types of profiling values.
/// </summary>
public enum ProfValueType
{
    Invalid = 0,
    TimeAllocSample,
    Int32,
    Int64
}

/// <summary>
/// A sample containing time (in seconds) and amount allocated.
/// </summary>
public struct TimeAndAllocSample
{
    public float Time;
    public long Alloc;
}
