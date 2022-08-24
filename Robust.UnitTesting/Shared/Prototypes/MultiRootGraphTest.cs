using System;
using System.Linq;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Robust.UnitTesting.Shared.Prototypes;

[TestFixture]
public sealed class MultiRootGraphTest
{
    private const string Id1 = "id1";
    private const string Id2 = "id2";
    private const string Id3 = "id3";
    private const string Id4 = "id4";

    [Test]
    public void AddAndRemoveRoot()
    {
        var graph = new MultiRootInheritanceGraph<string>();
        graph.Add(Id1);
        Assert.That(graph.RootNodes.Count, Is.EqualTo(1));
        Assert.That(graph.RootNodes.Contains(Id1));
    }

    [Test]
    public void AddAndRemoveRootAndChild()
    {
        var graph = new MultiRootInheritanceGraph<string>();
        graph.Add(Id3, new []{Id1});

        var children = graph.GetChildren(Id1);
        Assert.That(children, Is.Not.Null);
        Assert.That(children!.Count, Is.EqualTo(1));
        Assert.That(children.Contains(Id3));

        var parents = graph.GetParents(Id3);
        Assert.That(parents, Is.Not.Null);
        Assert.That(parents!.Count, Is.EqualTo(1));
        Assert.That(parents.Contains(Id1));
    }

    [Test]
    public void AddTwoParentsRemoveOne()
    {
        var graph = new MultiRootInheritanceGraph<string>();
        graph.Add(Id3, new []{Id1, Id2});

        var parents = graph.GetParents(Id3);
        Assert.That(parents, Is.Not.Null);
        Assert.That(parents!.Count, Is.EqualTo(2));
        Assert.That(parents.Contains(Id1));
        Assert.That(parents.Contains(Id2));

        var children = graph.GetChildren(Id1);
        Assert.That(children, Is.Not.Null);
        Assert.That(children!.Count, Is.EqualTo(1));
        Assert.That(children.Contains(Id3));

        children = graph.GetChildren(Id2);
        Assert.That(children, Is.Not.Null);
        Assert.That(children!.Count, Is.EqualTo(1));
        Assert.That(children.Contains(Id3));

        Assert.That(graph.RootNodes.Count, Is.EqualTo(2));
        Assert.That(graph.RootNodes.Contains(Id1));
        Assert.That(graph.RootNodes.Contains(Id2));
    }

    [Test]
    public void OneParentTwoChildrenRemoveParent()
    {
        var graph = new MultiRootInheritanceGraph<string>();
        graph.Add(Id3, new []{Id1});
        graph.Add(Id4, new []{Id1});

        var parents = graph.GetParents(Id3);
        Assert.That(parents, Is.Not.Null);
        Assert.That(parents!.Count, Is.EqualTo(1));
        Assert.That(parents.Contains(Id1));

        parents = graph.GetParents(Id4);
        Assert.That(parents, Is.Not.Null);
        Assert.That(parents!.Count, Is.EqualTo(1));
        Assert.That(parents.Contains(Id1));

        var children = graph.GetChildren(Id1);
        Assert.That(children, Is.Not.Null);
        Assert.That(children!.Count, Is.EqualTo(2));
        Assert.That(children.Contains(Id3));
        Assert.That(children.Contains(Id4));

        Assert.That(graph.RootNodes.Count, Is.EqualTo(1));
        Assert.That(graph.RootNodes.Contains(Id1));
    }

    [Test]
    public void CircleCheckTest()
    {
        var graph = new MultiRootInheritanceGraph<string>();
        graph.Add(Id1, new []{Id2});
        Assert.Throws<InvalidOperationException>(() => graph.Add(Id2, new []{Id1}));

        graph.Add(Id2, new[] { Id3 });
        Assert.Throws<InvalidOperationException>(() => graph.Add(Id3, new[] { Id1 }));

    }
}
