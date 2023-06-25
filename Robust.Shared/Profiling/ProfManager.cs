using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.Profiling;

// No interfaces here, don't want the interface dispatch overhead.

// See ProfData.cs for description of profiling data layout.

public sealed class ProfManager
{
    [IoC.Dependency] private readonly IConfigurationManager _cfg = default!;
    [IoC.Dependency] private readonly ILogManager _logManager = default!;

    /// <summary>
    /// Proxy to <c>prof.enabled</c> CVar.
    /// </summary>
    public bool IsEnabled { get; private set; }

    // I don't care that this isn't a tree I will call upon the string tree just like in BYOND.
    private readonly Dictionary<string, int> _stringTreeIndices = new();
    private ValueList<string> _stringTree;

    public ProfBuffer Buffer;

    private ISawmill? _sawmill = default!;

    internal void Initialize()
    {
        _sawmill = _logManager.GetSawmill("prof");

        _cfg.OnValueChanged(CVars.ProfIndexSize, i =>
        {
            if (!BitOperations.IsPow2(i))
            {
                _sawmill.Warning("Rounding prof.index_size to next POT");
                i = BufferHelpers.FittingPowerOfTwo(i);
            }

            Buffer.IndexBuffer = new ProfIndex[i];
            Buffer.IndexWriteOffset = 0;
        }, true);

        _cfg.OnValueChanged(CVars.ProfBufferSize, i =>
        {
            if (!BitOperations.IsPow2(i))
            {
                _sawmill.Warning("Rounding prof.buffer_size to next POT");
                i = BufferHelpers.FittingPowerOfTwo(i);
            }

            Buffer.LogBuffer = new ProfLog[i];
            // Invalidate all indices by artificially incrementing the write position.
            Buffer.LogWriteOffset += i;
        }, true);

        _cfg.OnValueChanged(CVars.ProfEnabled, b => IsEnabled = b, true);
    }

    /// <summary>
    /// Write an index covering the region from <paramref name="start"/> to the current write position.
    /// </summary>
    /// <param name="start">The absolute start index of </param>
    /// <param name="type">The type of index to mark.</param>
    public void MarkIndex(long start, ProfIndexType type)
    {
        if (!IsEnabled)
            return;

        // Ha
        var indexIdx = Buffer.IndexWriteOffset++;

        ProfIndex index = default;
        index.StartPos = start;
        index.EndPos = Buffer.LogWriteOffset;
        index.Type = type;

        Buffer.Index(indexIdx) = index;
    }

    /// <summary>
    /// Write a single profiling value to the log.
    /// </summary>
    /// <param name="text">The name of the entry.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>The absolute position of the written log entry.</returns>
    public long WriteValue(string text, in ProfValue value)
    {
        if (!IsEnabled)
            return 0;

        var stringRef = InsertString(text);

        var idx = Buffer.LogWriteOffset;
        ref var cmd = ref WriteCmd();
        cmd = default;
        cmd.Type = ProfLogType.Value;
        cmd.Value.Value = value;
        cmd.Value.StringId = stringRef;

        return idx;
    }

    /// <summary>
    /// Write a single profiling value to the log. Automatically samples the given sampler.
    /// </summary>
    /// <param name="text">The name of the entry.</param>
    /// <param name="sampler">The value of the sampler is measured to store a <see cref="TimeAndAllocSample"/>.</param>
    /// <returns>The absolute position of the written log entry.</returns>
    public long WriteValue(string text, in ProfSampler sampler) => WriteValue(text, ProfData.TimeAlloc(sampler));

    /// <summary>
    /// Write a single profiling value to the log.
    /// </summary>
    /// <param name="text">The name of the entry.</param>
    /// <param name="int32">The value to write.</param>
    /// <returns>The absolute position of the written log entry.</returns>
    public long WriteValue(string text, int int32) => WriteValue(text, ProfData.Int32(int32));

    /// <summary>
    /// Write a single profiling value to the log.
    /// </summary>
    /// <param name="text">The name of the entry.</param>
    /// <param name="int64">The value to write.</param>
    /// <returns>The absolute position of the written log entry.</returns>
    public long WriteValue(string text, long int64) => WriteValue(text, ProfData.Int64(int64));

    /// <summary>
    /// Write the start of a new log group.
    /// </summary>
    /// <returns>The absolute position of the written log entry.</returns>
    public long WriteGroupStart()
    {
        if (!IsEnabled)
            return 0;

        var idx = Buffer.LogWriteOffset;
        ref var cmd = ref WriteCmd();
        cmd = default;
        cmd.Type = ProfLogType.GroupStart;

        return idx;
    }

    /// <summary>
    /// Write the end of a log group.
    /// </summary>
    /// <param name="startIndex">The index of the matching group start.</param>
    /// <param name="text">The name of the group.</param>
    /// <param name="value">The value of the group.</param>
    public void WriteGroupEnd(long startIndex, string text, in ProfValue value)
    {
        if (!IsEnabled)
            return;

        var stringRef = InsertString(text);

        ref var cmd = ref WriteCmd();
        cmd = default;
        cmd.Type = ProfLogType.GroupEnd;
        cmd.GroupEnd.StringId = stringRef;
        cmd.GroupEnd.StartIndex = startIndex;
        cmd.GroupEnd.Value = value;
    }
    /// <summary>
    /// Write the end of a log group.
    /// </summary>
    /// <param name="startIndex">The index of the matching group start.</param>
    /// <param name="text">The name of the group.</param>
    /// <param name="sampler">The value of the sampler is measured to store a <see cref="TimeAndAllocSample"/>.</param>
    public void WriteGroupEnd(long startIndex, string text, in ProfSampler sampler)
    {
        WriteGroupEnd(startIndex, text, ProfData.TimeAlloc(sampler));
    }

    /// <summary>
    /// Make a guarded group for usage with using blocks.
    /// </summary>
    public GroupGuard Group(string name)
    {
        var start = WriteGroupStart();
        return new GroupGuard(this, start, name);
    }

    /// <summary>
    /// Get the value for a string in the string tree.
    /// </summary>
    public string GetString(int stringId) => _stringTree[stringId];

    /// <summary>
    /// Get the index of a string in the string tree.
    /// </summary>
    public int? GetStringIdx(string stringValue)
    {
        if (_stringTreeIndices.TryGetValue(stringValue, out var idx))
            return idx;

        return null;
    }

    private int InsertString(string text)
    {
        ref var stringRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_stringTreeIndices, text, out var exists);
        if (!exists)
        {
            stringRef = _stringTree.Count;
            _stringTree.Add(text);
        }

        return stringRef;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref ProfLog WriteCmd()
    {
        var buf = Buffer.LogBuffer;
        // This uses LongLength because it saves a single instruction to load the value from memory.
        // I spent more time on this function in sharplab than was probably worth it.
        var idx = Buffer.LogWriteOffset & (buf.LongLength - 1);

        Buffer.LogWriteOffset += 1;

        return ref buf[(int)idx];
    }

    public readonly struct GroupGuard : IDisposable
    {
        private readonly ProfManager _mgr;
        private readonly long _startIndex;
        private readonly string _groupName;
        private readonly ProfSampler _sampler;

        public GroupGuard(ProfManager mgr, long startIndex, string groupName)
        {
            _mgr = mgr;
            _startIndex = startIndex;
            _groupName = groupName;
            _sampler = ProfSampler.StartNew();
        }

        public void Dispose()
        {
            _mgr.WriteGroupEnd(_startIndex, _groupName, _sampler);
        }
    }
}
