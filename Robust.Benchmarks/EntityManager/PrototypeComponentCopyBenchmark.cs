using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Robust.Benchmarks.Serialization;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Benchmarks.EntityManager;

public partial class PrototypeComponentCopyBenchmark : SerializationBenchmark
{
    private IComponent[] _sources = default!;
    private IComponent[] _targets = default!;

    [Params(1, 10, 100)]
    public int N;

    [GlobalSetup]
    public void GlobalSetup()
    {
        InitializeSerialization();

        _sources =
        [
            new PrototypeCopyBenchComponent01A(),
            new PrototypeCopyBenchComponent01B(),
            new PrototypeCopyBenchComponent01C(),
            new PrototypeCopyBenchComponent01D(),
            new PrototypeCopyBenchComponent01E(),
            new PrototypeCopyBenchComponent02A(),
            new PrototypeCopyBenchComponent02B(),
            new PrototypeCopyBenchComponent02C(),
            new PrototypeCopyBenchComponent02D(),
            new PrototypeCopyBenchComponent02E(),
            new PrototypeCopyBenchComponent03A(),
            new PrototypeCopyBenchComponent03B(),
            new PrototypeCopyBenchComponent03C(),
            new PrototypeCopyBenchComponent03D(),
            new PrototypeCopyBenchComponent03E(),
            new PrototypeCopyBenchComponent04A(),
            new PrototypeCopyBenchComponent04B(),
            new PrototypeCopyBenchComponent04C(),
            new PrototypeCopyBenchComponent04D(),
            new PrototypeCopyBenchComponent04E(),
            new PrototypeCopyBenchComponent05A(),
            new PrototypeCopyBenchComponent05B(),
            new PrototypeCopyBenchComponent05C(),
            new PrototypeCopyBenchComponent05D(),
            new PrototypeCopyBenchComponent05E(),
            new PrototypeCopyBenchComponent06A(),
            new PrototypeCopyBenchComponent06B(),
            new PrototypeCopyBenchComponent06C(),
            new PrototypeCopyBenchComponent06D(),
            new PrototypeCopyBenchComponent06E(),
            new PrototypeCopyBenchComponent07A(),
            new PrototypeCopyBenchComponent07B(),
            new PrototypeCopyBenchComponent07C(),
            new PrototypeCopyBenchComponent07D(),
            new PrototypeCopyBenchComponent07E(),
            new PrototypeCopyBenchComponent08A(),
            new PrototypeCopyBenchComponent08B(),
            new PrototypeCopyBenchComponent08C(),
            new PrototypeCopyBenchComponent08D(),
            new PrototypeCopyBenchComponent08E(),
            new PrototypeCopyBenchComponent09A(),
            new PrototypeCopyBenchComponent09B(),
            new PrototypeCopyBenchComponent09C(),
            new PrototypeCopyBenchComponent09D(),
            new PrototypeCopyBenchComponent09E(),
            new PrototypeCopyBenchComponent10A(),
            new PrototypeCopyBenchComponent10B(),
            new PrototypeCopyBenchComponent10C(),
            new PrototypeCopyBenchComponent10D(),
            new PrototypeCopyBenchComponent10E(),
        ];

        _targets = new IComponent[_sources.Length];
        for (var i = 0; i < _sources.Length; i++)
        {
            _targets[i] = (IComponent) Activator.CreateInstance(_sources[i].GetType())!;
        }
    }

    [Benchmark(Baseline = true)]
    public void SerializationManagerCopyTo()
    {
        for (var n = 0; n < N; n++)
        {
            for (var i = 0; i < _sources.Length; i++)
            {
                var target = _targets[i];
                SerializationManager.CopyTo(_sources[i], ref target, notNullableOverride: true);
                _targets[i] = target;
            }
        }
    }

    [Benchmark]
    public void GeneratedPrototypeCopy()
    {
        for (var n = 0; n < N; n++)
        {
            for (var i = 0; i < _sources.Length; i++)
            {
                var target = _targets[i];
                EntityPrototype.CopyComponentFromPrototype(_sources[i], ref target, SerializationManager);
                _targets[i] = target;
            }
        }
    }

    public abstract partial class PrototypeCopyBenchComponent01 : Component
    {
        [DataField] public int Field01 = 1;
    }

    public abstract partial class PrototypeCopyBenchComponent02 : Component
    {
        [DataField] public int Field01 = 1;
        [DataField] public string Field02 = "two";
    }

    public abstract partial class PrototypeCopyBenchComponent03 : Component
    {
        [DataField] public int Field01 = 1;
        [DataField] public string Field02 = "two";
        [DataField] public float Field03 = 3;
    }

    public abstract partial class PrototypeCopyBenchComponent04 : Component
    {
        [DataField] public int Field01 = 1;
        [DataField] public string Field02 = "two";
        [DataField] public float Field03 = 3;
        [DataField] public bool Field04 = true;
    }

    public abstract partial class PrototypeCopyBenchComponent05 : Component
    {
        [DataField] public int Field01 = 1;
        [DataField] public string Field02 = "two";
        [DataField] public float Field03 = 3;
        [DataField] public bool Field04 = true;
        [DataField] public long Field05 = 5;
    }

    public abstract partial class PrototypeCopyBenchComponent06 : Component
    {
        [DataField] public int Field01 = 1;
        [DataField] public string Field02 = "two";
        [DataField] public float Field03 = 3;
        [DataField] public bool Field04 = true;
        [DataField] public long Field05 = 5;
        [DataField] public double Field06 = 6;
    }

    public abstract partial class PrototypeCopyBenchComponent07 : Component
    {
        [DataField] public int Field01 = 1;
        [DataField] public string Field02 = "two";
        [DataField] public float Field03 = 3;
        [DataField] public bool Field04 = true;
        [DataField] public long Field05 = 5;
        [DataField] public double Field06 = 6;
        [DataField] public PrototypeCopyBenchNested Field07 = new();
    }

    public abstract partial class PrototypeCopyBenchComponent08 : Component
    {
        [DataField] public int Field01 = 1;
        [DataField] public string Field02 = "two";
        [DataField] public float Field03 = 3;
        [DataField] public bool Field04 = true;
        [DataField] public long Field05 = 5;
        [DataField] public double Field06 = 6;
        [DataField] public PrototypeCopyBenchNested Field07 = new();
        [DataField] public int[] Field08 = [1, 2, 3, 4];
    }

    public abstract partial class PrototypeCopyBenchComponent09 : Component
    {
        [DataField] public int Field01 = 1;
        [DataField] public string Field02 = "two";
        [DataField] public float Field03 = 3;
        [DataField] public bool Field04 = true;
        [DataField] public long Field05 = 5;
        [DataField] public double Field06 = 6;
        [DataField] public PrototypeCopyBenchNested Field07 = new();
        [DataField] public int[] Field08 = [1, 2, 3, 4];
        [DataField] public List<string> Field09 = ["a", "b", "c"];
    }

    public abstract partial class PrototypeCopyBenchComponent10 : Component
    {
        [DataField] public int Field01 = 1;
        [DataField] public string Field02 = "two";
        [DataField] public float Field03 = 3;
        [DataField] public bool Field04 = true;
        [DataField] public long Field05 = 5;
        [DataField] public double Field06 = 6;
        [DataField] public PrototypeCopyBenchNested Field07 = new();
        [DataField] public int[] Field08 = [1, 2, 3, 4];
        [DataField] public List<string> Field09 = ["a", "b", "c"];
        [DataField] public Dictionary<string, int> Field10 = new() { { "a", 1 }, { "b", 2 } };
    }

    [DataDefinition]
    public sealed partial class PrototypeCopyBenchNested
    {
        [DataField] public int A = 1;
        [DataField] public string B = "nested";
    }

    public sealed partial class PrototypeCopyBenchComponent01A : PrototypeCopyBenchComponent01 {}
    public sealed partial class PrototypeCopyBenchComponent01B : PrototypeCopyBenchComponent01 {}
    public sealed partial class PrototypeCopyBenchComponent01C : PrototypeCopyBenchComponent01 {}
    public sealed partial class PrototypeCopyBenchComponent01D : PrototypeCopyBenchComponent01 {}
    public sealed partial class PrototypeCopyBenchComponent01E : PrototypeCopyBenchComponent01 {}
    public sealed partial class PrototypeCopyBenchComponent02A : PrototypeCopyBenchComponent02 {}
    public sealed partial class PrototypeCopyBenchComponent02B : PrototypeCopyBenchComponent02 {}
    public sealed partial class PrototypeCopyBenchComponent02C : PrototypeCopyBenchComponent02 {}
    public sealed partial class PrototypeCopyBenchComponent02D : PrototypeCopyBenchComponent02 {}
    public sealed partial class PrototypeCopyBenchComponent02E : PrototypeCopyBenchComponent02 {}
    public sealed partial class PrototypeCopyBenchComponent03A : PrototypeCopyBenchComponent03 {}
    public sealed partial class PrototypeCopyBenchComponent03B : PrototypeCopyBenchComponent03 {}
    public sealed partial class PrototypeCopyBenchComponent03C : PrototypeCopyBenchComponent03 {}
    public sealed partial class PrototypeCopyBenchComponent03D : PrototypeCopyBenchComponent03 {}
    public sealed partial class PrototypeCopyBenchComponent03E : PrototypeCopyBenchComponent03 {}
    public sealed partial class PrototypeCopyBenchComponent04A : PrototypeCopyBenchComponent04 {}
    public sealed partial class PrototypeCopyBenchComponent04B : PrototypeCopyBenchComponent04 {}
    public sealed partial class PrototypeCopyBenchComponent04C : PrototypeCopyBenchComponent04 {}
    public sealed partial class PrototypeCopyBenchComponent04D : PrototypeCopyBenchComponent04 {}
    public sealed partial class PrototypeCopyBenchComponent04E : PrototypeCopyBenchComponent04 {}
    public sealed partial class PrototypeCopyBenchComponent05A : PrototypeCopyBenchComponent05 {}
    public sealed partial class PrototypeCopyBenchComponent05B : PrototypeCopyBenchComponent05 {}
    public sealed partial class PrototypeCopyBenchComponent05C : PrototypeCopyBenchComponent05 {}
    public sealed partial class PrototypeCopyBenchComponent05D : PrototypeCopyBenchComponent05 {}
    public sealed partial class PrototypeCopyBenchComponent05E : PrototypeCopyBenchComponent05 {}
    public sealed partial class PrototypeCopyBenchComponent06A : PrototypeCopyBenchComponent06 {}
    public sealed partial class PrototypeCopyBenchComponent06B : PrototypeCopyBenchComponent06 {}
    public sealed partial class PrototypeCopyBenchComponent06C : PrototypeCopyBenchComponent06 {}
    public sealed partial class PrototypeCopyBenchComponent06D : PrototypeCopyBenchComponent06 {}
    public sealed partial class PrototypeCopyBenchComponent06E : PrototypeCopyBenchComponent06 {}
    public sealed partial class PrototypeCopyBenchComponent07A : PrototypeCopyBenchComponent07 {}
    public sealed partial class PrototypeCopyBenchComponent07B : PrototypeCopyBenchComponent07 {}
    public sealed partial class PrototypeCopyBenchComponent07C : PrototypeCopyBenchComponent07 {}
    public sealed partial class PrototypeCopyBenchComponent07D : PrototypeCopyBenchComponent07 {}
    public sealed partial class PrototypeCopyBenchComponent07E : PrototypeCopyBenchComponent07 {}
    public sealed partial class PrototypeCopyBenchComponent08A : PrototypeCopyBenchComponent08 {}
    public sealed partial class PrototypeCopyBenchComponent08B : PrototypeCopyBenchComponent08 {}
    public sealed partial class PrototypeCopyBenchComponent08C : PrototypeCopyBenchComponent08 {}
    public sealed partial class PrototypeCopyBenchComponent08D : PrototypeCopyBenchComponent08 {}
    public sealed partial class PrototypeCopyBenchComponent08E : PrototypeCopyBenchComponent08 {}
    public sealed partial class PrototypeCopyBenchComponent09A : PrototypeCopyBenchComponent09 {}
    public sealed partial class PrototypeCopyBenchComponent09B : PrototypeCopyBenchComponent09 {}
    public sealed partial class PrototypeCopyBenchComponent09C : PrototypeCopyBenchComponent09 {}
    public sealed partial class PrototypeCopyBenchComponent09D : PrototypeCopyBenchComponent09 {}
    public sealed partial class PrototypeCopyBenchComponent09E : PrototypeCopyBenchComponent09 {}
    public sealed partial class PrototypeCopyBenchComponent10A : PrototypeCopyBenchComponent10 {}
    public sealed partial class PrototypeCopyBenchComponent10B : PrototypeCopyBenchComponent10 {}
    public sealed partial class PrototypeCopyBenchComponent10C : PrototypeCopyBenchComponent10 {}
    public sealed partial class PrototypeCopyBenchComponent10D : PrototypeCopyBenchComponent10 {}
    public sealed partial class PrototypeCopyBenchComponent10E : PrototypeCopyBenchComponent10 {}
}
