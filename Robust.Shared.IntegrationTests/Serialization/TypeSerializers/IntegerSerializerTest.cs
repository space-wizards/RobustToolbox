using NUnit.Framework;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers
{
    [TestFixture]
    internal sealed class IntegerSerializerTest : OurSerializationTest
    {
        [Test]
        public void IntReadTest()
        {
            var value = Serialization.Read<int>(new ValueDataNode("5"));

            Assert.That(value, Is.EqualTo(5));
        }

        [Test]
        public void NullableIntReadTest()
        {
            var nullValue = Serialization.Read<int?>(ValueDataNode.Null());

            Assert.That(nullValue, Is.Null);

            var value = Serialization.Read<int?>(new ValueDataNode("5"));

            Assert.That(value, Is.EqualTo(5));
        }
    }
}
