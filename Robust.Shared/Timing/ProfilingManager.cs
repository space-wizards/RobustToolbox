using System.Collections.Generic;
using Robust.Shared.Utility.Collections;

namespace Robust.Shared.Timing;

// No interfaces here,
public sealed class ProfilingManager
{
    public bool IsEnabled;

    // I don't care that this isn't a tree I will call upon the string tree just like in BYOND.
    private readonly Dictionary<string, int> _stringTreeIndices = new();
    private ValueList<string> _stringTree;

    public struct Cmd
    {
        public CmdType Type;

        public int StringId;
        public float Value;
    }

    public struct CmdSample
    {
        // public
    }

    public enum CmdType
    {
        Invalid,
        Sample
    }
}
