using System;
using System.Text;
using NUnit.Framework;
using static System.Reflection.BindingFlags;

namespace Lidgren.Network.UnitTests
{

    public class ReadWriteTests : TestsBase
    {

        [Test]
        public void TestSmallMessage1()
        {
            var peer = Peer;
            var msg = peer.CreateMessage();

            msg.Write(false);
            msg.Write(-3, 6);
            msg.Write(true);

            msg.WritePadBits();

            var data = msg.Buffer;

            var inc = CreateIncomingMessage(data, msg.LengthBits);

            var boolean = inc.ReadBoolean();
            Assert.That(boolean, Is.False);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse

            var value = inc.ReadInt32(6);
            Assert.That(value, Is.EqualTo(-3));

            boolean = inc.ReadBoolean();
            Assert.That(boolean, Is.True);

            inc.SkipPadBits();
        }

        [Test]
        public void TestSmallMessage2()
        {
            var peer = Peer;
            var msg = peer.CreateMessage();

            msg.Write(false);
            msg.Write(-3, 6);
            msg.Write(42);
            msg.Write("duke of earl");
            msg.Write((byte) 43);
            msg.Write((ushort) 44);
            msg.Write(UInt64.MaxValue, 64);
            var c = msg.WriteVariableInt64(long.MaxValue >> 16);
            msg.Write(true);
            Assert.That(c, Is.EqualTo(7));

            msg.WritePadBits();

            var data = msg.Buffer;

            var inc = CreateIncomingMessage(data, msg.LengthBits);

            var boolean = inc.ReadBoolean();
            Assert.That(boolean, Is.False);

            var value = inc.ReadInt32(6);
            Assert.That(value, Is.EqualTo(-3));

            value = inc.ReadInt32();
            Assert.That(value, Is.EqualTo(42));

            var ok = inc.ReadString(out var strResult);
            Assert.That(ok, Is.True, "Read/write failure");
            Assert.That(strResult, Is.EqualTo("duke of earl"));

            var byteVal = inc.ReadByte();
            Assert.That(byteVal, Is.EqualTo(43));

            Assert.That(inc.ReadUInt16(), Is.EqualTo((ushort) 44), "Read/write failure");

            Assert.That(inc.ReadUInt64(64), Is.EqualTo(UInt64.MaxValue), "Read/write failure");

            var longVal = inc.ReadVariableInt64();
            Assert.That(longVal, Is.EqualTo(long.MaxValue >> 16));

            boolean = inc.ReadBoolean();
            Assert.That(boolean, Is.True);

            inc.SkipPadBits();
        }

        [Test]
        public void TestComplexMessage()
        {
            var peer = Peer;
            var msg = peer.CreateMessage();

            msg.Write(false);
            msg.Write(-3, 6);
            msg.Write(42);
            msg.Write("duke of earl");
            msg.Write((byte) 43);
            msg.Write((ushort) 44);
            msg.Write(UInt64.MaxValue, 64);
            msg.Write(true);

            msg.WritePadBits();

            var bcnt = 0;

            msg.Write(567845.0f);
            msg.WriteVariableInt32(2115998022);
            msg.Write(46.0);
            msg.Write((ushort) 14, 9);
            bcnt += msg.WriteVariableInt32(-47);
            msg.WriteVariableInt32(470000);
            msg.WriteVariableUInt32(48);
            bcnt += msg.WriteVariableInt64(-49);

            Assert.That(bcnt, Is.EqualTo(2), "WriteVariable* wrote too many bytes!");

            var data = msg.Buffer;

            var inc = CreateIncomingMessage(data, msg.LengthBits);

            var boolean = inc.ReadBoolean();
            Assert.That(boolean, Is.False);
            var value = inc.ReadInt32(6);
            Assert.That(value, Is.EqualTo(-3));
            value = inc.ReadInt32();
            Assert.That(value, Is.EqualTo(42));

            var ok = inc.ReadString(out var strResult);
            Assert.That(ok, Is.True, "Read/write failure");
            Assert.That(strResult, Is.EqualTo("duke of earl"));

            var byteVal = inc.ReadByte();
            Assert.That(byteVal, Is.EqualTo(43));

            Assert.That(inc.ReadUInt16(), Is.EqualTo((ushort) 44), "Read/write failure");

            Assert.That(inc.ReadUInt64(64), Is.EqualTo(UInt64.MaxValue), "Read/write failure");

            boolean = inc.ReadBoolean();
            Assert.That(boolean, Is.True);

            inc.SkipPadBits();

            Assert.That(inc.ReadSingle(), Is.EqualTo(567845.0f));
            Assert.That(inc.ReadVariableInt32(), Is.EqualTo(2115998022));
            Assert.That(inc.ReadDouble(), Is.EqualTo(46.0));
            Assert.That(inc.ReadUInt32(9), Is.EqualTo(14));
            Assert.That(inc.ReadVariableInt32(), Is.EqualTo(-47));
            Assert.That(inc.ReadVariableInt32(), Is.EqualTo(470000));
            Assert.That(inc.ReadVariableUInt32(), Is.EqualTo(48));
            Assert.That(inc.ReadVariableInt64(), Is.EqualTo(-49));
        }

        [Test]
        public void TestWriteAllFields()
        {
            var peer = Peer;

            var msg = peer.CreateMessage();

            var tmp = peer.CreateMessage();
            tmp.Write((int) 42, 14);

            msg.Write(tmp);
            msg.Write(tmp);

            Assert.That(msg.LengthBits, Is.EqualTo(tmp.LengthBits * 2), "NetOutgoingMessage.Write(NetOutgoingMessage) failed!");

            tmp = peer.CreateMessage();

            var testItem = new TestItem
            {
                Number = 42,
                Name = "Hallon",
                Age = 8.2f
            };

            tmp.WriteAllFields(testItem, Public | Instance);

            var data = tmp.Buffer;

            var inc = CreateIncomingMessage(data, tmp.LengthBits);

            var readTestItem = new TestItem();
            inc.ReadAllFields(readTestItem, Public | Instance);

            Assert.That(readTestItem.Number, Is.EqualTo(42));
            Assert.That(readTestItem.Name, Is.EqualTo("Hallon"));
            Assert.That(readTestItem.Age, Is.EqualTo(8.2f));

            // test aligned WriteBytes/ReadBytes
            msg = peer.CreateMessage();
            var tmparr = new byte[] {5, 6, 7, 8, 9};
            msg.Write(tmparr);

            inc = CreateIncomingMessage(msg.Buffer, msg.LengthBits);
            var result = inc.ReadBytes(stackalloc byte[tmparr.Length]);

            for (var i = 0; i < tmparr.Length; i++)
            {
                Assert.That(tmparr[i], Is.EqualTo(result[i]), "readbytes fail");
            }
        }

        public class TestItemBase
        {

            public int Number;

        }

        public class TestItem : TestItemBase
        {

            public float Age;

            public string Name;

        }

    }

}
