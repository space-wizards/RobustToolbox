using NUnit.Framework;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers
{
    [TestFixture]
    public sealed class IntegerSerializerTest : SerializationTest
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
