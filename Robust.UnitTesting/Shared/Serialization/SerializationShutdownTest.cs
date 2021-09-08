using NUnit.Framework;

namespace Robust.UnitTesting.Shared.Serialization
{
    [TestFixture]
    public class SerializationShutdownTest : SerializationTest
    {
        [Test]
        public void SerializationInitializeShutdownInitializeTest()
        {
            Assert.DoesNotThrow(() =>
            {
                // First initialize is done in the parent class
                Serialization.Shutdown();
                Serialization.Initialize();
            });
        }
    }
}
