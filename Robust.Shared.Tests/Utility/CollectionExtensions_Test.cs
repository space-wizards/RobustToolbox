using NUnit.Framework;
using Robust.Shared.Utility;

namespace Robust.Shared.Tests.Utility
{
    [Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
    [TestFixture]
    internal sealed class CollectionExtensions_Test
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

        [Test]
        public void DictionaryEqualsTest()
        {
            var dict = new Dictionary<string, int>
            {
                ["one"] = 1,
                ["two"] = 2,
            };

            var same = new Dictionary<string, int>
            {
                ["two"] = 2,
                ["one"] = 1,
            };

            var differentCount = new Dictionary<string, int>
            {
                ["one"] = 1,
            };

            var differentKey = new Dictionary<string, int>
            {
                ["one"] = 1,
                ["three"] = 2,
            };

            var differentValue = new Dictionary<string, int>
            {
                ["one"] = 1,
                ["two"] = 3,
            };

            Assert.Multiple(() =>
            {
                Assert.That(dict.DictionaryEquals(dict), Is.True);
                Assert.That(dict.DictionaryEquals(same), Is.True);
                Assert.That(dict.DictionaryEquals(differentCount), Is.False);
                Assert.That(dict.DictionaryEquals(differentKey), Is.False);
                Assert.That(dict.DictionaryEquals(differentValue), Is.False);
            });
        }
    }
}
