using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Utility
{
    [Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
    [TestFixture]
    public sealed class CollectionExtensions_Test
    {
        [Test]
        public void RemoveSwapTest()
        {
            var list = new List<int> {1, 2, 3};
            list.RemoveSwap(2);
            Assert.That(list, Is.EqualTo(new List<int> {1, 2}));

            list = new List<int> {1, 2, 3};
            list.RemoveSwap(0);
            Assert.That(list, Is.EqualTo(new List<int> {3, 2}));
        }

        [Test]
        public void TestFirstOrNull()
        {
            Assert.That(Enumerable.Empty<int>().FirstOrNull(), Is.Null);
            Assert.That(new[] {1}.FirstOrNull(), Is.EqualTo(1));
            Assert.That(new[] {1, 2, 3}.FirstOrNull(), Is.EqualTo(1));

            Assert.That(Enumerable.Empty<int>().FirstOrNull(p => p == 2), Is.Null);
            Assert.That(new[] {1}.FirstOrNull(p => p == 2), Is.Null);
            Assert.That(new[] {1, 2, 3}.FirstOrNull(p => p == 2), Is.EqualTo(2));
        }
    }
}
