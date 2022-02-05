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
            var value = Serialization.Read(typeof(int), new ValueDataNode("5"));

            Assert.NotNull(value.RawValue);
            Assert.That(value.RawValue, Is.EqualTo(5));
        }

        [Test]
        public void NullableIntReadTest()
        {
            var nullValue = Serialization.Read(typeof(int?), new ValueDataNode("null"));

            Assert.Null(nullValue.RawValue);

            var value = Serialization.Read(typeof(int?), new ValueDataNode("5"));

            Assert.That(value.RawValue, Is.EqualTo(5));
        }
    }
}
