using NUnit.Framework;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Utility
{
    [Parallelizable(ParallelScope.All)]
    [TestFixture]
    [TestOf(typeof(MarshalHelper))]
    public sealed class MarshalHelper_Test
    {
        [Test]
        public unsafe void FindNullTerminatorTest()
        {
            var data = stackalloc byte[]
            {
                1, 2, 3, 0
            };

            Assert.That(MarshalHelper.FindNullTerminator(data), Is.EqualTo(3));
        }
    }
}
