using Moq;
using NUnit.Framework;
using Robust.Client.GameStates;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Map;
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

            var pq = new PriorityQueue<MsgEntity>(new ServerEntityManager.MessageSequenceComparer())
            {
                msgA,
                msgB,
                msgC,
                msgD,
                msgE,
                msgF
            };


            Assert.That(pq.Take(), Is.EqualTo(msgF));
            Assert.That(pq.Take(), Is.EqualTo(msgE));
            Assert.That(pq.Take(), Is.EqualTo(msgA));
            Assert.That(pq.Take(), Is.EqualTo(msgC));
            Assert.That(pq.Take(), Is.EqualTo(msgB));
            Assert.That(pq.Take(), Is.EqualTo(msgD));
        }

        [Test]
        public void TestSequencedMessagesDoNotMoveBackwardsInTime()
        {
            var state = new ServerEntityManager.SequencedMessageState();
            var channel = new Mock<INetChannel>().Object;
            var function = new KeyFunctionId(0);
            var down = new MsgEntity()
            {
                MsgChannel = channel,
                Type = EntityMessageType.SystemMessage,
                SystemMessage = new FullInputCmdMessage(
                    new GameTick(120),
                    0,
                    function,
                    BoundKeyState.Down,
                    NetCoordinates.Invalid,
                    ScreenCoordinates.Invalid,
                    NetEntity.Invalid),
                SourceTick = new GameTick(120),
                Sequence = 100,
            };
            var up = new MsgEntity()
            {
                MsgChannel = channel,
                Type = EntityMessageType.SystemMessage,
                SystemMessage = new FullInputCmdMessage(
                    new GameTick(115),
                    0,
                    function,
                    BoundKeyState.Up,
                    NetCoordinates.Invalid,
                    ScreenCoordinates.Invalid,
                    NetEntity.Invalid),
                SourceTick = new GameTick(115),
                Sequence = 101,
            };

            Assert.That(state.Queue(down), Is.True);
            Assert.That(state.Queue(up), Is.True);
            Assert.That(up.SourceTick, Is.EqualTo(down.SourceTick));

            var pq = new PriorityQueue<MsgEntity>(new ServerEntityManager.MessageSequenceComparer())
            {
                down,
                up,
            };

            Assert.That(pq.Take(), Is.EqualTo(down));
            Assert.That(pq.Take(), Is.EqualTo(up));
        }

        [Test]
        public void TestSequencedMessagesRejectStaleSequences()
        {
            var state = new ServerEntityManager.SequencedMessageState();

            var first = new MsgEntity() {SourceTick = new GameTick(120), Sequence = 100};
            var duplicate = new MsgEntity() {SourceTick = new GameTick(121), Sequence = 100};
            var alreadyProcessed = new MsgEntity() {SourceTick = new GameTick(122), Sequence = 99};

            Assert.That(state.Queue(first), Is.True);
            Assert.That(state.Queue(duplicate), Is.False);

            var processedState = new ServerEntityManager.SequencedMessageState();
            Assert.That(processedState.MarkProcessed(100), Is.True);
            Assert.That(processedState.Queue(alreadyProcessed), Is.False);
        }
    }
}
