using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.UnitTesting.Shared.Physics
{
    [TestFixture]
    [TestOf(typeof(B2DynamicTree<>))]
    public sealed class B2DynamicTree_Test
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
        public void AddAndQuery()
        {
            var dt = new B2DynamicTree<int>();

            for (var i = 0; i < aabbs1.Length; ++i)
            {
                dt.CreateProxy(aabbs1[i], i);
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
    }
}
