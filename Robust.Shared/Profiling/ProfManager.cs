using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using Robust.Shared.Utility.Collections;

namespace Robust.Shared.Profiling;

// No interfaces here, don't want the interface dispatch overhead.
public sealed class ProfManager
{
    [IoC.Dependency] private readonly IConfigurationManager _cfg = default!;

    public bool IsEnabled;

    // I don't care that this isn't a tree I will call upon the string tree just like in BYOND.
    private readonly Dictionary<string, int> _stringTreeIndices = new();
    private ValueList<string> _stringTree;

    public ProfBuffer Buffer;

    internal void Initialize()
    {
        _cfg.OnValueChanged(CVars.ProfIndexSize, i =>
        {
            if (!BitOperations.IsPow2(i))
            {
                Logger.WarningS("prof", "Rounding prof.index_size to next POT");
                i = BufferHelpers.FittingPowerOfTwo(i);
            }

            Buffer.Index = new ProfIndex[i];
            Buffer.IndexWriteOffset = 0;
        }, true);

        _cfg.OnValueChanged(CVars.ProfBufferSize, i =>
        {
            if (!BitOperations.IsPow2(i))
            {
                Logger.WarningS("prof", "Rounding prof.buffer_size to next POT");
                i = BufferHelpers.FittingPowerOfTwo(i);
            }

            Buffer.Buffer = new ProfLog[i];
            // Invalidate all indices by artificially incrementing the write position.
            Buffer.BufferWriteOffset += i;
        }, true);

        _cfg.OnValueChanged(CVars.ProfEnabled, b => IsEnabled = b, true);
    }

    public void MarkIndex(long start)
    {
        if (!IsEnabled)
            return;

        // Ha
        var indexIdx = Buffer.IndexWriteOffset++;

        ProfIndex index = default;
        index.StartPos = start;
        index.EndPos = Buffer.BufferWriteOffset;

        Buffer.IndexIdx(indexIdx) = index;
    }

    public long WriteSample(string text, in ProfValue value)
    {
        if (!IsEnabled)
            return 0;

        var stringRef = InsertString(text);

        var idx = Buffer.BufferWriteOffset;
        ref var cmd = ref WriteCmd();
        cmd = default;
        cmd.Type = ProfLogType.Sample;
        cmd.Value.Value = value;
        cmd.Value.StringId = stringRef;

        return idx;
    }

    public long WriteSample(string text, in ProfSampler sampler)
    {
        return WriteSample(text, ProfData.TimeAlloc(sampler));
    }

    public long WriteGroupStart()
    {
        if (!IsEnabled)
            return 0;

        var idx = Buffer.BufferWriteOffset;
        ref var cmd = ref WriteCmd();
        cmd = default;
        cmd.Type = ProfLogType.GroupStart;

        return idx;
    }

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

    public void WriteGroupEnd(long startIndex, string text, in ProfSampler sampler)
    {
        WriteGroupEnd(startIndex, text, ProfData.TimeAlloc(sampler));
    }

    public GroupGuard Group(string name)
    {
        var start = WriteGroupStart();
        return new GroupGuard(this, start, name);
    }

    public string GetString(int stringIdx) => _stringTree[stringIdx];

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
        var buf = Buffer.Buffer;
        // This uses LongLength because it saves a single instruction to load the value from memory.
        // I spent more time on this function in sharplab than was probably worth it.
        var idx = Buffer.BufferWriteOffset & (buf.LongLength - 1);

        Buffer.BufferWriteOffset += 1;

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
