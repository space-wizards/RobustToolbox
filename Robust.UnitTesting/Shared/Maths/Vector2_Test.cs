using NUnit.Framework;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Maths
{
    [TestFixture]
    [Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
    [TestOf(typeof(Vector2))]
    public sealed class Vector2_Test
    {
        [Test]
        [Sequential]
        public void ConstructorTest([Random(-1.0f, 1.0f, 5)] float x,
                                    [Random(-1.0f, 1.0f, 5)] float y)
        {
            var vec = new Vector2(x, y);
            Assert.That(vec.X, Is.EqualTo(x));
            Assert.That(vec.Y, Is.EqualTo(y));

            Assert.That(Vector2.One, Is.EqualTo(new Vector2(1, 1)));
            Assert.That(Vector2.Zero, Is.EqualTo(new Vector2(0, 0)));
            Assert.That(new Vector2(), Is.EqualTo(new Vector2(0, 0)));
        }

        // Testing basic operators: +, -, *, /
        [Test]
        [Sequential]
        public void ArithmeticTest([Random(-1.0f, 1.0f, 5)] float x1,
                                   [Random(-1.0f, 1.0f, 5)] float y1,
                                   [Random(-1.0f, 1.0f, 5)] float x2,
                                   [Random(-1.0f, 1.0f, 5)] float y2,
                                   [Random(-1.0f, 1.0f, 5)] float scale)
        {
            var vec1 = new Vector2(x1, y1);
            var vec2 = new Vector2(x2, y2);

            var add = vec1 + vec2;
            Assert.That(add.X, Is.EqualTo(x1 + x2));
            Assert.That(add.Y, Is.EqualTo(y1 + y2));

            var sub = vec1 - vec2;
            Assert.That(sub.X, Is.EqualTo(x1 - x2));
            Assert.That(sub.Y, Is.EqualTo(y1 - y2));

            var neg = -vec1;
            Assert.That(neg.X, Is.EqualTo(-x1));
            Assert.That(neg.Y, Is.EqualTo(-y1));

            var mul = vec1 * vec2;
            Assert.That(mul.X, Is.EqualTo(x1 * x2));
            Assert.That(mul.Y, Is.EqualTo(y1 * y2));

            var muls = vec1 * scale;
            Assert.That(muls.X, Is.EqualTo(x1 * scale));
            Assert.That(muls.Y, Is.EqualTo(y1 * scale));

            var div = vec1 / vec2;
            Assert.That(div.X, Is.EqualTo(x1 / x2));
            Assert.That(div.Y, Is.EqualTo(y1 / y2));

            var divs = vec1 / scale;
            Assert.That(divs.X, Is.EqualTo(x1 / scale));
            Assert.That(divs.Y, Is.EqualTo(y1 / scale));
        }

        [Test]
        public void ComponentMinMaxTest()
        {
            var vec1 = new Vector2(-1, 1);
            var vec2 = new Vector2(1, -1);

            Assert.That(Vector2.ComponentMin(vec1, vec2), Is.EqualTo(new Vector2(-1, -1)));
            Assert.That(Vector2.ComponentMax(vec1, vec2), Is.EqualTo(new Vector2(1, 1)));
        }

        [Test]
        [Sequential]
        public void OpenTKConversionTest([Random(-1.0f, 1.0f, 5)] float x,
                                         [Random(-1.0f, 1.0f, 5)] float y)
        {
            var vec = new Vector2(x, y);
            Vector2 ovec = vec;

            Assert.That(ovec.X, Is.EqualTo(x));
            Assert.That(ovec.Y, Is.EqualTo(y));

            Assert.That(ovec, Is.EqualTo(vec));
        }
    }
}
