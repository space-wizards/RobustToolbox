using NUnit.Framework;
using Robust.Shared.Serialization.Markdown.Value;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.Serialization
{
    [TestFixture]
    public class NullableSerializationTest : SerializationTest
    {
        [Test]
        public void NullableIntTest()
        {
            var nullValue = Serialization.Read(typeof(int?), new ValueDataNode("null"));

            Assert.Null(nullValue.RawValue);

            var value = Serialization.Read(typeof(int?), new ValueDataNode("5"));

            Assert.That(value.RawValue, Is.EqualTo(5));
        }
    }
}
