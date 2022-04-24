using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Robust.Shared.Utility.Collections;

namespace Robust.Shared.Profiling;

// No interfaces here, don't want the interface dispatch overhead.
public sealed class ProfManager
{
    public bool IsEnabled;

    // I don't care that this isn't a tree I will call upon the string tree just like in BYOND.
    private readonly Dictionary<string, int> _stringTreeIndices = new();
    private ValueList<string> _stringTree;

    public ValueList<ProfCmd> CommandsA;
    public ValueList<ProfCmd> CommandsB;
    public bool IsA;

    public void WriteSample(string text, in ProfValue value)
    {
        if (!IsEnabled)
            return;

        var stringRef = InsertString(text);

        ref var cmds = ref CurCmds();

        ref var cmd = ref cmds.AddRef();
        cmd = default;
        cmd.Type = ProfCmdType.Sample;
        cmd.Value.Value = value;
        cmd.Value.StringId = stringRef;
    }

    public void WriteSample(string text, in ProfSampler sampler)
    {
        WriteSample(text, ProfData.TimeAlloc(sampler));
    }

    public int WriteGroupStart()
    {
        if (!IsEnabled)
            return 0;

        ref var cmds = ref CurCmds();

        ref var cmd = ref cmds.AddRef();
        cmd = default;
        cmd.Type = ProfCmdType.GroupStart;

        return cmds.Count - 1;
    }

    public void WriteGroupEnd(int startIndex, string text, in ProfValue value)
    {
        if (!IsEnabled)
            return;

        var stringRef = InsertString(text);

        ref var cmds = ref CurCmds();

        ProfCmd cmd = default;
        cmd.Type = ProfCmdType.GroupEnd;
        cmd.GroupEnd.StringId = stringRef;
        cmd.GroupEnd.StartIndex = startIndex;
        cmd.GroupEnd.Value = value;

        cmds.Add(cmd);
    }

    public void WriteGroupEnd(int startIndex, string text, in ProfSampler sampler)
    {
        WriteGroupEnd(startIndex, text, ProfData.TimeAlloc(sampler));
    }

    public GroupGuard Group(string name)
    {
        var start = WriteGroupStart();
        return new GroupGuard(this, start, name);
    }

    public void Swap()
    {
        IsA = !IsA;
        CurCmds().Clear();
    }

    public string GetString(int stringIdx) => _stringTree[stringIdx];

    public ref ValueList<ProfCmd> CurCmds() => ref IsA ? ref CommandsB : ref CommandsA;
    public ref ValueList<ProfCmd> LastCmds() => ref IsA ? ref CommandsA : ref CommandsB;

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

    public readonly struct GroupGuard : IDisposable
    {
        private readonly ProfManager _mgr;
        private readonly int _startIndex;
        private readonly string _groupName;
        private readonly ProfSampler _sampler;

        public GroupGuard(ProfManager mgr, int startIndex, string groupName)
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
