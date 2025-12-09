using System;
using System.Collections;
using System.IO;
using System.Text;
using NetSerializer;
using NUnit.Framework;
using Robust.Shared.Serialization;

namespace Robust.UnitTesting.Shared.Serialization;

[Parallelizable(ParallelScope.All)]
[TestFixture, TestOf(typeof(NetBitArraySerializer))]
internal sealed class NetSerializerBitArrayTest
{
    // Test that BitArray serialization matches the behavior before .NET 10
    // This test can be removed in future RT versions.

    [Test]
    [TestCase("little creature, have you ever heard about gay people?",
        "AgAP2KWjxw7YlYOyDOSVi8YO6smrxgXAoIvmDsqByfcN6oGp5g7KyYOCDcqFk8cMwIST9g3q0YPyDMLlg4IOyr2Dxw3K/QHgBg==")]
    [TestCase("", "AgABAA==")]
    public void Test(string testData, string expected)
    {
        var bitData = Encoding.UTF8.GetBytes(testData);
        var bitArray = new BitArray(bitData);

        var serializer = new Serializer([typeof(BitArray)],
            new Settings
            {
                CustomTypeSerializers = [new NetBitArraySerializer()]
            });

        var stream = new MemoryStream();
        serializer.Serialize(stream, bitArray);

        var base64 = Convert.ToBase64String(stream.ToArray());

        TestContext.Out.WriteLine(base64);

        Assert.That(base64, Is.EqualTo(expected));

        stream.Position = 0;
        var newBitArray = (BitArray)serializer.Deserialize(stream);

        Assert.That(newBitArray, Is.EquivalentTo(bitArray));
    }

    [Test]
    public void Unset()
    {
        var bitArray = new BitArray(16);

        var serializer = new Serializer([typeof(BitArray)],
            new Settings
            {
                CustomTypeSerializers = [new NetBitArraySerializer()]
            });

        var stream = new MemoryStream();
        serializer.Serialize(stream, bitArray);

        var base64 = Convert.ToBase64String(stream.ToArray());

        TestContext.Out.WriteLine(base64);

        Assert.That(base64, Is.EqualTo("AgACACA="));

        stream.Position = 0;
        var newBitArray = (BitArray)serializer.Deserialize(stream);

        Assert.That(newBitArray, Is.EquivalentTo(bitArray));
    }

    [Test]
    public void TestClass()
    {
        var obj = new FooBar
        {
            Real = 0x3005,
            Heck = "omg",
            Wawa = new BitArray("I miss my wife"u8.ToArray())
        };

        var serializer = new Serializer([typeof(BitArray), typeof(FooBar)],
            new Settings
            {
                CustomTypeSerializers = [new NetBitArraySerializer()]
            });

        var stream = new MemoryStream();
        serializer.Serialize(stream, obj);

        var base64 = Convert.ToBase64String(stream.ToArray());

        TestContext.Out.WriteLine(base64);

        Assert.That(base64, Is.EqualTo("AgQDb21nisABAwAFkoHplg3mzYPSDfKBuZcNzJUD4AE="));

        stream.Position = 0;
        var newObject = (FooBar)serializer.Deserialize(stream);

        Assert.Multiple(() =>
        {
            Assert.That(newObject.Real, Is.EqualTo(obj.Real));
            Assert.That(newObject.Heck, Is.EqualTo(obj.Heck));
            Assert.That(newObject.Wawa, Is.EquivalentTo(obj.Wawa));
        });
    }

    [Serializable]
    private sealed class FooBar
    {
        public required int Real;
        public required string Heck;
        public required BitArray Wawa;
    }
}
