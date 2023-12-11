using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Robust.Server.GameStates;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Robust.Benchmarks.Pvs;

/// <summary>
/// Benchmark for optimizing <see cref="PvsSystem.ProcessLeavePvs"/>.. In particular, this checks whether its better to
/// check <see cref="HashSet{T}.Contains"/> first, or whether its better to data from a dictionary and then infer
/// the hashset contents.
/// </summary>
public class ProcessLeavePvsBenchmark
{
    /// <summary>
    /// Number of entities in PVS range.
    /// </summary>
    public const int InView = 4000;

    /// <summary>
    /// Number of entities that have left PVS range.
    /// </summary>
    [Params(0, 500, 2000, 4000)]
    public int LeftView { get; set; }

    /// <summary>
    /// Total number of entities.
    /// </summary>
    public const int N = InView * 10;

    public struct DataStruct
    {
        public EntityUid Uid;
        public GameTick LastSent;
        public GameTick LastLeft;
    }

    public GameTick CurTick = new(42);

    public Dictionary<EntityUid, DataStruct> EntityData = new(N);
    public HashSet<EntityUid> ToSend = new(InView);
    public HashSet<EntityUid> LastSent = new(InView);

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);

        var ents = new EntityUid[N];
        for (int i = 0; i < N; i++)
        {
            ents[i] = new EntityUid(i);
        }

        // Shuffle array, just in case the order in which they are added to the dictionary matters.
        Shuffle(ents, rng);

        foreach (var ent in ents)
        {
            EntityData.Add(ent, new() {Uid = ent});
        }

        // Randomly take some number of entities as currently "in view"
        Shuffle(ents, rng);
        ToSend = new(ents.Take(InView));

        // Take another set of entities with some overlap
        LastSent = new(ents.Skip(LeftView).Take(InView));

        if (LastSent.Intersect(ToSend).Count() != InView - LeftView)
            throw new Exception("Setup failed");

        foreach (var ent in LastSent)
        {
            EntityData[ent] = new DataStruct {Uid = ent, LastSent = CurTick - 1};
        }

        foreach (var ent in ToSend)
        {
            EntityData[ent] = new DataStruct {Uid = ent, LastSent = CurTick};
        }
    }

    static void Shuffle<T>(T[] arr, Random rng)
    {
        var n = arr.Length;
        while (n > 1)
        {
            n -= 1;
            var k = rng.Next(n + 1);
            (arr[k], arr[n]) = (arr[n], arr[k]);
        }
    }

    [Benchmark(Baseline = true)]
    public int ProcessLeaveHashset()
    {
        var total = 0;
        foreach (var ent in LastSent)
        {
            if (ToSend.Contains(ent))
                continue;

            ref var data = ref CollectionsMarshal.GetValueRefOrNullRef(EntityData, ent);
            if (Unsafe.IsNullRef(ref data))
                continue;

            data.LastLeft = CurTick;
            total += 1;
        }

        return total;
    }

    [Benchmark]
    public int ProcessLeaveDictionary()
    {
        var total = 0;
        foreach (var ent in LastSent)
        {
            ref var data = ref CollectionsMarshal.GetValueRefOrNullRef(EntityData, ent);
            if (Unsafe.IsNullRef(ref data))
                continue;

            if (data.LastSent == CurTick)
                continue;

            data.LastLeft = CurTick;
            total += 1;
        }

        return total;
    }
}
