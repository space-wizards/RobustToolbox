using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using JetBrains.Annotations;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.UnitTesting;

namespace Robust.Benchmarks.EntityManager;

[Virtual, MemoryDiagnoser]
public partial class PrototypeSpawnBenchmark : RobustIntegrationTest
{
    private const string PrototypeId = "PrototypeSpawnBenchmark";

    private static readonly Type[][] ComponentTypes =
    [
        [
            typeof(PrototypeSpawnBench01Component),
            typeof(PrototypeSpawnBench02Component),
            typeof(PrototypeSpawnBench03Component),
            typeof(PrototypeSpawnBench04Component),
            typeof(PrototypeSpawnBench05Component),
        ],
        [
            typeof(PrototypeSpawnBench06Component),
            typeof(PrototypeSpawnBench07Component),
            typeof(PrototypeSpawnBench08Component),
            typeof(PrototypeSpawnBench09Component),
            typeof(PrototypeSpawnBench10Component),
        ],
        [
            typeof(PrototypeSpawnBench11Component),
            typeof(PrototypeSpawnBench12Component),
            typeof(PrototypeSpawnBench13Component),
            typeof(PrototypeSpawnBench14Component),
            typeof(PrototypeSpawnBench15Component),
        ],
    ];

    private ServerIntegrationInstance _server = default!;
    private IEntityManager _entityManager = default!;
    private MapCoordinates _mapCoords;

    [UsedImplicitly]
    [Params(1, 5)]
    public int ComponentCount;

    [UsedImplicitly]
    [Params(1, 5, 10)]
    public int FieldCount;

    [UsedImplicitly]
    [Params(1, 10, 100)]
    public int SpawnCount;

    [GlobalSetup]
    public void GlobalSetup()
    {
        ProgramShared.PathOffset = "";
        _server = StartServer(new()
        {
            Pool = false,
            ExtraPrototypes = GetPrototype(),
            ContentAssemblies = [typeof(PrototypeSpawnBenchmark).Assembly],
            BeforeRegisterComponents = RegisterComponents,
        });

        _server.WaitIdleAsync().Wait();
        _entityManager = _server.ResolveDependency<IEntityManager>();
        var mapSystem = _entityManager.System<SharedMapSystem>();
        mapSystem.CreateMap(out var mapId);
        _mapCoords = new MapCoordinates(default, mapId);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _server.Dispose();
    }

    [Benchmark]
    public void SpawnDeletePrototype()
    {
        for (var i = 0; i < SpawnCount; i++)
        {
            var uid = _entityManager.SpawnEntity(PrototypeId, _mapCoords);
            _entityManager.DeleteEntity(uid);
        }
    }

    private string GetPrototype()
    {
        var components = ComponentTypes[FieldCount switch
        {
            1 => 0,
            5 => 1,
            10 => 2,
            _ => throw new NotSupportedException()
        }];

        var builder = new StringBuilder();
        builder.AppendLine("- type: entity");
        builder.AppendLine($"  id: {PrototypeId}");
        builder.AppendLine("  components:");

        for (var i = 0; i < ComponentCount; i++)
        {
            builder.AppendLine($"  - type: {GetComponentName(components[i])}");
        }

        return builder.ToString();
    }

    private static string GetComponentName(Type type)
    {
        const string suffix = "Component";
        return type.Name[..^suffix.Length];
    }

    private static void RegisterComponents()
    {
        IoCManager.Resolve<IComponentFactory>().RegisterTypes(
            typeof(PrototypeSpawnBench01Component),
            typeof(PrototypeSpawnBench02Component),
            typeof(PrototypeSpawnBench03Component),
            typeof(PrototypeSpawnBench04Component),
            typeof(PrototypeSpawnBench05Component),
            typeof(PrototypeSpawnBench06Component),
            typeof(PrototypeSpawnBench07Component),
            typeof(PrototypeSpawnBench08Component),
            typeof(PrototypeSpawnBench09Component),
            typeof(PrototypeSpawnBench10Component),
            typeof(PrototypeSpawnBench11Component),
            typeof(PrototypeSpawnBench12Component),
            typeof(PrototypeSpawnBench13Component),
            typeof(PrototypeSpawnBench14Component),
            typeof(PrototypeSpawnBench15Component));
    }
}

[RegisterComponent] public sealed partial class PrototypeSpawnBench01Component : PrototypeSpawnBenchComponent01 {}
[RegisterComponent] public sealed partial class PrototypeSpawnBench02Component : PrototypeSpawnBenchComponent01 {}
[RegisterComponent] public sealed partial class PrototypeSpawnBench03Component : PrototypeSpawnBenchComponent01 {}
[RegisterComponent] public sealed partial class PrototypeSpawnBench04Component : PrototypeSpawnBenchComponent01 {}
[RegisterComponent] public sealed partial class PrototypeSpawnBench05Component : PrototypeSpawnBenchComponent01 {}
[RegisterComponent] public sealed partial class PrototypeSpawnBench06Component : PrototypeSpawnBenchComponent05 {}
[RegisterComponent] public sealed partial class PrototypeSpawnBench07Component : PrototypeSpawnBenchComponent05 {}
[RegisterComponent] public sealed partial class PrototypeSpawnBench08Component : PrototypeSpawnBenchComponent05 {}
[RegisterComponent] public sealed partial class PrototypeSpawnBench09Component : PrototypeSpawnBenchComponent05 {}
[RegisterComponent] public sealed partial class PrototypeSpawnBench10Component : PrototypeSpawnBenchComponent05 {}
[RegisterComponent] public sealed partial class PrototypeSpawnBench11Component : PrototypeSpawnBenchComponent10 {}
[RegisterComponent] public sealed partial class PrototypeSpawnBench12Component : PrototypeSpawnBenchComponent10 {}
[RegisterComponent] public sealed partial class PrototypeSpawnBench13Component : PrototypeSpawnBenchComponent10 {}
[RegisterComponent] public sealed partial class PrototypeSpawnBench14Component : PrototypeSpawnBenchComponent10 {}
[RegisterComponent] public sealed partial class PrototypeSpawnBench15Component : PrototypeSpawnBenchComponent10 {}

public abstract partial class PrototypeSpawnBenchComponent01 : Component
{
    [DataField] public int Field01 = 1;
}

public abstract partial class PrototypeSpawnBenchComponent05 : Component
{
    [DataField] public int Field01 = 1;
    [DataField] public string Field02 = "two";
    [DataField] public float Field03 = 3;
    [DataField] public bool Field04 = true;
    [DataField] public long Field05 = 5;
}

public abstract partial class PrototypeSpawnBenchComponent10 : Component
{
    [DataField] public int Field01 = 1;
    [DataField] public string Field02 = "two";
    [DataField] public float Field03 = 3;
    [DataField] public bool Field04 = true;
    [DataField] public long Field05 = 5;
    [DataField] public double Field06 = 6;
    [DataField] public PrototypeSpawnBenchNested Field07 = new();
    [DataField] public int[] Field08 = [1, 2, 3, 4];
    [DataField] public string[] Field09 = ["a", "b", "c"];
    [DataField] public Dictionary<string, int> Field10 = new() { { "a", 1 }, { "b", 2 } };
}

[DataDefinition]
public sealed partial class PrototypeSpawnBenchNested
{
    [DataField] public int A = 1;
    [DataField] public string B = "nested";
}
