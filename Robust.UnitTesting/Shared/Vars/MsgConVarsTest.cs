using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lidgren.Network;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Network.Messages;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Robust.UnitTesting.Shared.Vars;

public sealed class MsgConVarsTest : RobustIntegrationTest
{
    [Test]
    public async Task TestMsgConVarsSerialization_ServerToClient()
    {
        var server = StartServer();
        var client = StartClient();

        byte[] buffer = null!;

        await server.WaitPost(() =>
        {
            var msg = new MsgConVars
            {
                Tick = new GameTick(30),
                NetworkedVars = new List<(string name, object value)>
                {
                    ("intVar", 123),
                    ("longVar", 1234567890123L),
                    ("boolVar", true),
                    ("stringVar", "testString"),
                    ("floatVar", 3.14f),
                    ("doubleVar", 2.71828)
                }
            };

            var serializer = (RobustSerializer) IoCManager.Resolve<IRobustSerializer>();
            var netPeer = new NetPeer(new NetPeerConfiguration("test"));
            var outMsg = netPeer.CreateMessage();

            msg.WriteToBuffer(outMsg, serializer);

            buffer = new byte[outMsg.LengthBytes];
            outMsg.Data.AsSpan(0, outMsg.LengthBytes).CopyTo(buffer);
        });

        await client.WaitPost(() =>
        {
            var serializer = (RobustSerializer) IoCManager.Resolve<IRobustSerializer>();
            var inMsg = new NetIncomingMessage(NetIncomingMessageType.Data);
            inMsg.Write(buffer, 0, buffer.Length);
            inMsg.Position = 0;

            var receivedMsg = new MsgConVars();
            receivedMsg.ReadFromBuffer(inMsg, serializer);

            Assert.That(receivedMsg.NetworkedVars.Count, Is.EqualTo(6));

            foreach (var (name, value) in receivedMsg.NetworkedVars)
            {
                switch (name)
                {
                    case "intVar":
                        Assert.That(value, Is.EqualTo(123));
                        break;
                    case "longVar":
                        Assert.That(value, Is.EqualTo(1234567890123L));
                        break;
                    case "boolVar":
                        Assert.That(value, Is.EqualTo(true));
                        break;
                    case "stringVar":
                        Assert.That(value, Is.EqualTo("testString"));
                        break;
                    case "floatVar":
                        Assert.That((float)value, Is.EqualTo(3.14f).Within(0.0001f));
                        break;
                    case "doubleVar":
                        Assert.That((double)value, Is.EqualTo(2.71828).Within(0.0001));
                        break;
                    default:
                        Assert.Fail($"Unexpected variable name: {name}");
                        break;
                }
            }
        });
    }
}
