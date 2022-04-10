using NUnit.Framework;
using Robust.Shared.Serialization.Markdown.Value;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers
{
    [TestFixture]
    public sealed class IntegerSerializerTest : SerializationTest
    {
        [Test]
        public void IntReadTest()
        {
            var value = Serialization.Read<int>(new ValueDataNode("5"));

            Assert.NotNull(value);
            Assert.That(value, Is.EqualTo(5));
        }

        [Test]
        public void NullableIntReadTest()
        {
            var nullValue = Serialization.Read<int?>(new ValueDataNode("null"));

            Assert.Null(nullValue);

            var value = Serialization.Read<int?>(new ValueDataNode("5"));

            Assert.That(value, Is.EqualTo(5));
        }
    }
}
