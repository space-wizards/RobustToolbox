using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Robust.Shared.IoC;

namespace Robust.Shared.Tests.IoC;

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

        Assert.That(a, Is.SameAs(b), () => "A & B instances must be reference equal");
    }

    [Test]
    public void TestRegisterSameImplementationAcrossBuildGraphReusesInstance()
    {
        var deps = new DependencyCollection();
        deps.Register<IA, C>();
        deps.BuildGraph();

        var a = deps.Resolve<IA>();

        deps.Register<IB, C>();
        deps.BuildGraph();

        var b = deps.Resolve<IB>();

        Assert.That(b, Is.SameAs(a), () => "A & B instances must be reference equal across BuildGraph calls");
    }

    [Test]
    public void TestResolveDependencyCollectionDefaultsToSelf()
    {
        var deps = new DependencyCollection();

        Assert.That(deps.Resolve<IDependencyCollection>(), Is.SameAs(deps));
        Assert.That(deps.ResolveType(typeof(IDependencyCollection)), Is.SameAs(deps));
    }

    [Test]
    public void TestResolveDependencyCollectionUsesRegisteredOverride()
    {
        var deps = new DependencyCollection();
        var overrideCollection = new DependencyCollection();
        deps.RegisterInstance<IDependencyCollection>(overrideCollection);
        deps.BuildGraph();

        Assert.That(deps.Resolve<IDependencyCollection>(), Is.SameAs(overrideCollection));
        Assert.That(deps.ResolveType(typeof(IDependencyCollection)), Is.SameAs(overrideCollection));
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
