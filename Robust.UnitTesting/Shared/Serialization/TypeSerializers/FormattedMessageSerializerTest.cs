using NUnit.Framework;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers;

[TestFixture]
[TestOf(typeof(FormattedMessageSerializer))]
public sealed class FormattedMessageSerializerTest : SerializationTest
{
    [Test]
    [TestCase("message")]
    [TestCase("[color=red]message[/color]")]
    public void SerializationTest(string text)
    {
        var message = FormattedMessage.FromMarkupOrThrow(text);
        var node = Serialization.WriteValueAs<ValueDataNode>(message);
        Assert.That(node.Value, Is.EqualTo(text));
    }

    [Test]
    [TestCase("message")]
    [TestCase("[color=red]message[/color]")]
    public void DeserializationTest(string text)
    {
        var node = new ValueDataNode(text);
        var deserializedMessage = Serialization.Read<FormattedMessage>(node, notNullableOverride: true);
        Assert.That(deserializedMessage.ToMarkup(), Is.EqualTo(text));
    }
}
