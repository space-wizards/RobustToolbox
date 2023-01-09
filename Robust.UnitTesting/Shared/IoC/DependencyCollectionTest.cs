using NUnit.Framework;
using Robust.Shared.IoC;

namespace Robust.UnitTesting.Shared.IoC;

[TestFixture]
[TestOf(typeof(DependencyCollection))]
[Parallelizable]
internal sealed class DependencyCollectionTest
{
    /// <summary>
    /// Tests that registering two interfaces with the same implementation results in a single instance being shared.
    /// </summary>
    [Test]
    public void TestRegisterSameImplementation()
    {
        var deps = new DependencyCollection();
        deps.Register<IA, C>();
        deps.Register<IB, C>();

        deps.BuildGraph();

        var a = deps.Resolve<IA>();
        var b = deps.Resolve<IB>();

        Assert.That(a, Is.EqualTo(b), () => "A & B instances must be reference equal");
    }

    private interface IA
    {

    }

    private interface IB
    {

    }

    private sealed class C : IA, IB
    {

    }
}
