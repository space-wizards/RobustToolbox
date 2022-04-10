using NUnit.Framework;
using Robust.Client.ResourceManagement;

namespace Robust.UnitTesting.Client.ResourceManagement
{
    [TestOf(typeof(RSIResource))]
    [Parallelizable(ParallelScope.All)]
    [TestFixture]
    public sealed class RsiResourceTest
    {
        /// <summary>
        ///     Simple test in which the delays are already identical so nothing should change much.
        /// </summary>
        [Test]
        public void TestFoldDelaysIdentical()
        {
            var delays = new[]
            {
                new float[] {5, 5, 5, 5},
                new float[] {5, 5, 5, 5},
                new float[] {5, 5, 5, 5},
                new float[] {5, 5, 5, 5},
            };

            var (newDelays, indices) = RSIResource.FoldDelays(delays);

            Assert.That(newDelays, Is.EqualTo(new[] {5f, 5f, 5f, 5f}));

            for (var i = 0; i < 4; i++)
            {
                var o = i * 4;
                Assert.That(indices[i], Is.EqualTo(new[] {o, o + 1, o + 2, o + 3}));
            }
        }

        /// <summary>
        ///     Test in which the delays are different but equal amount of frames.
        /// </summary>
        [Test]
        public void TestFoldDelaysDifferent()
        {
            var delays = new[]
            {
                new float[] {5, 5, 5, 5},
                new float[] {5, 7, 3, 5},
                new float[] {5, 5, 5, 5},
                new float[] {5, 5, 5, 5},
            };

            var (newDelays, indices) = RSIResource.FoldDelays(delays);

            Assert.That(newDelays, Is.EqualTo(new[] {5f, 5f, 2f, 3f, 5f}));

            Assert.That(indices[0], Is.EqualTo(new[] {0, 1, 2, 2, 3}));
            Assert.That(indices[1], Is.EqualTo(new[] {4, 5, 5, 6, 7}));
            Assert.That(indices[2], Is.EqualTo(new[] {8, 9, 10, 10, 11}));
            Assert.That(indices[3], Is.EqualTo(new[] {12, 13, 14, 14, 15}));
        }

        /// <summary>
        ///     Test in which the delays are different but equal amount of frames.
        /// </summary>
        [Test]
        public void TestFoldFramesDifferent()
        {
            var delays = new[]
            {
                new float[] {5, 5, 5, 5},
                new float[] {5, 5, 5, 5},
                new float[] {5, 5, 10},
                new float[] {5, 5, 5, 5},
            };

            var (newDelays, indices) = RSIResource.FoldDelays(delays);

            Assert.That(newDelays, Is.EqualTo(new[] {5f, 5f, 5f, 5f}));

            Assert.That(indices[0], Is.EqualTo(new[] {0, 1, 2, 3}));
            Assert.That(indices[1], Is.EqualTo(new[] {4, 5, 6, 7}));
            Assert.That(indices[2], Is.EqualTo(new[] {8, 9, 10, 10}));
            Assert.That(indices[3], Is.EqualTo(new[] {11, 12, 13, 14}));
        }

        /// <summary>
        ///     The very complicated test where a ton of stuff goes down.
        /// </summary>
        [Test]
        public void TestFoldWild()
        {
            var delays = new[]
            {
                new float[] {1, 9},
                new float[] {3, 3, 3, 1},
                new float[] {7, 1, 2},
                new float[] {5, 2, 2, 1},
            };

            var (newDelays, indices) = RSIResource.FoldDelays(delays);

            Assert.That(newDelays, Is.EqualTo(new[] {1f, 2f, 2f, 1f, 1f, 1f, 1f, 1f}));

            Assert.That(indices[0], Is.EqualTo(new[] {0, 1, 1, 1, 1, 1, 1, 1}));
            Assert.That(indices[1], Is.EqualTo(new[] {2, 2, 3, 3, 4, 4, 4, 5}));
            Assert.That(indices[2], Is.EqualTo(new[] {6, 6, 6, 6, 6, 7, 8, 8}));
            Assert.That(indices[3], Is.EqualTo(new[] {9, 9, 9, 10, 10, 11, 11, 12}));
        }
    }
}
