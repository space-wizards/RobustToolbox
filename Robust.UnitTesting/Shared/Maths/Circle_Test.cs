using System;
using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Maths
{
    [Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
    [TestFixture]
    [TestOf(typeof(Circle))]
    public sealed class Circle_Test
    {
        private static IEnumerable<float> Coordinates => new float[] { -1, 0, 1 };

        // Like coordinate x coordinate, except without (0, 0)
        private static IEnumerable<(float, float)> Offsets => new (float, float)[]
        {
            (0, -1),
            (0, 1),
            (1, 0),
            (1, -1),
            (1, 1),
            (-1, 0),
            (-1, -1),
            (-1, 1)
        };

        private static IEnumerable<float> Radii => new float[]
        {
            0.1f,
            1f,
            5f,
            10f
        };

        [Test]
        public void CircleConstructor([ValueSource(nameof(Coordinates))] float x,
                                      [ValueSource(nameof(Coordinates))] float y,
                                      [ValueSource(nameof(Radii))] float radius)
        {
            var centerVec = new Vector2(x, y);
            var circle = new Circle(centerVec, radius);

            Assert.That(circle.Position, Is.EqualTo(centerVec));
            Assert.That(circle.Radius, Is.EqualTo(radius));
        }

        [Test]
        public void CircleDegenerateContains([ValueSource(nameof(Coordinates))] float x,
                                             [ValueSource(nameof(Coordinates))] float y)
        {
            var centerVec = new Vector2(x, y);
            var circle = new Circle(centerVec, 0);

            Assert.That(circle.Contains(centerVec));
        }

        [Test]
        public void CircleContains([ValueSource(nameof(Coordinates))] float x,
                                   [ValueSource(nameof(Coordinates))] float y,
                                   [ValueSource(nameof(Radii))] float radius,
                                   [ValueSource(nameof(Offsets))] (float, float) offset)
        {
            var centerVec = new Vector2(x, y);
            var circle = new Circle(centerVec, radius);

            Assert.That(circle.Contains(centerVec));

            var (offX, offY) = offset;

            var offsetDirection = new Vector2(offX, offY).Normalized;
            var pointInside = centerVec + offsetDirection * (radius * 0.5f);
            var pointOn = centerVec + offsetDirection * radius;
            var pointOutside = centerVec + offsetDirection * (radius * 1.5f);

            Assert.That(circle.Contains(pointInside));
            Assert.That(circle.Contains(pointOn));
            Assert.That(circle.Contains(pointOutside), Is.False);
        }

        [Test]
        public void CircleIntersectsCircle([ValueSource(nameof(Coordinates))] float x,
                                           [ValueSource(nameof(Coordinates))] float y,
                                           [ValueSource(nameof(Radii))] float radius,
                                           [ValueSource(nameof(Offsets))] (float, float) offset)
        {
            var centerVec = new Vector2(x, y);
            var circle = new Circle(centerVec, radius);

            Assert.That(circle.Intersects(circle));

            var (offX, offY) = offset;

            var offsetDirection = new Vector2(offX, offY).Normalized;
            var pointOn = centerVec + offsetDirection * radius;
            var pointFar = centerVec + offsetDirection * (radius * 4);

            var circleOn = new Circle(pointOn, radius);
            var circleFar = new Circle(pointFar, radius);

            Assert.That(circle.Intersects(circleOn));
            Assert.That(circle.Intersects(circleFar), Is.False);

            Assert.That(circleOn.Intersects(circle));
            Assert.That(circleFar.Intersects(circle), Is.False);
        }

        [Test]
        public void CircleIntersectsBox2([ValueSource(nameof(Coordinates))] float x,
                                         [ValueSource(nameof(Coordinates))] float y,
                                         [ValueSource(nameof(Radii))] float radius,
                                         [ValueSource(nameof(Offsets))] (float, float) offset)
        {
            var centerVec = new Vector2(x, y);
            var circle = new Circle(centerVec, radius);

            var boxDim = 1f;

            var (offX, offY) = offset;
            var offsetDirection = new Vector2(offX, offY).Normalized;

            var boxCenterOn = centerVec + offsetDirection * radius;
            var boxCenterFar = centerVec + offsetDirection * (radius * 20);

            var boxIn = Box2.FromDimensions(boxCenterOn.X - boxDim / 2f, boxCenterOn.Y - boxDim / 2f, boxDim, boxDim);
            var boxOut = Box2.FromDimensions(boxCenterFar.X - boxDim / 2f, boxCenterFar.Y - boxDim / 2f, boxDim, boxDim);

            Assert.That(circle.Intersects(boxIn));
            Assert.That(circle.Intersects(boxOut), Is.False);
        }

        [Test]
        public void Box2Equals([ValueSource(nameof(Coordinates))] float x,
                               [ValueSource(nameof(Coordinates))] float y,
                               [ValueSource(nameof(Radii))] float radius)
        {
            var centerVec = new Vector2(x, y);

            var controlCircle = new Circle(centerVec, radius);
            var circleDifferentRadius = new Circle(centerVec, 100);
            var circleDifferentPosition = new Circle(new Vector2(100, 0), radius);
            var sameCircle = new Circle(centerVec, radius);
            Object sameCircleAsObject = sameCircle;
            Circle? nullCircle = null;
            Vector2 notCircle = centerVec;

            Assert.That(controlCircle.Equals(controlCircle));
            Assert.That(controlCircle.Equals(circleDifferentRadius), Is.False);
            Assert.That(controlCircle.Equals(circleDifferentPosition), Is.False);
            Assert.That(controlCircle.Equals(sameCircle));
            Assert.That(controlCircle.Equals(sameCircleAsObject));
            Assert.That(controlCircle.Equals(nullCircle), Is.False);
            Assert.That(controlCircle.Equals(notCircle), Is.False);
        }

        [Test]
        public void Box2EqualsOperator([ValueSource(nameof(Coordinates))] float x,
                                       [ValueSource(nameof(Coordinates))] float y,
                                       [ValueSource(nameof(Radii))] float radius)
        {
            var centerVec = new Vector2(x, y);

            var controlCircle = new Circle(centerVec, radius);
            var circleDifferentRadius = new Circle(centerVec, 100);
            var circleDifferentPosition = new Circle(new Vector2(100, 0), radius);
            var sameCircle = new Circle(centerVec, radius);

#pragma warning disable CS1718 // Comparison made to same variable
            Assert.That(controlCircle == controlCircle);
#pragma warning restore CS1718 // Comparison made to same variable
            Assert.That(controlCircle == circleDifferentRadius, Is.False);
            Assert.That(controlCircle == circleDifferentPosition, Is.False);
            Assert.That(controlCircle == sameCircle);
        }

        [Test]
        public void Box2InequalsOperator([ValueSource(nameof(Coordinates))] float x,
                                         [ValueSource(nameof(Coordinates))] float y,
                                         [ValueSource(nameof(Radii))] float radius)
        {
            var centerVec = new Vector2(x, y);

            var controlCircle = new Circle(centerVec, radius);
            var circleDifferentRadius = new Circle(centerVec, 100);
            var circleDifferentPosition = new Circle(new Vector2(100, 0), radius);
            var sameCircle = new Circle(centerVec, radius);

#pragma warning disable CS1718 // Comparison made to same variable
            Assert.That(controlCircle != controlCircle, Is.False);
#pragma warning restore CS1718 // Comparison made to same variable
            Assert.That(controlCircle != circleDifferentRadius);
            Assert.That(controlCircle != circleDifferentPosition);
            Assert.That(controlCircle != sameCircle, Is.False);
        }
    }
}
