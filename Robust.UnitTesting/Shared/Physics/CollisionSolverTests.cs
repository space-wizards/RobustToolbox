using System;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.UnitTesting.Shared.Physics
{
    [TestFixture, Parallelizable, TestOf(typeof(CollisionSolver))]
    class CollisionSolverTests
    {
        [Test]
        public void CircleCircle_NotCollide()
        {
            var a = new Circle(new Vector2(0,0), 0.5f);
            var b = new Circle(new Vector2(1, 1), 0.5f);

            CollisionSolver.CalculateCollisionFeatures(in a, in b, 1, out var results);

            Assert.That(results.Collided, Is.EqualTo(false));
        }

        [Test]
        public void CircleCircle_Collide()
        {
            var a = new Circle(new Vector2(0, 0), 1f);
            var b = new Circle(new Vector2(1.5f, 0), 1f);

            CollisionSolver.CalculateCollisionFeatures(in a, in b, 1, out var results);

            Assert.That(results.Collided);
            Assert.That(results.Normal, Is.EqualTo(Vector2.UnitX));
            Assert.That(results.Penetration, Is.EqualTo(0.5f / 2));
            Assert.That(results.Contacts, Is.Not.Null);
            Assert.That(results.Contacts.Length, Is.EqualTo(1));
            Assert.That(results.Contacts[0], Is.EqualTo(new Vector2(0.75f, 0)));
        }

        [Test]
        public void CircleCircle_SamePos()
        {
            var circle = new Circle(new Vector2(0, 0), 0.5f);

            CollisionSolver.CalculateCollisionFeatures(in circle, in circle, 1, out var results);

            Assert.That(results.Collided, Is.EqualTo(false));
        }

        [Test]
        public void InverseTransformPoint()
        {
            var obb = new OrientedRectangle(Vector2.One, Vector2.Zero, MathF.PI / 2);
            var worldPoint = new Vector2(1, 3);

            var localPoint = obb.InverseTransformPoint(worldPoint);

            Assert.That(localPoint, Is.Approximately(new Vector2(2, 0)));
        }

        [Test]
        public void TransformPoint()
        {
            var obb = new OrientedRectangle(Vector2.One, Vector2.Zero, MathF.PI / 2);
            var worldPoint = new Vector2(2, 0);

            var localPoint = obb.TransformPoint(worldPoint);

            Assert.That(localPoint, Is.Approximately(new Vector2(1, 3)));
        }

        [Test]
        public void TransformPointRoundTrip()
        {
            var obb = new OrientedRectangle(new Vector2(3, 5), Vector2.Zero, MathF.PI / 4);
            var worldPoint = new Vector2(11, 13);

            var localPoint = obb.InverseTransformPoint(worldPoint);
            var result = obb.TransformPoint(localPoint);

            Assert.That(result, Is.EqualTo(worldPoint));
        }

        [Test]
        public void ClosestPoint()
        {
            var obb = new OrientedRectangle(Vector2.One, Vector2.One * 0.5f, MathF.PI / 2);
            var worldPoint = new Vector2(13, -13);

            var closestPoint = obb.ClosestPointWorld(worldPoint);

            Assert.That(closestPoint, Is.Approximately(new Vector2(1.5f, 0.5f)));
        }

        [Test]
        public void RectCircle_NotCollide()
        {
            var a = new OrientedRectangle(Vector2.Zero, Vector2.One * 0.5f, MathF.PI / 4);
            var b = new Circle(new Vector2(1, 1), 0.5f);

            CollisionSolver.CalculateCollisionFeatures(in a, in b, 1, out var results);

            Assert.That(results.Collided, Is.EqualTo(false));
        }

        [Test]
        public void RectCircle_Collide()
        {
            var a = new OrientedRectangle(Vector2.Zero, new Vector2(3, 1), MathF.PI / 2);
            var b = new Circle(new Vector2(1.5f, 0), 1f);

            CollisionSolver.CalculateCollisionFeatures(in a, in b, 1, out var results);

            Assert.That(results.Collided);
            Assert.That(results.Normal, Is.Approximately(Vector2.UnitX));
            Assert.That(results.Penetration, Is.EqualTo(0.5f / 2));
            Assert.That(results.Contacts, Is.Not.Null);
            Assert.That(results.Contacts.Length, Is.EqualTo(1));
            Assert.That(results.Contacts[0], Is.Approximately(new Vector2(0.75f, 0)));
        }

        [Test]
        public void BoxBox_NotCollide()
        {
            var a = new AlignedRectangle(new Vector2(1, 0), new Vector2(0.5f, 0.5f));
            var b = new AlignedRectangle(Vector2.Zero, new Vector2(0.5f, 0.5f));

            CollisionSolver.CalculateCollisionFeatures(in a, in b, 1, out var results);

            Assert.That(results.Collided, Is.False);
        }

        [Test]
        public void BoxBox_Collide()
        {
            var a = new AlignedRectangle(new Vector2(1, 0), new Vector2(0.5f, 0.5f));
            var b = new AlignedRectangle(new Vector2(0.25f, 0.15f), new Vector2(0.5f, 0.5f));

            CollisionSolver.CalculateCollisionFeatures(in a, in b, 1, out var results);

            Assert.That(results.Collided, Is.True);
            Assert.That(results.Normal, Is.EqualTo(new Vector2(-1, 0)));
            Assert.That(results.Penetration, Is.EqualTo(0.25f / 2));
            Assert.That(results.Contacts, Is.Not.Null);
            Assert.That(results.Contacts.Length, Is.EqualTo(1));
            Assert.That(results.Contacts[0], Is.EqualTo(new Vector2(0.5f, 0.15f)));
        }
    }
}
