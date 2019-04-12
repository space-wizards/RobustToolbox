using NUnit.Framework;
using Robust.Client;
using Robust.Server;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Utility
{
    [TestFixture]
    [TestOf(typeof(TypeHelpers))]
    public class TypeHelpers_Test
    {
        public void TestIsServerSide()
        {
            Assert.That(typeof(BaseServer).IsServerSide());
            Assert.That(!typeof(BaseClient).IsServerSide());
            Assert.That(!typeof(Vector2).IsServerSide());
        }

        public void TestIsClientSide()
        {
            Assert.That(!typeof(BaseServer).IsClientSide());
            Assert.That(typeof(BaseClient).IsClientSide());
            Assert.That(!typeof(Vector2).IsClientSide());
        }

        public void TestIsSharedSide()
        {
            Assert.That(!typeof(BaseServer).IsSharedSide());
            Assert.That(!typeof(BaseClient).IsSharedSide());
            Assert.That(typeof(Vector2).IsSharedSide());
        }
    }
}
