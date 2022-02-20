using System;
using System.Linq;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.UnitTesting.Shared.Physics
{

    [TestFixture]
    [TestOf(typeof(DynamicTree<>))]
    public sealed class DynamicTree_Test
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

        private static Box2[] aabbs2 =
        {
            ((Box2) default).Enlarged(3), //6x6 square
            ((Box2) default).Enlarged(1), //2x2 square
            ((Box2) default).Enlarged(2), //4x4 square
            new(-3, 3, -3, 3), // point off to the bottom left
            new(-3, -3, -3, -3), // point off to the top left
            new(3, 3, 3, 3), // point off to the bottom right
            new(3, -3, 3, -3), // point off to the top right
            new(-3, 3, -3, 3), // point off to the bottom left
            new(-3, -3, -3, -3), // point off to the top left
            new(3, 3, 3, 3), // point off to the bottom right
            new(3, -3, 3, -3), // point off to the top right
            new(-3, 3, -3, 3), // point off to the bottom left
            new(-3, -3, -3, -3), // point off to the top left
            new(3, 3, 3, 3), // point off to the bottom right
            new(3, -3, 3, -3), // point off to the top right
            ((Box2) default).Enlarged(2), //4x4 square
            ((Box2) default).Enlarged(1), //2x2 square
            ((Box2) default).Enlarged(2), //4x4 square
            ((Box2) default).Enlarged(1), //2x2 square
        };

        [Test]
        public void AddAndGrow()
        {
            var dt = new DynamicTree<int>((in int x) => aabbs1[x], capacity: 16, growthFunc: x => x += 2);

            var initCap = dt.Capacity;

            Assert.That(initCap, Is.EqualTo(16));

            Assert.Multiple(() =>
            {
                for (var i = 0; i < aabbs1.Length; ++i)
                {
                    Assert.True(dt.Add(i), $"Add {i}");
                }
            });

            Assert.That(dt.Capacity, Is.AtLeast(initCap));
        }

        [Test]
        public void AddDuplicates()
        {
            var dt = new DynamicTree<int>((in int x) => aabbs1[x], capacity: 16, growthFunc: x => x += 2);

            Assert.Multiple(() =>
            {
                for (var i = 0; i < aabbs1.Length; ++i)
                {
                    Assert.True(dt.Add(i), $"Add {i}");
                }
            });

            Assert.Multiple(() =>
            {
                for (var i = 0; i < aabbs1.Length; ++i)
                {
                    Assert.False(dt.Add(i), $"Add Dupe {i}");
                }
            });
        }

        [Test]
        public void RemoveMissingWhileEmpty()
        {
            var dt = new DynamicTree<int>((in int x) => aabbs1[x], capacity: 16, growthFunc: x => x += 2);

            Assert.Multiple(() =>
            {
                for (var i = 0; i < aabbs1.Length; ++i)
                {
                    Assert.False(dt.Remove(i), $"Remove {i}");
                }
            });
        }

        [Test]
        public void UpdateMissingWhileEmpty()
        {
            var dt = new DynamicTree<int>((in int x) => aabbs1[x], capacity: 16, growthFunc: x => x += 2);

            Assert.Multiple(() =>
            {
                for (var i = 0; i < aabbs1.Length; ++i)
                {
                    Assert.False(dt.Update(i), $"Update {i}");
                }
            });
        }

        [Test]
        public void RemoveMissingNotEmpty()
        {
            var dt = new DynamicTree<int>((in int x) => aabbs1[x], capacity: 16, growthFunc: x => x += 2);

            Assert.Multiple(() =>
            {
                for (var i = 0; i < aabbs1.Length; ++i)
                {
                    Assert.True(dt.Add(i), $"Add {i}");
                }
            });

            Assert.Multiple(() =>
            {
                for (var i = aabbs1.Length; i < aabbs1.Length + aabbs2.Length; ++i)
                {
                    Assert.False(dt.Remove(i), $"Remove {i}");
                }
            });
        }

        [Test]
        public void UpdateMissingNotEmpty()
        {
            var dt = new DynamicTree<int>((in int x) => aabbs1[x], capacity: 16, growthFunc: x => x += 2);

            Assert.Multiple(() =>
            {
                for (var i = 0; i < aabbs1.Length; ++i)
                {
                    Assert.True(dt.Add(i), $"Add {i}");
                }
            });

            Assert.Multiple(() => {
                for (var i = aabbs1.Length; i < aabbs1.Length + aabbs2.Length; ++i)
                {
                    Assert.False(dt.Update(i), $"Update {i}");
                }
            });
        }

        [Test]
        public void AddThenRemove()
        {
            var aabbs = aabbs1;
            var dt = new DynamicTree<int>((in int x) => aabbs[x], capacity: 16, growthFunc: x => x += 2);

            Assert.Multiple(() =>
            {
                for (var i = 0; i < aabbs.Length; ++i)
                {
                    Assert.True(dt.Add(i), $"Add {i}");
                }
            });
            aabbs = aabbs2;

            Assert.Multiple(() =>
            {
                for (var i = 0; i < aabbs.Length; ++i)
                {
                    Assert.True(dt.Remove(i), $"Remove {i}");
                }
            });
        }

        [Test]
        public void AddAndQuery() {
            var dt = new DynamicTree<int>((in int x) => aabbs1[x], capacity: 16, growthFunc: x => x += 2);

            Assert.Multiple(() =>
            {
                for (var i = 0; i < aabbs1.Length; ++i)
                {
                    Assert.True(dt.Add(i), $"Add {i}");
                }
            });

            var point = new Vector2(0, 0);

            var containers = Enumerable.Range(0, aabbs1.Length)
                .Where(x => aabbs1[x].Contains(point))
                .OrderBy(x => x).ToArray();

            var results = dt.QueryPoint(point)
                .OrderBy(x => x).ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(results.Length, Is.EqualTo(containers.Length), "Length");
                var l = Math.Min(containers.Length, results.Length);
                for (var i = 0; i < l; ++i)
                {
                    Assert.That(results[i], Is.EqualTo(containers[i]));
                }
            });
        }

        // TODO: other Box2, Ray, Point query method tests
    }

}
