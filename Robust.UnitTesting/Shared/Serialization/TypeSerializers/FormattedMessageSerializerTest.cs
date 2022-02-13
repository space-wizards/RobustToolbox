using NUnit.Framework;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers;

[TestFixture]
[TestOf(typeof(FormattedMessageSerializer))]
public sealed class FormattedMessageSerializerTest : SerializationTest
{
    [Test]
    [TestCase("message")]
    [TestCase("[color=#FF0000FF]message[/color]")]
    public void SerializationTest(string text)
    {
        var message = FormattedMessage.FromMarkup(text);
        var node = Serialization.WriteValueAs<ValueDataNode>(message);
        Assert.That(node.Value, Is.EqualTo(text));
    }

    [Test]
    [TestCase("message")]
    [TestCase("[color=#FF0000FF]message[/color]")]
    public void DeserializationTest(string text)
    {
        var node = new ValueDataNode(text);
        var deserializedMessage = Serialization.ReadValueOrThrow<FormattedMessage>(node);
        Assert.That(deserializedMessage.ToMarkup(), Is.EqualTo(text));
    }
}
