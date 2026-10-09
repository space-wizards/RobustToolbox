using System.Numerics;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.Shared.Tests.Physics
{
    [TestFixture]
    [TestOf(typeof(B2DynamicTree<>))]
    internal sealed class B2DynamicTree_Test
    {
        private static Box2[] aabbs1 =
        {
            ((Box2) default).Enlarged(1), //2x2 square
            ((Box2) default).Enlarged(2), //4x4 square
            new(-3, 3, -3, 3), // point off to the bottom left
            new(-3, -3, -3, -3), // point off to the top left
            new(3, 3, 3, 3), // point off to the bottom right
            new(3, -3, 3, -3), // point off to the top right
            ((Box2) default).Enlarged(1), //2x2 square
            ((Box2) default).Enlarged(2), //4x4 square
            ((Box2) default).Enlarged(1), //2x2 square
            ((Box2) default).Enlarged(2), //4x4 square
            ((Box2) default).Enlarged(1), //2x2 square
            ((Box2) default).Enlarged(2), //4x4 square
            ((Box2) default).Enlarged(1), //2x2 square
            ((Box2) default).Enlarged(2), //4x4 square
            ((Box2) default).Enlarged(3), //6x6 square
            new(-3, 3, -3, 3), // point off to the bottom left
            new(-3, -3, -3, -3), // point off to the top left
            new(3, 3, 3, 3), // point off to the bottom right
            new(3, -3, 3, -3), // point off to the top right
        };

        [Test]
        public void ClearReleasesOnlyAllocatedLeaves()
        {
            var tree = new B2DynamicTree<string>(capacity: 4);
            var expected = new HashSet<string>();

            // Do some bullshit to spam the tree.
            for (var i = 0; i < 100; i++)
            {
                var value = i.ToString();
                var id = tree.CreateProxy(Box2.UnitCentered.Translated(new Vector2(i, 0)), uint.MaxValue, value);
                if (i % 3 == 0)
                    tree.DestroyProxy(id);
                else
                    expected.Add(value);
            }

            tree.Rebuild(true);
            var capacity = tree.Capacity;
            var removed = new HashSet<string>();
            tree.Clear(ref removed, static (ref HashSet<string> values, in string value) =>
                Assert.That(values.Add(value), Is.True, "Each live leaf must be released exactly once"));
            Assert.That(removed, Is.EquivalentTo(expected));
            Assert.That(tree.NodeCount, Is.Zero);
            Assert.That(tree.ProxyCount, Is.Zero);
            Assert.That(tree.Capacity, Is.EqualTo(capacity));
            tree.Clear(ref removed, static (ref HashSet<string> values, in string value) => Assert.Fail("Already empty"));
            tree.Query(_ => { Assert.Fail("Cleared tree must not return leaves"); return false; }, Box2.UnitCentered.Enlarged(6767));

            var proxy = tree.CreateProxy(Box2.UnitCentered, uint.MaxValue, "new");
            tree.MoveProxy(proxy, Box2.UnitCentered.Enlarged(1));
            Assert.That(tree.GetUserData(proxy), Is.EqualTo("new"));
            tree.Rebuild(true);
            tree.DestroyProxy(proxy);
            Assert.That(tree.NodeCount, Is.Zero);
        }

        [Test]
        public void AddAndQuery()
        {
            var dt = new B2DynamicTree<int>();

            for (var i = 0; i < aabbs1.Length; ++i)
            {
                dt.CreateProxy(aabbs1[i], uint.MaxValue, i);
            }

            var point = new Vector2(0, 0);
            var box2 = Box2.CenteredAround(point, new Vector2(0.1f, 0.1f));

            var results = new HashSet<int>();

            dt.Query(proxy =>
            {
                results.Add(dt.GetUserData(proxy));
                return true;
            }, box2);

            Assert.Multiple(() =>
            {
                for (var i = 0; i < aabbs1.Length; i++)
                {
                    var aabb = aabbs1[i];

                    if (aabb.Intersects(box2))
                    {
                        Assert.That(results, Does.Contain(i));
                    }
                }
            });
        }

        [Test]
        public void RebuildFullPreservesQueries()
        {
            var dt = new B2DynamicTree<int>();

            for (var i = 0; i < aabbs1.Length; ++i)
            {
                dt.CreateProxy(aabbs1[i], uint.MaxValue, i);
            }

            dt.Rebuild(true);

            var point = new Vector2(0, 0);
            var box = Box2.CenteredAround(point, new Vector2(0.1f, 0.1f));
            var results = new HashSet<int>();

            dt.Query(proxy =>
            {
                results.Add(dt.GetUserData(proxy));
                return true;
            }, box);

            Assert.Multiple(() =>
            {
                for (var i = 0; i < aabbs1.Length; i++)
                {
                    if (aabbs1[i].Intersects(box))
                    {
                        Assert.That(results, Does.Contain(i));
                    }
                }
            });
        }
    }
}
