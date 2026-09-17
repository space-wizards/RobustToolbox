using System.Diagnostics;
using Lidgren.Network;
using NUnit.Framework;
using Robust.Shared.Network;

namespace Robust.Shared.Tests.Networking;

public sealed class NetEncryptionDoSTest
{
    private const ulong Magic = 0x13377777_77777777;
    private static readonly TimeSpan MessageTimeout = TimeSpan.FromSeconds(10);
    private readonly List<NetPeer> _peers = new();

    [TearDown]
    public void TearDown()
    {
        foreach (var peer in _peers)
            peer.Shutdown(null);

        _peers.Clear();
    }

    [Test]
    [Description("A control test that ensures connecting in a test works.")]
    public void ConnectionWorks()
    {
        var (client, server) = MakeConnectionPair();

        var message = client.CreateMessage();

        message.WriteVariableUInt64(Magic);

        Assert.That(client.SendMessage(message, NetDeliveryMethod.ReliableOrdered),
            Is.EqualTo(NetSendResult.Sent).Or.EqualTo(NetSendResult.Queued));

        var packet = Receive(server);

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

        Assert.That(client.SendMessage(message, NetDeliveryMethod.ReliableOrdered),
            Is.EqualTo(NetSendResult.Sent).Or.EqualTo(NetSendResult.Queued));

        var packet = Receive(server);

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

        Assert.That(client.SendMessage(message, NetDeliveryMethod.ReliableOrdered),
            Is.EqualTo(NetSendResult.Sent).Or.EqualTo(NetSendResult.Queued));

        var packet = Receive(server);

        Assert.That(serverEnc.TryDecrypt(packet), Is.False);
    }

    private static int[] _badMessages =
    [
        5,
        1,
        4,
        16,
        1024,
    ];

    [Test]
    [Description("Attempt to decrypt a packet that is bogus, ensuring it doesn't throw.")]
    [TestCaseSource(nameof(_badMessages))]
    public void BadMessageDoesNotThrow(int badMessageLength)
    {
        var badMessage = new byte[badMessageLength];
        System.Random.Shared.NextBytes(badMessage);
        var (_, serverEnc) = MakeEncryptionPair(disjointKey: true);
        var (client, server) = MakeConnectionPair();

        var message = client.CreateMessage();

        message.Write(badMessage);

        // Don't encrypt at all.

        Assert.That(client.SendMessage(message, NetDeliveryMethod.ReliableOrdered),
            Is.EqualTo(NetSendResult.Sent).Or.EqualTo(NetSendResult.Queued));

        var packet = Receive(server);

        Assert.That(packet.LengthBytes, Is.EqualTo(badMessageLength));

        Assert.That(serverEnc.TryDecrypt(packet), Is.False);
    }


    // TODO: Generalize all this for other low level network tests.

    private (NetClient client, NetServer server) MakeConnectionPair()
    {
        const string id = "test";
        var client = new NetClient(new NetPeerConfiguration(id));

        var server = new NetServer(new NetPeerConfiguration(id));
        _peers.Add(client);
        _peers.Add(server);

        client.Start();
        // Lidgren has no facilities for mocking this nicely.
        // So we just use an actual socket.
        server.Start();

        client.Connect("localhost", server.Port);

        // The server can finish its handshake before the client updates its own status.
        // Wait for both notifications before attempting to send any data.
        client.Recycle(Receive(client, NetIncomingMessageType.StatusChanged));
        server.Recycle(Receive(server, NetIncomingMessageType.StatusChanged));

        return (client, server);
    }

    private NetIncomingMessage Receive(NetPeer peer, NetIncomingMessageType messageType = NetIncomingMessageType.Data)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < MessageTimeout)
        {
            if (peer.WaitMessage(100) is not { } message)
                continue;

            if (message.MessageType == NetIncomingMessageType.StatusChanged)
            {
                var status = (NetConnectionStatus) message.ReadByte();
                var reason = message.ReadString();
                Assert.That(status, Is.Not.EqualTo(NetConnectionStatus.Disconnected),
                    $"{peer.GetType().Name} disconnected while waiting for {messageType}: {reason}");

                if (status != NetConnectionStatus.Connected)
                {
                    peer.Recycle(message);
                    continue;
                }
            }

            if (message.MessageType == messageType)
                return message;

            peer.Recycle(message);
        }

        Assert.Fail($"{peer.GetType().Name} timed out after {MessageTimeout} waiting for {messageType}.");
        return null!;
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
