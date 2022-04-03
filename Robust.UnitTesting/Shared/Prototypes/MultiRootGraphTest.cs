using System.Linq;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Robust.UnitTesting.Shared.Prototypes;

[TestFixture]
public sealed class MultiRootGraphTest
{
    private const string RootId1 = "root1";
    private const string RootId2 = "root2";
    private const string ChildId1 = "child1";
    private const string ChildId2 = "child2";

    [Test]
    public void AddAndRemoveRoot()
    {
        var graph = new MultiRootInheritanceGraph<string>();
        graph.Add(RootId1);
        Assert.That(graph.RootNodes.Count, Is.EqualTo(1));
        Assert.That(graph.RootNodes.Contains(RootId1));
    }

    [Test]
    public void AddAndRemoveRootAndChild()
    {
        var graph = new MultiRootInheritanceGraph<string>();
        graph.Add(ChildId1, new []{RootId1});

        var children = graph.GetChildren(RootId1);
        Assert.That(children, Is.Not.Null);
        Assert.That(children!.Count, Is.EqualTo(1));
        Assert.That(children.Contains(ChildId1));

        var parents = graph.GetParents(ChildId1);
        Assert.That(parents, Is.Not.Null);
        Assert.That(parents!.Count, Is.EqualTo(1));
        Assert.That(parents.Contains(RootId1));
    }

    [Test]
    public void AddTwoParentsRemoveOne()
    {
        var graph = new MultiRootInheritanceGraph<string>();
        graph.Add(ChildId1, new []{RootId1, RootId2});

        var parents = graph.GetParents(ChildId1);
        Assert.That(parents, Is.Not.Null);
        Assert.That(parents!.Count, Is.EqualTo(2));
        Assert.That(parents.Contains(RootId1));
        Assert.That(parents.Contains(RootId2));

        var children = graph.GetChildren(RootId1);
        Assert.That(children, Is.Not.Null);
        Assert.That(children!.Count, Is.EqualTo(1));
        Assert.That(children.Contains(ChildId1));

        children = graph.GetChildren(RootId2);
        Assert.That(children, Is.Not.Null);
        Assert.That(children!.Count, Is.EqualTo(1));
        Assert.That(children.Contains(ChildId1));

        Assert.That(graph.RootNodes.Count, Is.EqualTo(2));
        Assert.That(graph.RootNodes.Contains(RootId1));
        Assert.That(graph.RootNodes.Contains(RootId2));
    }

    [Test]
    public void OneParentTwoChildrenRemoveParent()
    {
        var graph = new MultiRootInheritanceGraph<string>();
        graph.Add(ChildId1, new []{RootId1});
        graph.Add(ChildId2, new []{RootId1});

        var parents = graph.GetParents(ChildId1);
        Assert.That(parents, Is.Not.Null);
        Assert.That(parents!.Count, Is.EqualTo(1));
        Assert.That(parents.Contains(RootId1));

        parents = graph.GetParents(ChildId2);
        Assert.That(parents, Is.Not.Null);
        Assert.That(parents!.Count, Is.EqualTo(1));
        Assert.That(parents.Contains(RootId1));

        var children = graph.GetChildren(RootId1);
        Assert.That(children, Is.Not.Null);
        Assert.That(children!.Count, Is.EqualTo(2));
        Assert.That(children.Contains(ChildId1));
        Assert.That(children.Contains(ChildId2));

        Assert.That(graph.RootNodes.Count, Is.EqualTo(1));
        Assert.That(graph.RootNodes.Contains(RootId1));
    }
}
