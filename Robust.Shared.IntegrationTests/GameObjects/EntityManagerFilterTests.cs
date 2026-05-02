using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Robust.UnitTesting.Shared.GameObjects;

[TestOf(typeof(EntityManager))]
[TestOf(typeof(ComponentFilter))]
internal sealed class EntityManagerFilterTests : OurRobustUnitTest
{
    private const string TestEnt1 = "T_TestEnt1";

    private const string Prototypes = $"""
        - type: entity
          id: {TestEnt1}
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
    [TestOf(typeof(ComponentFilterQuery))]
    [Description("""
    Tests that filters work against some test targets with various permutations of components.
    Covers MatchesFilter, ExactlyMatchesFilter, and ComponentFilterQuery.
    """)]
    public void FilterEntities(
        [Values(null, typeof(Marker1Component))]
        Type? m1,
        [Values(null, typeof(Marker2Component))]
        Type? m2,
        [Values(null, typeof(Marker3Component))]
        Type? m3,
        [Values(null, typeof(Marker4Component))]
        Type? m4
        )
    {
        EntityUid? target = null;
        EntityUid? target2 = null;
        try
        {
            var filter = new ComponentFilter(new [] {m1, m2, m3, m4}.Where(x => x is not null).Cast<Type>());

            target = _entMan.Spawn(TestEnt1);

            Assert.That(_entMan.MatchesFilter(target.Value, filter));

            // If there's four entries, then it should be an exact match.
            if (filter.Count == 4)
                Assert.That(_entMan.ExactlyMatchesFilter(target.Value, filter));
            else
                Assert.That(_entMan.ExactlyMatchesFilter(target.Value, filter), NUnit.Framework.Is.False);

            var query = _entMan.ComponentFilterQuery(filter);
            Assert.That(query.Count(), NUnit.Framework.Is.EqualTo(1));

            target2 = _entMan.Spawn(TestEnt1);

            Assert.That(query.Count(), NUnit.Framework.Is.EqualTo(2));
        }
        finally
        {
            _entMan.DeleteEntity(target);
            _entMan.DeleteEntity(target2);
        }
    }

    [Test]
    [Description("Asserts that using FillMissesWithNewComponents should make the entity match the filter afterward.")]
    public void FillMisses(
        [Values(null, typeof(Marker1Component))]
        Type? m1,
        [Values(null, typeof(Marker2Component))]
        Type? m2,
        [Values(null, typeof(Marker3Component))]
        Type? m3,
        [Values(null, typeof(Marker4Component))]
        Type? m4
    )
    {
        var target = _entMan.Spawn();
        try
        {
            var filter = new ComponentFilter(new [] {m1, m2, m3, m4}.Where(x => x is not null).Cast<Type>());

            _entMan.FillMissesWithNewComponents(target, filter);

            Assert.That(_entMan.MatchesFilter(target, filter));
        }
        finally
        {
            _entMan.DeleteEntity(target);
        }
    }

    [Test]
    [Description("""
    Tests the various component-set operations, on a full (all markers) and empty (no markers) entity.
    Ensures an edge case in EnumerateEntityMisses (xform and metadata existing) is present.
    """)]
    public void EnumerateFilterHits(
        [Values(null, typeof(Marker1Component))]
        Type? m1,
        [Values(null, typeof(Marker2Component))]
        Type? m2,
        [Values(null, typeof(Marker3Component))]
        Type? m3,
        [Values(null, typeof(Marker4Component))]
        Type? m4
    )
    {
        var target = _entMan.Spawn(TestEnt1);
        var target2 = _entMan.Spawn();
        try
        {
            var filter = new ComponentFilter(new [] {m1, m2, m3, m4}.Where(x => x is not null).Cast<Type>());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(_entMan.EnumerateFilterHits(target, filter),
                    NUnit.Framework.Is.EquivalentTo(filter),
                    "Expected the test entity with all markers to match the filter and the resulting set intersection to match as well.");
                Assert.That(_entMan.EnumerateFilterMisses(target, filter),
                    NUnit.Framework.Is.Empty,
                    "Expected there to be no misses on an entity with every marker that can be in the filter.");
                Assert.That(_entMan.EnumerateFilterMisses(target2, filter),
                    NUnit.Framework.Is.EquivalentTo(filter),
                    "Expected every item in the filter to miss on an entity with no markers.");
                Assert.That(_entMan.EnumerateEntityMisses(target2, filter),
                    NUnit.Framework.Is.EquivalentTo([typeof(MetaDataComponent), typeof(TransformComponent)]),
                    "Expected the entity to have two components the filter does not, transform and metadata.");
            }
        }
        finally
        {
            _entMan.DeleteEntity(target);
            _entMan.DeleteEntity(target2);
        }
    }

}

internal sealed partial class Marker1Component : Component
{

}

internal sealed partial class Marker2Component : Component
{

}

internal sealed partial class Marker3Component : Component
{

}

internal sealed partial class Marker4Component : Component
{

}
