using NetSerializer;
using NUnit.Framework;
using Robust.Shared.RichText;
using Robust.Shared.Serialization;

namespace Robust.Shared.Tests.Serialization;

[Parallelizable(ParallelScope.All)]
[TestFixture, TestOf(typeof(NetFormattedStringSerializer))]
internal sealed class NetSerializerFormattedStringTest
{
    [Test]
    [TestCase("")]
    [TestCase("real")]
    [TestCase("[i]heck[/i]")]
    public void TestBasic(string markup)
    {
        var serializer = MakeSerializer();

        var str = FormattedString.FromMarkup(markup);

        var stream = new MemoryStream();
        serializer.Serialize(stream, str);
        stream.Position = 0;

        var deserialized = (FormattedString) serializer.Deserialize(stream);

        Assert.That(deserialized, NUnit.Framework.Is.EqualTo(str));
    }

    /// <summary>
    /// Test that the on-wire representation of a <see cref="FormattedString"/> is the same as a regular string.
    /// This is to ensure <see cref="TestInvalid"/> is a valid test.
    /// </summary>
    [Test]
    [TestCase("")]
    [TestCase("real")]
    [TestCase("[i]heck[/i]")]
    public void TestEqualToString(string markup)
    {
        var serializer = MakeSerializer();

        var stream = new MemoryStream();
        serializer.SerializeDirect(stream, markup);
        stream.Position = 0;

        serializer.DeserializeDirect(stream, out FormattedString str);

        Assert.That(str.Markup, Is.EqualTo(markup));
    }

    /// <summary>
    /// Test that deserialization fails if a malicious client sends broken markup.
    /// </summary>
    [Test]
    public void TestInvalid()
    {
        var serializer = MakeSerializer();

        var stream = new MemoryStream();
        serializer.SerializeDirect(stream, "[wahoooo");
        stream.Position = 0;

        Assert.That(() => serializer.DeserializeDirect(stream, out FormattedString _), Throws.Exception);
    }

    private static Serializer MakeSerializer()
    {
        return new Serializer([typeof(FormattedString)],
            new Settings
            {
                CustomTypeSerializers = [new NetFormattedStringSerializer()]
            });
    }
}
