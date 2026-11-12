using Moq;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.IntegrationTests.GameObjects
{
    internal sealed class ServerEntityNetworkManagerTest
    {
        [Test]
        public void TestMessageSort()
        {
            var tickA = new GameTick(5);
            var tickB = new GameTick(3);
            var channel = new Mock<INetChannel>().Object;
            var msgA = new MsgEntity() {MsgChannel = channel, Type = EntityMessageType.SystemMessage, SourceTick = tickA, Sequence = 10};
            var msgB = new MsgEntity() {MsgChannel = channel, Type = EntityMessageType.SystemMessage, SourceTick = tickA, Sequence = 13};
            var msgC = new MsgEntity() {MsgChannel = channel, Type = EntityMessageType.SystemMessage, SourceTick = tickA, Sequence = 12};
            var msgD = new MsgEntity() {MsgChannel = channel, Type = EntityMessageType.SystemMessage, SourceTick = tickA, Sequence = 14};
            var msgE = new MsgEntity() {MsgChannel = channel, Type = EntityMessageType.SystemMessage, SourceTick = tickB, Sequence = 7};
            var msgF = new MsgEntity() {MsgChannel = channel, Type = EntityMessageType.SystemMessage, SourceTick = tickB, Sequence = 4};

            var pq = new PriorityQueue<(ulong, MsgEntity)>(new ServerEntityManager.MessageSequenceComparer())
            {
                (1, msgA),
                (2, msgB),
                (3, msgC),
                (4, msgD),
                (5, msgE),
                (6, msgF)
            };


            Assert.That(pq.Take().Item2, Is.EqualTo(msgF));
            Assert.That(pq.Take().Item2, Is.EqualTo(msgE));
            Assert.That(pq.Take().Item2, Is.EqualTo(msgA));
            Assert.That(pq.Take().Item2, Is.EqualTo(msgC));
            Assert.That(pq.Take().Item2, Is.EqualTo(msgB));
            Assert.That(pq.Take().Item2, Is.EqualTo(msgD));
        }

        /// <summary>
        /// Non-predicted messages share sequence 0, so same-tick ones must keep their arrival order.
        /// </summary>
        [Test]
        public void TestUnsequencedMessagesKeepArrivalOrder()
        {
            var channel = new Mock<INetChannel>().Object;
            var pq = new PriorityQueue<(ulong, MsgEntity)>(new ServerEntityManager.MessageSequenceComparer());
            var msgs = new MsgEntity[64];

            for (var i = 0; i < msgs.Length; i++)
            {
                msgs[i] = new MsgEntity {MsgChannel = channel, Type = EntityMessageType.SystemMessage, SourceTick = new GameTick(5)};
                pq.Add(((ulong) i + 1, msgs[i]));
            }

            foreach (var msg in msgs)
            {
                Assert.That(pq.Take().Item2, Is.EqualTo(msg));
            }
        }
    }
}
