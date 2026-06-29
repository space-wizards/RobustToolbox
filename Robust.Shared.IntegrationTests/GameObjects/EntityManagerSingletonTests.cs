using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Robust.UnitTesting.Shared.GameObjects;

[TestOf(typeof(EntityManager))]
internal sealed class EntityManagerSingletonTests : OurRobustUnitTest
{
    private const string TestSingleton1 = "T_TestSingleton1";

    private const string Prototypes = $"""
        - type: entity
          id: {TestSingleton1}
          components:
          - type: Marker1
          - type: Marker2
          - type: Marker3
          - type: Marker4
        """;

    protected override Type[]? ExtraComponents =>
    [
        typeof(Marker1Component), typeof(Marker2Component), typeof(Marker3Component), typeof(Marker4Component)
    ];

    private PrototypeManager _protoMan = default!;
    private IEntityManager _entMan = default!;

    [OneTimeSetUp]
    public void Setup()
    {
        IoCManager.Resolve<ISerializationManager>().Initialize();

        _protoMan = (PrototypeManager) IoCManager.Resolve<IPrototypeManager>();
        _protoMan.RegisterKind(typeof(EntityPrototype), typeof(EntityCategoryPrototype));
        _protoMan.LoadString(Prototypes);
        _protoMan.ResolveResults();

        _entMan = IoCManager.Resolve<IEntityManager>();
    }

    [Test]
    [Description("""
    Creates an entity with marker components 1-4, and ensures increasingly constrained Single<>() queries match it.
    Then creates a second entity, and ensures queries fail.
    Then deletes both, and ensures Single fails while TrySingle is fine.
    """)]
    public void SingleEntity()
    {
        var mySingle = _entMan.Spawn(TestSingleton1);

        var single1 = _entMan.Single<Marker1Component>();
        var single2 = _entMan.Single<Marker1Component, Marker2Component>();
        var single3 = _entMan.Single<Marker1Component, Marker2Component, Marker3Component>();
        var single4 = _entMan.Single<Marker1Component, Marker2Component, Marker3Component, Marker4Component>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mySingle, NUnit.Framework.Is.EqualTo(single1.Owner));
            Assert.That(mySingle, NUnit.Framework.Is.EqualTo(single2.Owner));
            Assert.That(mySingle, NUnit.Framework.Is.EqualTo(single3.Owner));
            Assert.That(mySingle, NUnit.Framework.Is.EqualTo(single4.Owner));
        }

        var bonusSingle = _entMan.Spawn(TestSingleton1);

        using (Assert.EnterMultipleScope())
        {
            Assert.Throws<NonUniqueSingletonException>(() => _entMan.Single<Marker1Component>());
            Assert.Throws<NonUniqueSingletonException>(() => _entMan.Single<Marker1Component, Marker2Component>());
            Assert.Throws<NonUniqueSingletonException>(() =>
                _entMan.Single<Marker1Component, Marker2Component, Marker3Component>());
            Assert.Throws<NonUniqueSingletonException>(() =>
                _entMan.Single<Marker1Component, Marker2Component, Marker3Component, Marker4Component>());
            Assert.Throws<NonUniqueSingletonException>(() => _entMan.TrySingle<Marker1Component>(out _));
            Assert.Throws<NonUniqueSingletonException>(() =>
                _entMan.TrySingle<Marker1Component, Marker2Component>(out _));
            Assert.Throws<NonUniqueSingletonException>(() =>
                _entMan.TrySingle<Marker1Component, Marker2Component, Marker3Component>(out _));
            Assert.Throws<NonUniqueSingletonException>(() =>
                _entMan.TrySingle<Marker1Component, Marker2Component, Marker3Component, Marker4Component>(out _));
        }

        _entMan.DeleteEntity(bonusSingle);
        _entMan.DeleteEntity(mySingle);

        using (Assert.EnterMultipleScope())
        {
            Assert.Throws<MatchNotFoundException>(() => _entMan.Single<Marker1Component>());
            Assert.Throws<MatchNotFoundException>(() => _entMan.Single<Marker1Component, Marker2Component>());
            Assert.Throws<MatchNotFoundException>(() =>
                _entMan.Single<Marker1Component, Marker2Component, Marker3Component>());
            Assert.Throws<MatchNotFoundException>(() =>
                _entMan.Single<Marker1Component, Marker2Component, Marker3Component, Marker4Component>());
            Assert.That(_entMan.TrySingle<Marker1Component>(out _), NUnit.Framework.Is.False);
            Assert.That(_entMan.TrySingle<Marker1Component, Marker2Component>(out _), NUnit.Framework.Is.False);
            Assert.That(_entMan.TrySingle<Marker1Component, Marker2Component, Marker3Component>(out _),
                NUnit.Framework.Is.False);
            Assert.That(
                _entMan.TrySingle<Marker1Component, Marker2Component, Marker3Component, Marker4Component>(out _),
                NUnit.Framework.Is.False);
        }
    }
}

internal sealed partial class Marker1Component : Component;
internal sealed partial class Marker2Component : Component;
internal sealed partial class Marker3Component : Component;
internal sealed partial class Marker4Component : Component;
