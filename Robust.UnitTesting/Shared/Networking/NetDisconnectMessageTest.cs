using NUnit.Framework;
using Robust.Shared.Network;

namespace Robust.UnitTesting.Shared.Networking;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[TestOf(typeof(NetDisconnectMessage))]
internal sealed class NetDisconnectMessageTest
{
    [Test]
    [TestCase("Disconnected: bye", "Disconnected: bye", NetDisconnectMessage.DefaultRedialFlag)]
    [TestCase("Disconnected: {\"reason\": \"bye\"}", "bye", NetDisconnectMessage.DefaultRedialFlag)]
    [TestCase("Disconnected: {}", NetDisconnectMessage.DefaultReason, NetDisconnectMessage.DefaultRedialFlag)]
    [TestCase("Disconnected: {\"redial\": true}", NetDisconnectMessage.DefaultReason, true)]
    [TestCase("Disconnected: {\"redial\": true, \"foobar\": 5}", NetDisconnectMessage.DefaultReason, true)]
    [TestCase("Disconnected: {\"redial\": true, \"foobar\": 5, \"reason\": \"asdf\"}", "asdf", true)]
    [TestCase("Disconnected: {", "Disconnected: {", NetDisconnectMessage.DefaultRedialFlag)]
    [TestCase("Disconnected: {\"a\":[]}", NetDisconnectMessage.DefaultReason, NetDisconnectMessage.DefaultRedialFlag)]
    [TestCase("{\"redial\": true, \"foobar\": 5, \"reason\": \"asdf\"}", "asdf", true)]
    public void TestBasicDecode(string encoded, string reasonExpected, bool redialExpected)
    {
        var parsed = NetDisconnectMessage.Decode(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Reason, Is.EqualTo(reasonExpected));
            Assert.That(parsed.RedialFlag, Is.EqualTo(redialExpected));
        });
    }

    [Test]
    public void TestEncode()
    {
        var value = new NetDisconnectMessage("foobar", true)
        {
            Values =
            {
                ["asdf"] = 20.5f
            }
        };

        var encoded = value.Encode();
        TestContext.Write($"Encoded: {encoded}\n");
        var decodedAgain = NetDisconnectMessage.Decode(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(decodedAgain.Reason, Is.EqualTo("foobar"));
            Assert.That(decodedAgain.RedialFlag, Is.EqualTo(true));
            Assert.That(decodedAgain.SingleOf("asdf"), Is.EqualTo(20.5f));
        });
    }

    [Test]
    public void TestDefaultConstructor()
    {
        var value = new NetDisconnectMessage("foobar");

        Assert.Multiple(() =>
        {
            Assert.That(value.Reason, Is.EqualTo("foobar"));
            Assert.That(value.RedialFlag, Is.EqualTo(false));
        });
    }

    [Test]
    public void TestValueOfInt()
    {
        var parsed = NetDisconnectMessage.Decode("{\"foobar\": 5}");

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Int32Of("foobar"), Is.EqualTo(5));
            Assert.That(parsed.Int32Of("asdf"), Is.Null);
            Assert.That(parsed.Int32Of("asdf", 7), Is.EqualTo(7));
        });
    }

    [Test]
    public void TestValueOfFloat()
    {
        var parsed = NetDisconnectMessage.Decode("{\"foobar\": 5.5}");

        Assert.Multiple(() =>
        {
            Assert.That(parsed.SingleOf("foobar"), Is.EqualTo(5.5f));
            Assert.That(parsed.SingleOf("asdf"), Is.Null);
            Assert.That(parsed.SingleOf("asdf", 7), Is.EqualTo(7f));
        });
    }

    [Test]
    public void TestValueOfFloatInt()
    {
        var parsed = NetDisconnectMessage.Decode("{\"foobar\": 5}");

        Assert.That(parsed.SingleOf("foobar"), Is.EqualTo(5f));
    }

    [Test]
    public void TestValueOfString()
    {
        var parsed = NetDisconnectMessage.Decode("{\"foobar\": \"real\"}");

        Assert.Multiple(() =>
        {
            Assert.That(parsed.StringOf("foobar"), Is.EqualTo("real"));
            Assert.That(parsed.StringOf("asdf"), Is.Null);
            Assert.That(parsed.StringOf("asdf", "honk"), Is.EqualTo("honk"));
        });
    }
}
