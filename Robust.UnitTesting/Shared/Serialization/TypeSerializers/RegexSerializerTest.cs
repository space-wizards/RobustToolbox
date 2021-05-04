using System.Text.RegularExpressions;
using NUnit.Framework;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers
{
    [TestFixture]
    [TestOf(typeof(RegexSerializer))]
    public class RegexSerializerTest : SerializationTest
    {
        [Test]
        public void SerializationTest()
        {
            var str = "[AEIOU]";
            var regex = new Regex(str);
            var node = Serialization.WriteValueAs<ValueDataNode>(regex);

            Assert.That(node.Value, Is.EqualTo(str));
        }

        [Test]
        public void DeserializationTest()
        {
            var str = "[AEIOU]";
            var node = new ValueDataNode(str);
            var deserializedRegex = Serialization.ReadValueOrThrow<Regex>(node);
            var regex = new Regex(str, RegexOptions.Compiled);

            Assert.That(deserializedRegex.ToString(), Is.EqualTo(regex.ToString()));
            Assert.That(deserializedRegex.Options, Is.EqualTo(regex.Options));
        }
    }
}
