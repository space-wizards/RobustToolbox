using System;
using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Maths
{
    [Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
    [TestFixture]
    [TestOf(typeof(Box2Rotated))]
    public class Box2Rotated_Test
    {


        [Test]
        public void GetBox2RotatedPoints() {
            Box2Rotated testBox = new Box2Rotated(new Box2(-5, -5, 5, 5), MathHelper.PiOver4);
            Assert.That(testBox.TopRight, Is.EqualTo(new Vector2(0, 7.071068f)));
        }
    }
}
