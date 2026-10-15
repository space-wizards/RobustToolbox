using NUnit.Framework;

namespace Robust.UnitTesting.Shared.Serialization
{
    [TestFixture]
    [NonParallelizable]
    internal sealed class SerializationShutdownTest : OurSerializationTest
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
