using System;
using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Maths
{
    [Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
    [TestFixture]
    [TestOf(typeof(Box2))]
    public sealed class Box2_Test
    {
        private static IEnumerable<(float left, float bottom, float right, float top)> Sources =>
            new (float, float, float, float)[]
            {
                (0, 0, 0, 0),
                (0, 0, 0, 1),
                (0, 0, 1, 0),
                (0, 0, 1, 1),
                (0, -1, 0, 0),
                (0, -1, 0, 1),
                (0, -1, 1, 0),
                (0, -1, 1, 1),
                (-1, 0, 0, 0),
                (-1, 0, 0, 1),
                (-1, 0, 1, 0),
                (-1, 0, 1, 1),
                (-1, -1, 0, 0),
                (-1, -1, 0, 1),
                (-1, -1, 1, 0),
                (-1, -1, 1, 1)
            };

        private static IEnumerable<(float x, float y)> SmallTranslations => new[]
        {
            (0, 0.1f),
            (0.1f, 0),
            (0.1f, 0.1f),
            (0, -0.1f),
            (0.1f, -0.1f),
            (-0.1f, 0),
            (-0.1f, 0.1f),
            (-0.1f, -0.1f)
        };

        private static IEnumerable<(float x, float y)> LargeTranslations => new (float, float)[]
        {
            (0, 5),
            (5, 0),
            (5, 5),
            (0, -5),
            (5, -5),
            (-5, 0),
            (-5, 5),
            (-5, -5)
        };

        private static IEnumerable<float> Scalars => new[]
        {
            0.0f,
            0.1f,
            1.0f,
            5.0f,
            10.0f
        };

        /// <summary>
        ///     Check whether the sources list has correct data.
        ///     That is, no boxes where left > right or top > bottom.
        /// </summary>
        [Test]
        public void AssertSourcesValid([ValueSource(nameof(Sources))] (float, float, float, float) test)
        {
            var (left, bottom, right, top) = test;
            Assert.That(right, Is.GreaterThanOrEqualTo(left));
            Assert.That(top, Is.GreaterThanOrEqualTo(bottom));
        }

        [Test]
        public void Box2VectorConstructor([ValueSource(nameof(Sources))] (float, float, float, float) test)
        {
            var (left, bottom, right, top) = test;
            var box = new Box2(new Vector2(left, bottom), new Vector2(right, top));

            Assert.That(box.Left, Is.EqualTo(left));
            Assert.That(box.Top, Is.EqualTo(top));
            Assert.That(box.Right, Is.EqualTo(right));
            Assert.That(box.Bottom, Is.EqualTo(bottom));
        }

        [Test]
        public void Box2EdgesConstructor([ValueSource(nameof(Sources))] (float, float, float, float) test)
        {
            var (left, bottom, right, top) = test;
            var box = new Box2(left, bottom, right, top);

            Assert.That(box.Left, Is.EqualTo(left));
            Assert.That(box.Top, Is.EqualTo(top));
            Assert.That(box.Right, Is.EqualTo(right));
            Assert.That(box.Bottom, Is.EqualTo(bottom));
        }

        [Test]
        public void Box2CornerVectorProperties([ValueSource(nameof(Sources))] (float, float, float, float) test)
        {
            var (left, bottom, right, top) = test;
            var box = new Box2(left, bottom, right, top);

            var br = new Vector2(right, bottom);
            var tl = new Vector2(left, top);
            var tr = new Vector2(right, top);
            var bl = new Vector2(left, bottom);

            Assert.That(box.BottomRight, Is.EqualTo(br));
            Assert.That(box.TopLeft, Is.EqualTo(tl));
            Assert.That(box.TopRight, Is.EqualTo(tr));
            Assert.That(box.BottomLeft, Is.EqualTo(bl));
        }

        [Test]
        public void Box2FromDimensionsFloats([ValueSource(nameof(Sources))] (float, float, float, float) test)
        {
            var (left, bottom, right, top) = test;

            var width = Math.Abs(left - right);
            var height = Math.Abs(top - bottom);

            var box = Box2.FromDimensions(left, bottom, width, height);

            Assert.That(box.Left, Is.EqualTo(left));
            Assert.That(box.Bottom, Is.EqualTo(bottom));
            Assert.That(box.Right, Is.EqualTo(left + width));
            Assert.That(box.Top, Is.EqualTo(bottom + height));

            Assert.That(box.Width, Is.EqualTo(width));
            Assert.That(box.Height, Is.EqualTo(height));
        }

        [Test]
        public void Box2FromDimensionsVectors([ValueSource(nameof(Sources))] (float, float, float, float) test)
        {
            var (left, bottom, right, top) = test;

            var width = Math.Abs(left - right);
            var height = Math.Abs(top - bottom);
            var size = new Vector2(width, height);

            var box = Box2.FromDimensions(new Vector2(left, bottom), size);

            Assert.That(box.Left, Is.EqualTo(left));
            Assert.That(box.Top, Is.EqualTo(top));
            Assert.That(box.Right, Is.EqualTo(left + width));
            Assert.That(box.Top, Is.EqualTo(bottom + height));

            Assert.That(box.Size, Is.EqualTo(size));
        }

        [Test]
        public void Box2IntersectsSelf([ValueSource(nameof(Sources))] (float, float, float, float) test)
        {
            var (left, bottom, right, top) = test;

            var box = new Box2(left, bottom, right, top);

            Assert.That(box.Intersects(box));
        }

        [Test]
        public void Box2IntersectsWithSmallTranslation([ValueSource(nameof(SmallTranslations))]
            (float, float) test)
        {
            var (x, y) = test;

            var box = new Box2(-1, -1, 1, 1);
            var translatedBox = box.Translated(new Vector2(x, y));

            Assert.That(box.Intersects(translatedBox));
        }

        [Test]
        public void Box2NotIntersectsWithLargeTranslation([ValueSource(nameof(LargeTranslations))]
            (float, float) test)
        {
            var (x, y) = test;

            var box = new Box2(-1, -1, 1, 1);
            var translatedBox = box.Translated(new Vector2(x, y));

            Assert.That(box.Intersects(translatedBox), Is.False);
        }

        [Test]
        public void Box2Intersect()
        {
            var boxOne = new Box2(-1, -1, 1, 1);
            var boxTwo = new Box2(0, 0, 2, 2);

            var result = boxOne.Intersect(boxTwo);

            Assert.That(result.Left, Is.EqualTo(0f));
            Assert.That(result.Bottom, Is.EqualTo(0f));
            Assert.That(result.Right, Is.EqualTo(1f));
            Assert.That(result.Top, Is.EqualTo(1f));
        }

        [Test]
        public void Box2NotIntersect()
        {
            var boxOne = new Box2(-1, -1, 0, 0);
            var boxTwo = new Box2(0, 0, 2, 2);

            var result = boxOne.Intersect(boxTwo);

            Assert.That(result.Left, Is.EqualTo(0f));
            Assert.That(result.Bottom, Is.EqualTo(0f));
            Assert.That(result.Right, Is.EqualTo(0f));
            Assert.That(result.Top, Is.EqualTo(0f));
        }

        [Test]
        public void Box2Union()
        {
            var boxOne = new Box2(-1, -1, 1, 1);
            var boxTwo = new Box2(0, 0, 2, 2);

            var result = boxOne.Union(boxTwo);

            Assert.That(result.Left, Is.EqualTo(-1f));
            Assert.That(result.Bottom, Is.EqualTo(-1f));
            Assert.That(result.Right, Is.EqualTo(2f));
            Assert.That(result.Top, Is.EqualTo(2f));
        }

        [Test]
        public void Box2IsEmpty()
        {
            var degenerateBox = new Box2(0, 0, 0, 0);

            Assert.That(degenerateBox.IsEmpty());

            var tallDegenBox = new Box2(0, -1, 0, 1);
            var wideDegenBox = new Box2(-1, 0, 1, 0);
            var meatyBox = new Box2(-1, -1, 1, 1);

            Assert.That(tallDegenBox.IsEmpty(), Is.False);
            Assert.That(wideDegenBox.IsEmpty(), Is.False);
            Assert.That(meatyBox.IsEmpty(), Is.False);
        }

        [Test]
        public void Box2NotEnclosesSelf()
        {
            var box = new Box2(-1, -1, 1, 1);

            Assert.That(box.Encloses(box), Is.False);
        }

        [Test]
        public void Box2ScaledEncloses()
        {
            var box = new Box2(-1, -1, 1, 1);
            var smallBox = box.Scale(0.5f);
            var bigBox = box.Scale(2.0f);

            Assert.That(box.Encloses(smallBox));
            Assert.That(box.Encloses(bigBox), Is.False);
            Assert.That(smallBox.Encloses(box), Is.False);
            Assert.That(bigBox.Encloses(box));
        }

        [Test]
        public void Box2TranslatedNotEncloses([ValueSource(nameof(LargeTranslations))]
            (float, float) test)
        {
            var (x, y) = test;

            var box = new Box2(-1, -1, 1, 1);
            var translatedBox = box.Translated(new Vector2(x, y));

            Assert.That(box.Encloses(translatedBox), Is.False);
            Assert.That(translatedBox.Encloses(box), Is.False);
        }

        [Test]
        public void Box2NotContainsSelfOpen()
        {
            var box = new Box2(-1, -1, 1, 1);

            Assert.That(box.Contains(box.BottomLeft, false), Is.False);
            Assert.That(box.Contains(box.TopLeft, false), Is.False);
            Assert.That(box.Contains(box.TopRight, false), Is.False);
            Assert.That(box.Contains(box.BottomRight, false), Is.False);
        }

        [Test]
        public void Box2ContainsSelfClosed()
        {
            var box = new Box2(-1, -1, 1, 1);

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
        public void Box2Contains([ValueSource(nameof(SmallTranslations))]
            (float, float) test)
        {
            var (x, y) = test;
            var vec = new Vector2(x, y);

            var box = new Box2(-1, -1, 1, 1);

            Assert.That(box.Contains(x, y));
            Assert.That(box.Contains(vec));
            Assert.That(box.Contains(vec, false));
        }

        [Test]
        public void Box2NotContains([ValueSource(nameof(LargeTranslations))]
            (float, float) test)
        {
            var (x, y) = test;
            var vec = new Vector2(x, y);

            var box = new Box2(-1, -1, 1, 1);

            Assert.That(box.Contains(x, y), Is.False);
            Assert.That(box.Contains(vec), Is.False);
            Assert.That(box.Contains(vec, false), Is.False);
        }

        [Test]
        public void Box2Scale([ValueSource(nameof(Scalars))] float scalar)
        {
            var box = new Box2(-1, -1, 1, 1);
            var scaledBox = box.Scale(scalar);

            Assert.That(scaledBox.Center, Is.EqualTo(box.Center));
            Assert.That(scaledBox.Size, Is.EqualTo(box.Size * scalar));
        }

        [Test]
        public void Box2ScaleNegativeException()
        {
            var box = new Box2(-1, -1, 1, 1);
            Assert.That(() => box.Scale(-1), Throws.Exception);
        }

        [Test]
        public void Box2Translated([ValueSource(nameof(LargeTranslations))]
            (float, float) test)
        {
            var (x, y) = test;
            var vec = new Vector2(x, y);

            var box = new Box2(-1, -1, 1, 1);
            var scaledBox = box.Translated(vec);

            Assert.That(scaledBox.Left, Is.EqualTo(box.Left + x));
            Assert.That(scaledBox.Top, Is.EqualTo(box.Top + y));
            Assert.That(scaledBox.Bottom, Is.EqualTo(box.Bottom + y));
            Assert.That(scaledBox.Right, Is.EqualTo(box.Right + x));
        }

        [Test]
        public void Box2Equals([ValueSource(nameof(Sources))] (float, float, float, float) test)
        {
            var (left, top, right, bottom) = test;

            var controlBox = new Box2(left, bottom, right, top);
            var differentBox = new Box2(-MathHelper.Pi, -MathHelper.Pi, MathHelper.Pi, MathHelper.Pi);
            var sameBox = new Box2(left, bottom, right, top);
            Object sameBoxAsObject = sameBox;
            Box2? nullBox = null;
            Vector2 notBox = new Vector2(left, top);

            Assert.That(controlBox.Equals(controlBox));
            Assert.That(controlBox.Equals(differentBox), Is.False);
            Assert.That(controlBox.Equals(sameBox));
            Assert.That(controlBox.Equals(sameBoxAsObject));
            // ReSharper disable once ExpressionIsAlwaysNull
            Assert.That(controlBox.Equals(nullBox), Is.False);
            // ReSharper disable once SuspiciousTypeConversion.Global
            Assert.That(controlBox.Equals(notBox), Is.False);
        }

        [Test]
        public void Box2EqualsOperator([ValueSource(nameof(Sources))] (float, float, float, float) test)
        {
            var (left, top, right, bottom) = test;

            var controlBox = new Box2(left, bottom, right, top);
            var differentBox = new Box2(-MathHelper.Pi, -MathHelper.Pi, MathHelper.Pi, MathHelper.Pi);
            var sameBox = new Box2(left, bottom, right, top);

#pragma warning disable CS1718 // Comparison made to same variable
            // ReSharper disable once EqualExpressionComparison
            Assert.That(controlBox == controlBox);
#pragma warning restore CS1718 // Comparison made to same variable
            Assert.That(controlBox == differentBox, Is.False);
            Assert.That(controlBox == sameBox);
        }

        [Test]
        public void Box2InequalsOperator([ValueSource(nameof(Sources))] (float, float, float, float) test)
        {
            var (left, top, right, bottom) = test;

            var controlBox = new Box2(left, bottom, right, top);
            var differentBox = new Box2(-MathHelper.Pi, -MathHelper.Pi, MathHelper.Pi, MathHelper.Pi);
            var sameBox = new Box2(left, bottom, right, top);

#pragma warning disable CS1718 // Comparison made to same variable
            // ReSharper disable once EqualExpressionComparison
            Assert.That(controlBox != controlBox, Is.False);
#pragma warning restore CS1718 // Comparison made to same variable
            Assert.That(controlBox != differentBox);
            Assert.That(controlBox != sameBox, Is.False);
        }

        [Test]
        public void Box2CenteredAround()
        {
            var center = new Vector2(1, 1);
            var size = new Vector2(1, 1);

            var box = Box2.CenteredAround(center, size);

            Assert.That(box.Center, Is.EqualTo(center));
            Assert.That(box.Size, Is.EqualTo(size));
        }

        [Test]
        public void Enlarged()
        {
            var box = new Box2(1, 1, 2, 2).Enlarged(1);

            Assert.That(box, Is.EqualTo(new Box2(0, 0, 3, 3)));
        }
    }
}
