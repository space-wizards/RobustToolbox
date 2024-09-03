using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Utility;

[TestFixture]
[TestOf(typeof(Rope))]
[Parallelizable(ParallelScope.All)]
public static class TextRope_Test
{
    [Test]
    public static void TestCalcWeight()
    {
        // Just using the example from Wikipedia:
        // https://commons.wikimedia.org/wiki/File:Vector_Rope_example.svg

        BuildExample(out var tree);

        Assert.Multiple(() =>
        {
            Assert.That(tree.NodeN.Weight, Is.EqualTo(6));
            Assert.That(tree.NodeM.Weight, Is.EqualTo(1));
            Assert.That(tree.NodeK.Weight, Is.EqualTo(4));
            Assert.That(tree.NodeJ.Weight, Is.EqualTo(2));
            Assert.That(tree.NodeF.Weight, Is.EqualTo(3));
            Assert.That(tree.NodeE.Weight, Is.EqualTo(6));

            Assert.That(tree.NodeH.Weight, Is.EqualTo(1));
            Assert.That(tree.NodeG.Weight, Is.EqualTo(2));

            Assert.That(tree.NodeC.Weight, Is.EqualTo(6));
            Assert.That(tree.NodeD.Weight, Is.EqualTo(6));

            Assert.That(tree.NodeB.Weight, Is.EqualTo(9));

            Assert.That(tree.NodeA.Weight, Is.EqualTo(22));
        });
    }

    [Test]
    public static void TestCollect()
    {
        var tree = BuildExample(out _);
        var leaves = Rope.CollectLeaves(tree).Select(x => x.Text).ToArray();

        Assert.That(leaves, Is.EquivalentTo(new[]
        {
            "Hello ", "my ", "na", "me i", "s", " Simon"
        }));
    }

    [Test]
    public static void TestCollectReverse()
    {
        var tree = BuildExample(out _);
        var leaves = Rope.CollectLeavesReverse(tree).Select(x => x.Text).ToArray();

        Assert.That(leaves, Is.EquivalentTo(new[]
        {
            "Hello ", "my ", "na", "me i", "s", " Simon"
        }.Reverse()));
    }

    [Test]
    public static void TestCollapse()
    {
        var tree = BuildExample(out _);

        Assert.That(Rope.Collapse(tree), Is.EqualTo("Hello my name is Simon"));
    }

    [Test]
    public static void TestSplit()
    {
        var tree = BuildExample(out _);
        var (left, right) = Rope.Split(tree, 7);

        Assert.Multiple(() =>
        {
            Assert.That(Rope.Collapse(left), Is.EqualTo("Hello m"));
            Assert.That(Rope.Collapse(right), Is.EqualTo("y name is Simon"));
        });
    }

    [Test]
    public static void TestDelete()
    {
        var tree = BuildExample(out _);

        tree = Rope.Delete(tree, 2, 11);
        Assert.That(Rope.Collapse(tree), Is.EqualTo("He is Simon"));
    }

    [Test]
    public static void TestEnumerateRunesReverseSub()
    {
        var tree = BuildExample(out _);

        var runes = Rope.EnumerateRunesReverse(tree, 10);
        Assert.That(
            runes,
            Is.EquivalentTo("Hello my n".EnumerateRunes().Reverse()));
    }

    private static Rope.Node BuildExample(out ExampleTree tree)
    {
        tree = default;

        tree.NodeN = new Rope.Leaf(" Simon");
        tree.NodeM = new Rope.Leaf("s");
        tree.NodeK = new Rope.Leaf("me i");
        tree.NodeJ = new Rope.Leaf("na");
        tree.NodeF = new Rope.Leaf("my ");
        tree.NodeE = new Rope.Leaf("Hello ");

        tree.NodeH = new Rope.Branch(tree.NodeM, tree.NodeN);
        tree.NodeG = new Rope.Branch(tree.NodeJ, tree.NodeK);

        tree.NodeC = new Rope.Branch(tree.NodeE, tree.NodeF);
        tree.NodeD = new Rope.Branch(tree.NodeG, tree.NodeH);

        tree.NodeB = new Rope.Branch(tree.NodeC, tree.NodeD);

        tree.NodeA = new Rope.Branch(tree.NodeB, null);

        return tree.NodeA;
    }

    public struct ExampleTree
    {
        public Rope.Node NodeN;
        public Rope.Node NodeM;
        public Rope.Node NodeK;
        public Rope.Node NodeJ;
        public Rope.Node NodeF;
        public Rope.Node NodeE;
        public Rope.Node NodeH;
        public Rope.Node NodeG;
        public Rope.Node NodeC;
        public Rope.Node NodeD;
        public Rope.Node NodeB;
        public Rope.Node NodeA;
    }
}
