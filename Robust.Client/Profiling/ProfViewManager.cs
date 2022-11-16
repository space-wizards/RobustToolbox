using System;
using Robust.Shared.Collections;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Profiling;
using Robust.Shared.Timing;

namespace Robust.Client.Profiling;

/// <summary>
/// Manager for managing recording and snapshots of profiles, consuming shared <see cref="ProfManager"/>
/// </summary>
public sealed class ProfViewManager
{
    [Dependency]
    private readonly ProfManager _profManager = default!;

    public ValueList<Snapshot> Snapshots;

    public event Action? SnapshotsUpdated;

    private int _nextId = 1;

    public void Snap()
    {
        var buffer = _profManager.Buffer.Snapshot();

        var lowestIndex = CalcLowestIndex(buffer);
        var highestIndex = buffer.IndexWriteOffset - 1;

        long start;
        long end;
        if (lowestIndex == buffer.IndexWriteOffset)
        {
            // Literally not a single valid index. Time to give up.
            start = 0;
            end = 0;
        }
        else
        {
            (start, end) = CalcBufferRange(lowestIndex, highestIndex, buffer);
        }

        Snapshots.Add(new Snapshot
        {
            StartFrame = start,
            EndFrame = end,
            LowestValidIndex = lowestIndex,
            Buffer = buffer,
            Identifier = _nextId++
        });

        SnapshotsUpdated?.Invoke();
    }

    public void DeleteSnapshot(Snapshot snapshot)
    {
        if (Snapshots.Remove(snapshot))
            SnapshotsUpdated?.Invoke();
    }

    private static long CalcLowestIndex(in ProfBuffer buffer)
    {
        // Any commands further back than this index are invalid.
        // Indices in their range are thus also invalid and to be ignored.
        var cmdBufferStartValid = buffer.LogWriteOffset - buffer.LogBuffer.LongLength;

        // Just scan over the index ring buffer in reverse to find the earliest index that's still valid.
        var min = buffer.IndexWriteOffset - buffer.IndexBuffer.LongLength;
        for (var i = buffer.IndexWriteOffset - 1; i >= min; i--)
        {
            ref var index = ref buffer.Index(i);
            if (index.StartPos < cmdBufferStartValid)
                return i + 1;
        }

        return min;
    }

    private (long start, long end) CalcBufferRange(long lowestIndex, long highestIndex, in ProfBuffer buffer)
    {
        return (GetFrameOfIndex(lowestIndex, buffer), GetFrameOfIndex(highestIndex, buffer));
    }

    private long GetFrameOfIndex(long index, in ProfBuffer buffer)
    {
        ref var indexRef = ref buffer.Index(index);
        ref var firstLog = ref buffer.Log(indexRef.StartPos);

        if (firstLog.Type != ProfLogType.Value ||
            _profManager.GetString(firstLog.Value.StringId) != GameLoop.ProfTextStartFrame)
            throw new InvalidOperationException("Unable to find start frame");

        if (firstLog.Value.Value.Type != ProfValueType.Int64)
            throw new InvalidOperationException("Start frame has incorrect value type");

        return firstLog.Value.Value.Int64;
    }

    public long GetIndexOfFrame(long frame, Snapshot snapshot)
    {
        for (var i = snapshot.Buffer.IndexWriteOffset - 1; i >= snapshot.LowestValidIndex; i--)
        {
            if (GetFrameOfIndex(i, snapshot.Buffer) == frame)
                return i;
        }

        return 0;
    }

    public sealed class Snapshot
    {
        public int Identifier;

        public long LowestValidIndex;
        public long StartFrame;
        public long EndFrame;
        public ProfBuffer Buffer;

        public long FrameCount => EndFrame - StartFrame + 1;
    }
}

public sealed class ProfSnapshotCommand : LocalizedCommands
{
    [Dependency] private readonly ProfViewManager _profView = default!;

    // ReSharper disable once StringLiteralTypo
    public override string Command => "profsnap";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _profView.Snap();
    }
}
