using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.UnitTesting.Shared.Physics
{
    [TestFixture, Parallelizable]
    [TestOf(typeof(IVerticesSimplifier))]
    public sealed class VerticesSimplifier_Test : RobustUnitTest
    {
        /*
         * Collinear tests
         */
        [Test]
        public void TestCollinearLine()
        {
            var simplifier = new CollinearSimplifier();

            var line = new List<Vector2>
            {
                new(0.0f, 0f),
                new(0.5f, 0f),
                new(1.0f, 0f),
                new(1.5f, 0f),
                new(2.0f, 0f),
            };

            Assert.That(simplifier.Simplify(line, 0.01f).Count, Is.EqualTo(2));
        }

        [Test]
        public void TestCollinearBox()
        {
            // Box should still simplify to a box.
            var simplifier = new CollinearSimplifier();

            var line = new List<Vector2>
            {
                new(0.0f, 0f),
                new(0.0f, 1.0f),
                new(1.0f, 1.0f),
                new(1.0f, 0f),
            };

            Assert.That(simplifier.Simplify(line, 0.01f).Count, Is.EqualTo(4));
        }

        [Test]
        public void TestCollinearSquiggle()
        {
            var simplifier = new CollinearSimplifier();

            var line = new List<Vector2>
            {
                new(0.0f, 0f),
                new(0.5f, 0.05f),
                new(1.0f, 0f),
                new(1.5f, -0.05f),
                new(2.0f, 0f),
            };

            Assert.That(simplifier.Simplify(line, 0.1f).Count, Is.EqualTo(2));
        }


        /*
         * Douglas Peucker tests
         */
        [Test]
        public void TestDouglasPeuckerLine()
        {
            var simplifier = new RamerDouglasPeuckerSimplifier();

            var line = new List<Vector2>
            {
                new(0.0f, 0f),
                new(0.5f, 0f),
                new(1.0f, 0f),
                new(1.5f, 0f),
                new(2.0f, 0f),
            };

            Assert.That(simplifier.Simplify(line, 0.01f).Count, Is.EqualTo(2));
        }

        [Test]
        public void TestDouglasPeuckerBox()
        {
            // Box should still simplify to a box.
            var simplifier = new RamerDouglasPeuckerSimplifier();

            var line = new List<Vector2>
            {
                new(0.0f, 0f),
                new(0.0f, 1.0f),
                new(1.0f, 1.0f),
                new(1.0f, 0f),
            };

            Assert.That(simplifier.Simplify(line, 0.01f).Count, Is.EqualTo(4));
        }

        [Test]
        public void TestDouglasPeuckerSquiggle()
        {
            var simplifier = new RamerDouglasPeuckerSimplifier();

            var line = new List<Vector2>
            {
                new(0.0f, 0f),
                new(0.5f, 0.05f),
                new(1.0f, 0f),
                new(1.5f, -0.05f),
                new(2.0f, 0f),
            };

            Assert.That(simplifier.Simplify(line, 0.1f).Count, Is.EqualTo(2));
        }
    }
}
