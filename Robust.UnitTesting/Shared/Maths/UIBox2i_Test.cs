using System;
using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Maths
{
    [Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
    [TestFixture]
    [TestOf(typeof(UIBox2i))]
    public class UIBox2i_Test
    {
        private static IEnumerable<(int left, int top, int right, int bottom)> Sources => new (int, int, int, int)[]
        {
            (0, 0, 0, 0),
            (0, 0, 0, 10),
            (0, 0, 10, 0),
            (0, 0, 10, 10),
            (0, -10, 0, 0),
            (0, -10, 0, 10),
            (0, -10, 10, 0),
            (0, -10, 10, 10),
            (-10, 0, 0, 0),
            (-10, 0, 0, 10),
            (-10, 0, 10, 0),
            (-10, 0, 10, 10),
            (-10, -10, 0, 0),
            (-10, -10, 0, 10),
            (-10, -10, 10, 0),
            (-10, -10, 10, 10)
        };

        private static IEnumerable<(int x, int y)> SmallTranslations => new (int, int)[]
        {
            (0, 1),
            (1, 0),
            (1, 1),
            (0, -1),
            (1, -1),
            (-1, 0),
            (-1, 1),
            (-1, -1)
        };

        private static IEnumerable<(int x, int y)> LargeTranslations => new (int, int)[]
        {
            (0, 20),
            (20, 0),
            (20, 20),
            (0, -20),
            (20, -20),
            (-20, 0),
            (-20, 20),
            (-20, -20)
        };

        private static IEnumerable<(UIBox2i a, UIBox2i b, UIBox2i? expected)> Intersections =>
            new (UIBox2i, UIBox2i, UIBox2i?)[]
            {
                (new UIBox2i(0, 0, 5, 5), new UIBox2i(2, 2, 4, 4), new UIBox2i(2, 2, 4, 4)),
                (new UIBox2i(0, 0, 5, 5), new UIBox2i(3, 3, 7, 7), new UIBox2i(3, 3, 5, 5)),
                (new UIBox2i(2, 0, 5, 5), new UIBox2i(0, 3, 4, 7), new UIBox2i(2, 3, 4, 5)),
                (new UIBox2i(2, 0, 5, 5), new UIBox2i(6, 6, 10, 10), null),
            };

        [Test]
        public void Box2iVectorConstructor([ValueSource(nameof(Sources))] (int, int, int, int) test)
        {
            var (left, top, right, bottom) = test;
            var box = new UIBox2i(new Vector2i(left, top), new Vector2i(right, bottom));

            Assert.That(box.Left, Is.EqualTo(left));
            Assert.That(box.Top, Is.EqualTo(top));
            Assert.That(box.Right, Is.EqualTo(right));
            Assert.That(box.Bottom, Is.EqualTo(bottom));
        }

        [Test]
        public void Box2iEdgesConstructor([ValueSource(nameof(Sources))] (int, int, int, int) test)
        {
            var (left, top, right, bottom) = test;
            var box = new UIBox2i(left, top, right, bottom);

            Assert.That(box.Left, Is.EqualTo(left));
            Assert.That(box.Top, Is.EqualTo(top));
            Assert.That(box.Right, Is.EqualTo(right));
            Assert.That(box.Bottom, Is.EqualTo(bottom));
        }

        [Test]
        public void Box2iCornerVectorProperties([ValueSource(nameof(Sources))] (int, int, int, int) test)
        {
            var (left, top, right, bottom) = test;
            var box = new UIBox2i(left, top, right, bottom);

            var br = new Vector2i(right, bottom);
            var tl = new Vector2i(left, top);
            var tr = new Vector2i(right, top);
            var bl = new Vector2i(left, bottom);

            Assert.That(box.BottomRight, Is.EqualTo(br));
            Assert.That(box.TopLeft, Is.EqualTo(tl));
            Assert.That(box.TopRight, Is.EqualTo(tr));
            Assert.That(box.BottomLeft, Is.EqualTo(bl));
        }

        [Test]
        public void Box2iFromDimensionsInt([ValueSource(nameof(Sources))] (int, int, int, int) test)
        {
            var (left, top, right, bottom) = test;

            var width = Math.Abs(left - right);
            var height = Math.Abs(top - bottom);

            var box = UIBox2i.FromDimensions(left, top, width, height);

            Assert.That(box.Left, Is.EqualTo(left));
            Assert.That(box.Top, Is.EqualTo(top));
            Assert.That(box.Right, Is.EqualTo(left + width));
            Assert.That(box.Bottom, Is.EqualTo(top + height));

            Assert.That(box.Width, Is.EqualTo(width));
            Assert.That(box.Height, Is.EqualTo(height));
        }

        [Test]
        public void Box2iFromDimensionsVectors([ValueSource(nameof(Sources))] (int, int, int, int) test)
        {
            var (left, top, right, bottom) = test;

            var width = Math.Abs(left - right);
            var height = Math.Abs(top - bottom);
            var size = new Vector2i(width, height);

            var box = UIBox2i.FromDimensions(new Vector2i(left, top), size);

            Assert.That(box.Left, Is.EqualTo(left));
            Assert.That(box.Top, Is.EqualTo(top));
            Assert.That(box.Right, Is.EqualTo(left + width));
            Assert.That(box.Bottom, Is.EqualTo(top + height));

            Assert.That(box.Size, Is.EqualTo(size));
        }

        [Test]
        public void Box2iNotContainsSelfOpen()
        {
            var box = new UIBox2i(-1, -1, 1, 1);

            Assert.That(box.Contains(box.BottomLeft, false), Is.False);
            Assert.That(box.Contains(box.TopLeft, false), Is.False);
            Assert.That(box.Contains(box.TopRight, false), Is.False);
            Assert.That(box.Contains(box.BottomRight, false), Is.False);
        }

        [Test]
        public void Box2iContainsSelfClosed()
        {
            var box = new UIBox2i(-1, -1, 1, 1);

            Assert.That(box.Contains(box.BottomLeft));
            Assert.That(box.Contains(box.TopLeft));
            Assert.That(box.Contains(box.TopRight));
            Assert.That(box.Contains(box.BottomRight));

            var bl = box.BottomLeft;
            var tl = box.TopLeft;
            var tr = box.TopRight;
            var br = box.BottomRight;

            Assert.That(box.Contains(bl.X, bl.Y));
            Assert.That(box.Contains(tl.X, tl.Y));
            Assert.That(box.Contains(tr.X, tr.Y));
            Assert.That(box.Contains(br.X, br.Y));
        }

        [Test]
        public void Box2iContains([ValueSource(nameof(SmallTranslations))]
            (int, int) test)
        {
            var (x, y) = test;
            var vec = new Vector2i(x, y);

            var box = new UIBox2i(-2, -2, 2, 2);

            Assert.That(box.Contains(x, y));
            Assert.That(box.Contains(vec));
            Assert.That(box.Contains(vec, false));
        }

        [Test]
        public void Box2iNotContains([ValueSource(nameof(LargeTranslations))]
            (int, int) test)
        {
            var (x, y) = test;
            var vec = new Vector2i(x, y);

            var box = new UIBox2i(-2, -2, 2, 2);

            Assert.That(box.Contains(x, y), Is.False);
            Assert.That(box.Contains(vec), Is.False);
            Assert.That(box.Contains(vec, false), Is.False);
        }

        [Test]
        public void Box2iTranslated([ValueSource(nameof(LargeTranslations))]
            (int, int) test)
        {
            var (x, y) = test;
            var vec = new Vector2i(x, y);

            var box = new UIBox2i(-1, -1, 1, 1);
            var scaledBox = box.Translated(vec);

            Assert.That(scaledBox.Left, Is.EqualTo(box.Left + x));
            Assert.That(scaledBox.Top, Is.EqualTo(box.Top + y));
            Assert.That(scaledBox.Bottom, Is.EqualTo(box.Bottom + y));
            Assert.That(scaledBox.Right, Is.EqualTo(box.Right + x));
        }

        [Test]
        public void Box2iEquals([ValueSource(nameof(Sources))] (int, int, int, int) test)
        {
            var (left, top, right, bottom) = test;

            var controlBox = new UIBox2i(left, top, right, bottom);
            var differentBox = new UIBox2i(-3, -3, 3, 3);
            var sameBox = new UIBox2i(left, top, right, bottom);
            Object sameBoxAsObject = sameBox;
            UIBox2i? nullBox = null;
            Vector2i notBox = new Vector2i(left, top);

            Assert.That(controlBox.Equals(controlBox));
            Assert.That(controlBox.Equals(differentBox), Is.False);
            Assert.That(controlBox.Equals(sameBox));
            Assert.That(controlBox.Equals(sameBoxAsObject));
            Assert.That(controlBox.Equals(nullBox), Is.False);
            Assert.That(controlBox.Equals(notBox), Is.False);
        }

        [Test]
        public void UIBox2iIntersection(
            [ValueSource(nameof(Intersections))] (UIBox2i a, UIBox2i b, UIBox2i? expected) value)
        {
            var (a, b, expected) = value;

            // This should be a symmetric operation.
            Assert.That(a.Intersection(b), Is.EqualTo(expected));
            Assert.That(b.Intersection(a), Is.EqualTo(expected));
        }
    }
}
