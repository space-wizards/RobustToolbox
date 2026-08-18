using System.Runtime.CompilerServices;
using NUnit.Framework;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.UnitTesting;
using Robust.UnitTesting.Server;

namespace Robust.Shared.IntegrationTests.Prototypes;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.None)]
internal sealed partial class PrototypePartialTest
{
    private static readonly EntProtoId SequenceId = $"{nameof(PrototypePartialTest)}Sequence";
    private static readonly EntProtoId MappingId = $"{nameof(PrototypePartialTest)}Mapping";
    private static readonly EntProtoId MappingSequenceId = $"{nameof(PrototypePartialTest)}MappingSequence";

    private static readonly string Sequence = $@"
- type: entity
  id: {SequenceId}
  components:
  - type: PrototypePartial
    list:
    - 1
    - 2
    - 3
";

    private static readonly string AddSequence = $@"
- type: entity
  id: {SequenceId}
  components:
  - type: PrototypePartial
    list:
    - 4
";

    private static readonly string RemoveSequence = $@"
- type: entity
  id: {SequenceId}
  components:
  - type: PrototypePartial
    list:
    - !Remove 2
";

    private static readonly string AddAndRemoveSequence = $@"
- type: entity
  id: {SequenceId}
  components:
  - type: PrototypePartial
    list:
    - 4
    - !Remove 2
";

    private static readonly string Mapping = $"""
        - type: entity
          id: {MappingId}
          components:
          - type: PrototypePartial
            dictionary:
              "a": 1
              "b": 2
              "c": 3
        """;

    private static readonly string AddMapping = $"""
        - type: entity
          id: {MappingId}
          components:
          - type: PrototypePartial
            dictionary:
              "d": 4
        """;

    private static readonly string RemoveMapping = $"""
        - type: entity
          id: {MappingId}
          components:
          - type: PrototypePartial
            dictionary:
              "a": !Remove
        """;

    private static readonly string RemoveAllMapping = $"""
        - type: entity
          id: {MappingId}
          components:
          - type: PrototypePartial
            dictionary: !Remove
        """;

    private static readonly string AddAndRemoveMapping = $"""
        - type: entity
          id: {MappingId}
          components:
          - type: PrototypePartial
            dictionary:
              "a": !Remove
              "d": 4
        """;

    private static readonly string MappingSequence = $"""
        - type: entity
          id: {MappingSequenceId}
          components:
          - type: PrototypePartial
            dictionaryList:
            - "a": 1
              "b": 2
              "c": 3
            - "d": 4
              "e": 5
              "f": 6
        """;

    private static readonly string AddMappingSequence = $"""
        - type: entity
          id: {MappingSequenceId}
          components:
          - type: PrototypePartial
            dictionaryList:
            - "g": 7
            - "h": 8
        """;

    private static readonly string RemoveMappingSequence = $"""
        - type: entity
          id: {MappingSequenceId}
          components:
          - type: PrototypePartial
            dictionaryList:
            - "b": !Remove
            - "e": !Remove
        """;

    private static readonly string RemoveAllAndAddMappingSequence = $"""
        - type: entity
          id: {MappingSequenceId}
          components:
          - type: PrototypePartial
            dictionaryList: !Remove
            - "z": 123
        """;

    private static readonly string AddAndRemoveMappingSequence = $"""
        - type: entity
          id: {MappingSequenceId}
          components:
          - type: PrototypePartial
            dictionaryList:
            - "g": 7
              "b": !Remove
            - "h": 8
              "e": !Remove
        """;

    private static readonly string PartialOnlyAddSequence = $@"
- type: !PartialOnly entity
  id: {SequenceId}
  components:
  - type: PrototypePartial
    list:
    - 4
";

    private static readonly string RemoveComponent = $@"
- type: entity
  id: {SequenceId}
  components:
  - !Remove type: PrototypePartial
";

    private static readonly string RemoveComponentTheOtherWay = $@"
- type: entity
  id: {SequenceId}
  components:
  - type: !Remove PrototypePartial
";

    private static readonly string LoadOrderSequenceClear = $@"
- type: entity
  id: {SequenceId}
  components:
  - type: PrototypePartial
    list: !Remove
";

    private static readonly string LoadOrderSequenceAdd = $@"
- type: entity
  id: {SequenceId}
  components:
  - type: PrototypePartial
    list:
    - 10
";

    private ISimulation StartSim(
        bool partial = true,
        bool addBase = true,
        DiContainerDelegate? dependencies = null,
        ChangeCVarDelegate? changeCVar = null,
        Action<MemoryContentRoot>? addFiles = null,
        Action<IPrototypeManager>? loadOrder = null,
        params string[] ymlToLoad)
    {
        return RobustServerSimulation.NewSimulation()
            .ChangeCVar(factory => changeCVar?.Invoke(factory))
            .RegisterDependencies(factory => dependencies?.Invoke(factory))
            .RegisterComponents(factory => { factory.RegisterClass<PrototypePartialComponent>(); })
            .AddRoot(factory =>
            {
                var root = new MemoryContentRoot();
                if (addBase)
                {
                    root.AddOrUpdateFile(new ResPath($"/Base/{nameof(Sequence)}.yml"), Sequence);
                    root.AddOrUpdateFile(new ResPath($"/Base/{nameof(Mapping)}.yml"), Mapping);
                    root.AddOrUpdateFile(new ResPath($"/Base/{nameof(MappingSequence)}.yml"), MappingSequence);
                }

                for (var i = 0; i < ymlToLoad.Length; i++)
                {
                    var yml = ymlToLoad[i];
                    root.AddOrUpdateFile(new ResPath($"/Partials/{i}.yml"), yml);
                }

                addFiles?.Invoke(root);
                factory.AddRoot(new ResPath("/"), root);
            })
            .RegisterPrototypes(factory =>
            {
                if (loadOrder != null)
                {
                    loadOrder.Invoke(factory);
                }
                else
                {
                    if (partial)
                    {
                        factory.PartialDirectory(new ResPath("/Partials/"));
                    }
                }

                factory.LoadDirectory(new ResPath("/"));
            })
            .InitializeInstance();
    }

    [Test]
    public void NoPartialLogsDuplicateError()
    {
        var sim = StartSim(false,
            dependencies: factory => factory.Register<ILogManager, SpyLogManager>(true),
            changeCVar: factory => factory.SetCVar(RTCVars.FailureLogLevel, LogLevel.Fatal),
            ymlToLoad: AddSequence
        );

        Assert.That(
            ((SpyLogManager)sim.Resolve<ILogManager>()).CountError,
            Is.GreaterThanOrEqualTo(1)
        );
    }

    [Test]
    public void PartialDoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
            {
                StartSim(ymlToLoad: AddSequence);
            }
        );
    }

    [Test]
    public void AddSequenceTest()
    {
        var sim = StartSim(ymlToLoad: AddSequence);

        var comps = sim.Resolve<IComponentFactory>();
        var prototypes = sim.Resolve<IPrototypeManager>();
        var ent = prototypes.Index(SequenceId);
        Assert.That(ent.TryComp(out PrototypePartialComponent? partial, comps), Is.True);
        Assert.That(partial, Is.Not.Null);
        Assert.That(partial.List, Has.Count.EqualTo(4));
        Assert.That(partial.List, Is.EquivalentTo([1, 2, 3, 4]));
        Assert.That(partial.Dictionary, Is.Empty);
    }

    [Test]
    public void RemoveSequenceTest()
    {
        var sim = StartSim(ymlToLoad: RemoveSequence);

        var comps = sim.Resolve<IComponentFactory>();
        var prototypes = sim.Resolve<IPrototypeManager>();
        var ent = prototypes.Index(SequenceId);
        Assert.That(ent.TryComp(out PrototypePartialComponent? partial, comps), Is.True);
        Assert.That(partial, Is.Not.Null);
        Assert.That(partial.List, Has.Count.EqualTo(2));
        Assert.That(partial.List, Is.EquivalentTo([1, 3]));
        Assert.That(partial.Dictionary, Is.Empty);
    }

    [Test]
    public void AddAndRemoveSequenceTest()
    {
        var sim = StartSim(ymlToLoad: AddAndRemoveSequence);

        var comps = sim.Resolve<IComponentFactory>();
        var prototypes = sim.Resolve<IPrototypeManager>();
        var ent = prototypes.Index(SequenceId);
        Assert.That(ent.TryComp(out PrototypePartialComponent? partial, comps), Is.True);
        Assert.That(partial, Is.Not.Null);
        Assert.That(partial.List, Has.Count.EqualTo(3));
        Assert.That(partial.List, Is.EquivalentTo([1, 3, 4]));
        Assert.That(partial.Dictionary, Is.Empty);
    }

    [Test]
    public void AddMappingTest()
    {
        var sim = StartSim(ymlToLoad: AddMapping);

        var comps = sim.Resolve<IComponentFactory>();
        var prototypes = sim.Resolve<IPrototypeManager>();
        var ent = prototypes.Index(MappingId);
        Assert.That(ent.TryComp(out PrototypePartialComponent? partial, comps), Is.True);
        Assert.That(partial, Is.Not.Null);
        Assert.That(partial.Dictionary, Has.Count.EqualTo(4));
        Assert.That(partial.Dictionary["a"], Is.EqualTo(1));
        Assert.That(partial.Dictionary["b"], Is.EqualTo(2));
        Assert.That(partial.Dictionary["c"], Is.EqualTo(3));
        Assert.That(partial.Dictionary["d"], Is.EqualTo(4));
        Assert.That(partial.List, Is.Empty);
    }

    [Test]
    public void RemoveMappingTest()
    {
        var sim = StartSim(ymlToLoad: RemoveMapping);

        var comps = sim.Resolve<IComponentFactory>();
        var prototypes = sim.Resolve<IPrototypeManager>();
        var ent = prototypes.Index(MappingId);
        Assert.That(ent.TryComp(out PrototypePartialComponent? partial, comps), Is.True);
        Assert.That(partial, Is.Not.Null);
        Assert.That(partial.Dictionary, Has.Count.EqualTo(2));
        Assert.That(partial.Dictionary, Does.Not.ContainKey("a"));
        Assert.That(partial.Dictionary, Does.Not.ContainValue(1));
        Assert.That(partial.Dictionary["b"], Is.EqualTo(2));
        Assert.That(partial.Dictionary["c"], Is.EqualTo(3));
        Assert.That(partial.List, Is.Empty);
    }

    [Test]
    public void AddAndRemoveMappingTest()
    {
        var sim = StartSim(ymlToLoad: AddAndRemoveMapping);

        var comps = sim.Resolve<IComponentFactory>();
        var prototypes = sim.Resolve<IPrototypeManager>();
        var ent = prototypes.Index(MappingId);
        Assert.That(ent.TryComp(out PrototypePartialComponent? partial, comps), Is.True);
        Assert.That(partial, Is.Not.Null);
        Assert.That(partial.Dictionary, Has.Count.EqualTo(3));
        Assert.That(partial.Dictionary, Does.Not.ContainKey("a"));
        Assert.That(partial.Dictionary, Does.Not.ContainValue(1));
        Assert.That(partial.Dictionary["b"], Is.EqualTo(2));
        Assert.That(partial.Dictionary["c"], Is.EqualTo(3));
        Assert.That(partial.Dictionary["d"], Is.EqualTo(4));
        Assert.That(partial.List, Is.Empty);
    }

    [Test]
    public void AddMappingSequenceTest()
    {
        var sim = StartSim(ymlToLoad: AddMappingSequence);

        var comps = sim.Resolve<IComponentFactory>();
        var prototypes = sim.Resolve<IPrototypeManager>();
        var ent = prototypes.Index(MappingSequenceId);
        Assert.That(ent.TryComp(out PrototypePartialComponent? partial, comps), Is.True);
        Assert.That(partial, Is.Not.Null);
        Assert.That(partial.DictionaryList, Has.Count.EqualTo(2));

        var first = partial.DictionaryList[0];
        Assert.That(first, Has.Count.EqualTo(4));
        Assert.That(first["a"], Is.EqualTo(1));
        Assert.That(first["b"], Is.EqualTo(2));
        Assert.That(first["c"], Is.EqualTo(3));

        var second = partial.DictionaryList[1];
        Assert.That(second, Has.Count.EqualTo(4));
        Assert.That(second["d"], Is.EqualTo(4));
        Assert.That(second["e"], Is.EqualTo(5));
        Assert.That(second["f"], Is.EqualTo(6));

        Assert.That(first["g"], Is.EqualTo(7));
        Assert.That(second["h"], Is.EqualTo(8));
    }

    [Test]
    public void RemoveMappingSequenceTest()
    {
        var sim = StartSim(ymlToLoad: RemoveMappingSequence);

        var comps = sim.Resolve<IComponentFactory>();
        var prototypes = sim.Resolve<IPrototypeManager>();
        var ent = prototypes.Index(MappingSequenceId);
        Assert.That(ent.TryComp(out PrototypePartialComponent? partial, comps), Is.True);
        Assert.That(partial, Is.Not.Null);
        Assert.That(partial.DictionaryList, Has.Count.EqualTo(2));

        var first = partial.DictionaryList[0];
        Assert.That(first, Has.Count.EqualTo(2));
        Assert.That(first["a"], Is.EqualTo(1));
        Assert.That(first["c"], Is.EqualTo(3));

        var second = partial.DictionaryList[1];
        Assert.That(second, Has.Count.EqualTo(2));
        Assert.That(second["d"], Is.EqualTo(4));
        Assert.That(second["f"], Is.EqualTo(6));

        Assert.That(first, Does.Not.ContainKey("b"));
        Assert.That(first, Does.Not.ContainValue(2));

        Assert.That(first, Does.Not.ContainKey("e"));
        Assert.That(first, Does.Not.ContainValue(5));
    }

    [Test]
    public void RemoveAllAndAddMappingSequenceTest()
    {
        var sim = StartSim(ymlToLoad: RemoveAllAndAddMappingSequence);

        var comps = sim.Resolve<IComponentFactory>();
        var prototypes = sim.Resolve<IPrototypeManager>();
        var ent = prototypes.Index(MappingSequenceId);
        Assert.That(ent.TryComp(out PrototypePartialComponent? partial, comps), Is.True);
        Assert.That(partial, Is.Not.Null);
        Assert.That(partial.DictionaryList, Has.Count.EqualTo(1));

        var first = partial.DictionaryList[0];
        Assert.That(first, Has.Count.EqualTo(1));
        Assert.That(first["z"], Is.EqualTo(123));
    }

    [Test]
    public void AddAndRemoveMappingSequenceTest()
    {
        var sim = StartSim(ymlToLoad: AddAndRemoveMappingSequence);

        var comps = sim.Resolve<IComponentFactory>();
        var prototypes = sim.Resolve<IPrototypeManager>();
        var ent = prototypes.Index(MappingSequenceId);
        Assert.That(ent.TryComp(out PrototypePartialComponent? partial, comps), Is.True);
        Assert.That(partial, Is.Not.Null);
        Assert.That(partial.DictionaryList, Has.Count.EqualTo(2));

        var first = partial.DictionaryList[0];
        Assert.That(first, Has.Count.EqualTo(3));
        Assert.That(first["a"], Is.EqualTo(1));
        Assert.That(first["c"], Is.EqualTo(3));

        var second = partial.DictionaryList[1];
        Assert.That(second, Has.Count.EqualTo(3));
        Assert.That(second["d"], Is.EqualTo(4));
        Assert.That(second["f"], Is.EqualTo(6));

        Assert.That(first["g"], Is.EqualTo(7));
        Assert.That(second["h"], Is.EqualTo(8));

        Assert.That(first, Does.Not.ContainKey("b"));
        Assert.That(first, Does.Not.ContainValue(2));

        Assert.That(second, Does.Not.ContainKey("e"));
        Assert.That(second, Does.Not.ContainValue(5));
    }

    [Test]
    public void PartialOnlyExistingAddSequenceTest()
    {
        var sim = StartSim(ymlToLoad: PartialOnlyAddSequence);

        var comps = sim.Resolve<IComponentFactory>();
        var prototypes = sim.Resolve<IPrototypeManager>();
        var ent = prototypes.Index(SequenceId);
        Assert.That(ent.TryComp(out PrototypePartialComponent? partial, comps), Is.True);
        Assert.That(partial, Is.Not.Null);
        Assert.That(partial.List, Has.Count.EqualTo(4));
        Assert.That(partial.List, Is.EquivalentTo([1, 2, 3, 4]));
        Assert.That(partial.Dictionary, Is.Empty);
    }

    [Test]
    public void PartialOnlyNoExistingNoPrototypeAddSequenceTest()
    {
        var sim = StartSim(addBase: false,
            ymlToLoad: PartialOnlyAddSequence
        );

        var prototypes = sim.Resolve<IPrototypeManager>();
        Assert.That(prototypes.HasIndex(SequenceId), Is.False);
    }

    [Test]
    public void RemoveComponentTest()
    {
        var sim = StartSim(ymlToLoad: RemoveComponent);

        var comps = sim.Resolve<IComponentFactory>();
        var prototypes = sim.Resolve<IPrototypeManager>();
        var ent = prototypes.Index(SequenceId);
        Assert.That(ent.HasComp<PrototypePartialComponent>(comps), Is.False);
    }

    [Test]
    public void RemoveComponentTheOtherWayTest()
    {
        var sim = StartSim(ymlToLoad: RemoveComponentTheOtherWay);

        var comps = sim.Resolve<IComponentFactory>();
        var prototypes = sim.Resolve<IPrototypeManager>();
        var ent = prototypes.Index(SequenceId);
        Assert.That(ent.HasComp<PrototypePartialComponent>(comps), Is.False);
    }

    [Test]
    public void LoadOrderClearAndAdd()
    {
        var clearDirectory = new ResPath($"/Partials/{nameof(LoadOrderSequenceClear)}.yml");
        var addDirectory = new ResPath($"/Partials/{nameof(LoadOrderSequenceAdd)}.yml");
        var sim = StartSim(loadOrder: factory =>
            {
                factory.PartialDirectory(clearDirectory, addDirectory);
            },
            addFiles: factory =>
            {
                factory.AddOrUpdateFile(clearDirectory, LoadOrderSequenceClear);
                factory.AddOrUpdateFile(addDirectory, LoadOrderSequenceAdd);
            }
        );

        var comps = sim.Resolve<IComponentFactory>();
        var prototypes = sim.Resolve<IPrototypeManager>();
        var ent = prototypes.Index(SequenceId);
        Assert.That(ent.TryComp(out PrototypePartialComponent? partial, comps), Is.True);
        Assert.That(partial, Is.Not.Null);
        Assert.That(partial.List, Has.Count.EqualTo(1));
        Assert.That(partial.List, Is.EquivalentTo([10]));
        Assert.That(partial.Dictionary, Is.Empty);
    }

    [Test]
    public void LoadOrderAddAndClear()
    {
        var clearDirectory = new ResPath($"/Partials/{nameof(LoadOrderSequenceClear)}.yml");
        var addDirectory = new ResPath($"/Partials/{nameof(LoadOrderSequenceAdd)}.yml");
        var sim = StartSim(loadOrder: factory =>
            {
                factory.PartialDirectory(addDirectory, clearDirectory);
            },
            addFiles: factory =>
            {
                factory.AddOrUpdateFile(clearDirectory, LoadOrderSequenceClear);
                factory.AddOrUpdateFile(addDirectory, LoadOrderSequenceAdd);
            }
        );

        var comps = sim.Resolve<IComponentFactory>();
        var prototypes = sim.Resolve<IPrototypeManager>();
        var ent = prototypes.Index(SequenceId);
        Assert.That(ent.TryComp(out PrototypePartialComponent? partial, comps), Is.True);
        Assert.That(partial, Is.Not.Null);
        Assert.That(partial.List, Is.Empty);
        Assert.That(partial.Dictionary, Is.Empty);
    }

    internal sealed partial class PrototypePartialComponent : Component
    {
        [DataField]
        public List<int> List = new();

        [DataField]
        public Dictionary<string, int> Dictionary = new();

        [DataField]
        public List<Dictionary<string, int>> DictionaryList = new();

        [DataField]
        public Dictionary<string, List<int>> ListDictionary = new();
    }
}
