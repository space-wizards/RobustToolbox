using NUnit.Framework;
using Robust.Shared.Utility;
using System.Collections.Generic;

namespace Robust.UnitTesting.Shared.Utility
{
    [Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
    [TestFixture]
    public class CollectionExtensions_Test
    {
        [Test]
        public void RemoveSwapTest()
        {
            var list = new List<int> { 1, 2, 3 };
            list.RemoveSwap(2);
            Assert.That(list, Is.EqualTo(new List<int> { 1, 2 }));

            list = new List<int> { 1, 2, 3 };
            list.RemoveSwap(0);
            Assert.That(list, Is.EqualTo(new List<int> { 3, 2 }));
        }
    }
}
