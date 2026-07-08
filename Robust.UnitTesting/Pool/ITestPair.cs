using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Robust.Shared.Timing;

namespace Robust.UnitTesting.Pool;

[NotContentImplementable]
public interface ITestPair
{
    int Id { get; }
    public Stopwatch Watch { get; }
    public PairState State { get; }
    public bool Initialized { get; }
    void Kill();

    [Obsolete("Constructed on the spot when needed, use ExtendedTestHistory instead.")]
    List<string> TestHistory => ExtendedTestHistory.Select(x => x.TestName).ToList();
    IReadOnlyList<TestHistoryEntry> ExtendedTestHistory { get; }
    PairSettings Settings { get; set; }

    int ServerSeed { get; }
    int ClientSeed { get; }

    void ActivateContext(TextWriter testOut);
    void ValidateSettings(PairSettings settings);
    void SetupSeed();
    void ClearModifiedCvars();
    void Use();
    Task Init(int id, BasePoolManager manager, PairSettings settings, TextWriter testOut, TextWriter? gravestone);
    Task RecycleInternal(PairSettings next, TextWriter testOut);
    Task ApplySettings(PairSettings settings);
    Task RunTicksSync(int ticks);
    Task SyncTicks(int targetDelta = 1);
    Task AddToHistory(string testName);
}

public enum PairState : byte
{
    Ready = 0,
    InUse = 1,
    CleanDisposed = 2,
    Dead = 3,
}
