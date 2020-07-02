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
            Assert.IsFalse(boolean);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse

            var value = inc.ReadInt32(6);
            Assert.AreEqual(-3, value);

            boolean = inc.ReadBoolean();
            Assert.IsTrue(boolean);

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
            Assert.AreEqual(7, c);

            msg.WritePadBits();

            var data = msg.Buffer;

            var inc = CreateIncomingMessage(data, msg.LengthBits);

            var boolean = inc.ReadBoolean();
            Assert.IsFalse(boolean);

            var value = inc.ReadInt32(6);
            Assert.AreEqual(-3, value);

            value = inc.ReadInt32();
            Assert.AreEqual(42, value);

            var ok = inc.ReadString(out var strResult);
            Assert.IsTrue(ok, "Read/write failure");
            Assert.AreEqual("duke of earl", strResult);

            var byteVal = inc.ReadByte();
            Assert.AreEqual(43,byteVal);

            Assert.AreEqual((ushort) 44, inc.ReadUInt16(), "Read/write failure");

            Assert.AreEqual(UInt64.MaxValue, inc.ReadUInt64(64), "Read/write failure");

            var longVal = inc.ReadVariableInt64();
            Assert.AreEqual(long.MaxValue >> 16, longVal);

            boolean = inc.ReadBoolean();
            Assert.IsTrue(boolean);

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

            Assert.AreEqual(2, bcnt, "WriteVariable* wrote too many bytes!");

            var data = msg.Buffer;

            var inc = CreateIncomingMessage(data, msg.LengthBits);

            var boolean = inc.ReadBoolean();
            Assert.IsFalse(boolean);
            var value = inc.ReadInt32(6);
            Assert.AreEqual(-3, value);
            value = inc.ReadInt32();
            Assert.AreEqual(42, value);

            var ok = inc.ReadString(out var strResult);
            Assert.IsTrue(ok, "Read/write failure");
            Assert.AreEqual("duke of earl", strResult);

            var byteVal = inc.ReadByte();
            Assert.AreEqual(43, byteVal);

            Assert.AreEqual((ushort) 44, inc.ReadUInt16(), "Read/write failure");

            Assert.AreEqual(UInt64.MaxValue, inc.ReadUInt64(64), "Read/write failure");

            boolean = inc.ReadBoolean();
            Assert.IsTrue(boolean);

            inc.SkipPadBits();

            Assert.AreEqual(567845.0f, inc.ReadSingle());
            Assert.AreEqual(2115998022, inc.ReadVariableInt32());
            Assert.AreEqual(46.0, inc.ReadDouble());
            Assert.AreEqual(14, inc.ReadUInt32(9));
            Assert.AreEqual(-47, inc.ReadVariableInt32());
            Assert.AreEqual(470000, inc.ReadVariableInt32());
            Assert.AreEqual(48, inc.ReadVariableUInt32());
            Assert.AreEqual(-49, inc.ReadVariableInt64());
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

            Assert.AreEqual(tmp.LengthBits * 2, msg.LengthBits, "NetOutgoingMessage.Write(NetOutgoingMessage) failed!");

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

            Assert.AreEqual(42, readTestItem.Number);
            Assert.AreEqual("Hallon", readTestItem.Name);
            Assert.AreEqual(8.2f, readTestItem.Age);

            // test aligned WriteBytes/ReadBytes
            msg = peer.CreateMessage();
            var tmparr = new byte[] {5, 6, 7, 8, 9};
            msg.Write(tmparr);

            inc = CreateIncomingMessage(msg.Buffer, msg.LengthBits);
            var result = inc.ReadBytes(stackalloc byte[tmparr.Length]);

            for (var i = 0; i < tmparr.Length; i++)
            {
                Assert.AreEqual(result[i], tmparr[i], "readbytes fail");
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
