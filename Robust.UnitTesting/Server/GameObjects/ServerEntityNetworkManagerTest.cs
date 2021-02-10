using Moq;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Server.GameObjects
{
    public class ServerEntityNetworkManagerTest
    {
        [Test]
        public void TestMessageSort()
        {
            var tickA = new GameTick(5);
            var tickB = new GameTick(3);
            var channel = new Mock<INetChannel>().Object;
            var msgA = new MsgEntity(channel) {Type = EntityMessageType.SystemMessage, SourceTick = tickA, Sequence = 10};
            var msgB = new MsgEntity(channel) {Type = EntityMessageType.SystemMessage, SourceTick = tickA, Sequence = 13};
            var msgC = new MsgEntity(channel) {Type = EntityMessageType.SystemMessage, SourceTick = tickA, Sequence = 12};
            var msgD = new MsgEntity(channel) {Type = EntityMessageType.SystemMessage, SourceTick = tickA, Sequence = 14};
            var msgE = new MsgEntity(channel) {Type = EntityMessageType.SystemMessage, SourceTick = tickB, Sequence = 7};
            var msgF = new MsgEntity(channel) {Type = EntityMessageType.SystemMessage, SourceTick = tickB, Sequence = 4};

            var pq = new PriorityQueue<MsgEntity>(new ServerEntityNetworkManager.MessageSequenceComparer());

            pq.Add(msgA);
            pq.Add(msgB);
            pq.Add(msgC);
            pq.Add(msgD);
            pq.Add(msgE);
            pq.Add(msgF);

            Assert.That(pq.Take(), Is.EqualTo(msgF));
            Assert.That(pq.Take(), Is.EqualTo(msgE));
            Assert.That(pq.Take(), Is.EqualTo(msgA));
            Assert.That(pq.Take(), Is.EqualTo(msgC));
            Assert.That(pq.Take(), Is.EqualTo(msgB));
            Assert.That(pq.Take(), Is.EqualTo(msgD));
        }
    }
}
