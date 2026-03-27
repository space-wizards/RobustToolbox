using Lidgren.Network;
using NUnit.Framework;
using Robust.Shared.Network;

namespace Robust.Shared.Tests.Networking;

public sealed class NetEncryptionDoSTest
{
    private const ulong Magic = 0x13377777_77777777;

    [Test]
    [Description("A control test that ensures connecting in a test works.")]
    public void ConnectionWorks()
    {
        var (client, server) = MakeConnectionPair();

        var message = client.CreateMessage();

        message.WriteVariableUInt64(Magic);

        client.SendMessage(message, NetDeliveryMethod.ReliableOrdered);

        var packet = Receive(server);

        Assert.That(packet, Is.Not.Null);

        Assert.That(packet.ReadVariableUInt64(), Is.EqualTo(Magic));
    }

    [Test]
    [Description("A control test that just ensures encryption works as other tests expect.")]
    public void EncryptionWorks()
    {
        var (clientEnc, serverEnc) = MakeEncryptionPair();
        var (client, server) = MakeConnectionPair();

        var message = client.CreateMessage();

        message.WriteVariableUInt64(Magic);

        clientEnc.Encrypt(message);

        client.SendMessage(message, NetDeliveryMethod.ReliableOrdered);

        var packet = Receive(server);

        Assert.That(packet, Is.Not.Null);

        Assert.That(serverEnc.TryDecrypt(packet), Is.True);
    }

    [Test]
    [Description("Attempt to decrypt a packet that is using the wrong encryption keys, ensuring it doesn't throw.")]
    public void WrongKeyFailureDoesNotThrow()
    {
        var (clientEnc, serverEnc) = MakeEncryptionPair(disjointKey: true);
        var (client, server) = MakeConnectionPair();

        var message = client.CreateMessage();

        message.WriteVariableUInt64(Magic);

        clientEnc.Encrypt(message);

        client.SendMessage(message, NetDeliveryMethod.ReliableOrdered);

        var packet = server.WaitMessage(1000);

        Assert.That(packet, Is.Not.Null);

        Assert.That(serverEnc.TryDecrypt(packet), Is.False);
    }

    [Test]
    [Description("Attempt to decrypt a packet that is bogus, ensuring it doesn't throw.")]
    public void BadMessageDoesNotThrow()
    {
        var (_, serverEnc) = MakeEncryptionPair(disjointKey: true);
        var (client, server) = MakeConnectionPair();

        var message = client.CreateMessage();

        message.WriteVariableUInt64(Magic);

        // Don't encrypt at all.

        client.SendMessage(message, NetDeliveryMethod.ReliableOrdered);

        var packet = server.WaitMessage(1000);

        Assert.That(packet, Is.Not.Null);

        Assert.That(serverEnc.TryDecrypt(packet), Is.False);
    }


    // TODO: Generalize all this for other low level network tests.

    private (NetClient client, NetServer server) MakeConnectionPair()
    {
        const string id = "test";
        var client = new NetClient(new NetPeerConfiguration(id));

        var server = new NetServer( new NetPeerConfiguration(id));

        client.Start();
        // Lidgren has no facilities for mocking this nicely.
        // So we just use an actual socket.
        server.Start();

        client.Connect("localhost", server.Port);

        var ready = false;

        while (!ready)
        {
            switch (server.WaitMessage(1000))
            {
                case { MessageType: NetIncomingMessageType.StatusChanged } msg:
                {
                    // hello there.
                    var status = (NetConnectionStatus)msg.ReadByte();

                    if (status == NetConnectionStatus.Connected)
                        ready = true;

                    break;
                }
            }
        }

        return (client, server);
    }

    private NetIncomingMessage Receive(NetPeer peer)
    {
        NetIncomingMessage? found = null;

        while (found == null)
        {
            switch (peer.WaitMessage(1000))
            {
                case { MessageType: NetIncomingMessageType.Data } msg:
                {
                    found = msg;
                    break;
                }
            }
        }

        return found;
    }

    private (NetEncryption client, NetEncryption server) MakeEncryptionPair(bool disjointKey = false)
    {
        var serverKey = new byte[32];

        System.Random.Shared.NextBytes(serverKey.AsSpan());

        var clientKey = (byte[])serverKey.Clone();

        if (disjointKey)
            System.Random.Shared.NextBytes(clientKey.AsSpan());

        return (new NetEncryption(clientKey, false), new NetEncryption(serverKey, true));
    }
}
