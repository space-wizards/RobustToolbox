using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Robust.Shared.GameObjects;

namespace Robust.UnitTesting.Shared.GameObjects
{
    [TestFixture, Parallelizable, TestOf(typeof(EntityEventBus))]
    public partial class EntityEventBusTests
    {
        private static EntityEventBus BusFactory()
        {
            var entityMan = new Mock<IEntityManager>();
            var compMan = new Mock<IComponentManager>();
            entityMan.Setup(m => m.ComponentManager).Returns(compMan.Object);
            var bus = new EntityEventBus(entityMan.Object);
            return bus;
        }

        /// <summary>
        /// Trying to subscribe a null handler causes a <see cref="ArgumentNullException"/> to be thrown.
        /// </summary>
        [Test]
        public void SubscribeEvent_NullHandler_NullArgumentException()
        {
            // Arrange
            var bus = BusFactory();
            var subscriber = new TestEventSubscriber();

            // Act
            void Code() => bus.SubscribeEvent(EventSource.Local, subscriber, (EntityEventHandler<TestEventArgs>) null!);

            //Assert
            Assert.Throws<ArgumentNullException>(Code);
        }

        /// <summary>
        /// Trying to subscribe with a null subscriber causes a <see cref="ArgumentNullException"/> to be thrown.
        /// </summary>
        [Test]
        public void SubscribeEvent_NullSubscriber_NullArgumentException()
        {
            // Arrange
            var bus = BusFactory();

            // Act
            void Code() => bus.SubscribeEvent<TestEventArgs>(EventSource.Local, null!, ev => {});

            //Assert: this should do nothing
            Assert.Throws<ArgumentNullException>(Code);
        }

        /// <summary>
        /// Unlike C# events, the set of event handler delegates is unique.
        /// Subscribing the same delegate multiple times will only call the handler once.
        /// </summary>
        [Test]
        public void SubscribeEvent_DuplicateSubscription_RaisedOnce()
        {
            // Arrange
            var bus = BusFactory();
            var subscriber = new TestEventSubscriber();

            int delegateCallCount = 0;
            void Handler(TestEventArgs ev) => delegateCallCount++;

            // 2 subscriptions 1 handler
            bus.SubscribeEvent<TestEventArgs>(EventSource.Local, subscriber, Handler);
            bus.SubscribeEvent<TestEventArgs>(EventSource.Local, subscriber, Handler);

            // Act
            bus.RaiseEvent(EventSource.Local, new TestEventArgs());

            //Assert
            Assert.That(delegateCallCount, Is.EqualTo(1));
        }

        /// <summary>
        /// Subscribing two different delegates to a single event type causes both events
        /// to be raised in an indeterminate order.
        /// </summary>
        [Test]
        public void SubscribeEvent_MultipleDelegates_BothRaised()
        {
            // Arrange
            var bus = BusFactory();
            var subscriber = new TestEventSubscriber();

            int delFooCount = 0;
            int delBarCount = 0;

            bus.SubscribeEvent<TestEventArgs>(EventSource.Local, subscriber, ev => delFooCount++);
            bus.SubscribeEvent<TestEventArgs>(EventSource.Local, subscriber, ev => delBarCount++);

            // Act
            bus.RaiseEvent(EventSource.Local, new TestEventArgs());

            // Assert
            Assert.That(delFooCount, Is.EqualTo(1));
            Assert.That(delBarCount, Is.EqualTo(1));
        }

        /// <summary>
        /// A subscriber's handlers are properly called only when the specified event type is raised.
        /// </summary>
        [Test]
        public void SubscribeEvent_MultipleSubscriptions_IndividuallyCalled()
        {
            // Arrange
            var bus = BusFactory();
            var subscriber = new TestEventSubscriber();

            int delFooCount = 0;
            int delBarCount = 0;

            bus.SubscribeEvent<TestEventArgs>(EventSource.Local, subscriber, ev => delFooCount++);
            bus.SubscribeEvent<TestEventTwoArgs>(EventSource.Local, subscriber, ev => delBarCount++);

            // Act & Assert
            bus.RaiseEvent(EventSource.Local, new TestEventArgs());
            Assert.That(delFooCount, Is.EqualTo(1));
            Assert.That(delBarCount, Is.EqualTo(0));

            delFooCount = delBarCount = 0;

            bus.RaiseEvent(EventSource.Local, new TestEventTwoArgs());
            Assert.That(delFooCount, Is.EqualTo(0));
            Assert.That(delBarCount, Is.EqualTo(1));
        }

        /// <summary>
        /// Trying to subscribe with <see cref="EventSource.None"/> makes no sense and causes
        /// a <see cref="ArgumentOutOfRangeException"/> to be thrown.
        /// </summary>
        [Test]
        public void SubscribeEvent_SourceNone_ArgOutOfRange()
        {
            // Arrange
            var bus = BusFactory();
            var subscriber = new TestEventSubscriber();

            // Act
            void Code() => bus.SubscribeEvent(EventSource.None, subscriber, (EntityEventHandler<TestEventArgs>)null!);

            //Assert
            Assert.Throws<ArgumentOutOfRangeException>(Code);
        }

        /// <summary>
        /// Unsubscribing a handler twice does nothing.
        /// </summary>
        [Test]
        public void UnsubscribeEvent_DoubleUnsubscribe_Nop()
        {
            // Arrange
            var bus = BusFactory();
            var subscriber = new TestEventSubscriber();

            void Handler(TestEventArgs ev) { }

            bus.SubscribeEvent<TestEventArgs>(EventSource.Local, subscriber, Handler);
            bus.UnsubscribeEvent<TestEventArgs>(EventSource.Local, subscriber);

            // Act
            bus.UnsubscribeEvent<TestEventArgs>(EventSource.Local, subscriber);

            // Assert: Does not throw
        }

        /// <summary>
        /// Unsubscribing a handler that was never subscribed in the first place does nothing.
        /// </summary>
        [Test]
        public void UnsubscribeEvent_NoSubscription_Nop()
        {
            // Arrange
            var bus = BusFactory();
            var subscriber = new TestEventSubscriber();

            // Act
            bus.UnsubscribeEvent<TestEventArgs>(EventSource.Local, subscriber);

            // Assert: Does not throw
        }

        /// <summary>
        /// Trying to unsubscribe with a null subscriber causes a <see cref="ArgumentNullException"/> to be thrown.
        /// </summary>
        [Test]
        public void UnsubscribeEvent_NullSubscriber_NullArgumentException()
        {
            // Arrange
            var bus = BusFactory();

            // Act
            void Code() => bus.UnsubscribeEvent<TestEventArgs>(EventSource.Local, null!);

            // Assert
            Assert.Throws<ArgumentNullException>(Code);
        }

        /// <summary>
        /// An event cannot be subscribed to with <see cref="EventSource.None"/>, so trying to unsubscribe
        /// with an <see cref="EventSource.None"/> causes a <see cref="ArgumentOutOfRangeException"/> to be thrown.
        /// </summary>
        [Test]
        public void UnsubscribeEvent_SourceNone_ArgOutOfRange()
        {
            // Arrange
            var bus = BusFactory();
            var subscriber = new TestEventSubscriber();

            // Act
            void Code() => bus.UnsubscribeEvent<TestEventArgs>(EventSource.None, subscriber);

            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(Code);
        }

        /// <summary>
        /// Trying to queue a null event causes a <see cref="ArgumentNullException"/> to be thrown.
        /// </summary>
        [Test]
        public void RaiseEvent_NullEvent_ArgumentNullException()
        {
            // Arrange
            var bus = BusFactory();

            // Act
            void Code() => bus.RaiseEvent(EventSource.Local, null!);

            // Assert
            Assert.Throws<ArgumentNullException>(Code);
        }

        /// <summary>
        /// Raising an event with no handlers subscribed to it does nothing.
        /// </summary>
        [Test]
        public void RaiseEvent_NoSubscriptions_Nop()
        {
            // Arrange
            var bus = BusFactory();
            var subscriber = new TestEventSubscriber();

            int delCalledCount = 0;
            bus.SubscribeEvent<TestEventTwoArgs>(EventSource.Local, subscriber, ev => delCalledCount++);

            // Act
            bus.RaiseEvent(EventSource.Local, new TestEventArgs());

            // Assert
            Assert.That(delCalledCount, Is.EqualTo(0));
        }

        /// <summary>
        /// Raising an event when a handler has been unsubscribed no longer calls the handler.
        /// </summary>
        [Test]
        public void RaiseEvent_Unsubscribed_Nop()
        {
            // Arrange
            var bus = BusFactory();
            var subscriber = new TestEventSubscriber();

            int delCallCount = 0;
            void Handler(TestEventArgs ev) => delCallCount++;

            bus.SubscribeEvent<TestEventArgs>(EventSource.Local, subscriber, Handler);
            bus.UnsubscribeEvent<TestEventArgs>(EventSource.Local, subscriber);

            // Act
            bus.RaiseEvent(EventSource.Local, new TestEventArgs());

            // Assert
            Assert.That(delCallCount, Is.EqualTo(0));
        }

        /// <summary>
        /// Trying to raise an event with <see cref="EventSource.None"/> makes no sense and causes
        /// a <see cref="ArgumentOutOfRangeException"/> to be thrown.
        /// </summary>
        [Test]
        public void RaiseEvent_SourceNone_ArgOutOfRange()
        {
            // Arrange
            var bus = BusFactory();

            // Act
            void Code() => bus.RaiseEvent(EventSource.None, new TestEventArgs());

            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(Code);
        }

        /// <summary>
        /// Trying to unsubscribe all of a null subscriber's events causes a <see cref="ArgumentNullException"/> to be thrown.
        /// </summary>
        [Test]
        public void UnsubscribeEvents_NullSubscriber_NullArgumentException()
        {
            // Arrange
            var bus = BusFactory();

            // Act
            void Code() => bus.UnsubscribeEvents(null!);

            // Assert
            Assert.Throws<ArgumentNullException>(Code);
        }

        /// <summary>
        /// Unsubscribing a subscriber with no subscriptions does nothing.
        /// </summary>
        [Test]
        public void UnsubscribeEvents_NoSubscriptions_Nop()
        {
            // Arrange
            var bus = BusFactory();
            var subscriber = new TestEventSubscriber();

            // Act
            bus.UnsubscribeEvents(subscriber);

            // Assert: no exception
        }

        /// <summary>
        /// The subscriber's handlers are not raised after they are unsubscribed.
        /// </summary>
        [Test]
        public void UnsubscribeEvents_UnsubscribedHandler_Nop()
        {
            // Arrange
            var bus = BusFactory();
            var subscriber = new TestEventSubscriber();

            int delCallCount = 0;
            void Handler(TestEventArgs ev) => delCallCount++;

            bus.SubscribeEvent<TestEventArgs>(EventSource.Local, subscriber, Handler);
            bus.UnsubscribeEvents(subscriber);

            // Act
            bus.RaiseEvent(EventSource.Local, new TestEventArgs());

            // Assert
            Assert.That(delCallCount, Is.EqualTo(0));
        }

        /// <summary>
        /// Trying to queue a null event causes a <see cref="ArgumentNullException"/> to be thrown.
        /// </summary>
        [Test]
        public void QueueEvent_NullEvent_ArgumentNullException()
        {
            // Arrange
            var bus = BusFactory();

            // Act
            void Code() => bus.QueueEvent(EventSource.Local, null!);

            // Assert
            Assert.Throws<ArgumentNullException>(Code);
        }

        /// <summary>
        /// Queuing an event does not immediately raise the event unless the queue is processed.
        /// </summary>
        [Test]
        public void QueueEvent_EventQueued_DoesNotImmediatelyRaise()
        {
            // Arrange
            var bus = BusFactory();
            var subscriber = new TestEventSubscriber();

            int delCallCount = 0;
            void Handler(TestEventArgs ev) => delCallCount++;

            bus.SubscribeEvent<TestEventArgs>(EventSource.Local, subscriber, Handler);

            // Act
            bus.QueueEvent(EventSource.Local, new TestEventArgs());

            // Assert
            Assert.That(delCallCount, Is.EqualTo(0));
        }

        /// <summary>
        /// Trying to queue an event with <see cref="EventSource.None"/> makes no sense and causes
        /// a <see cref="ArgumentOutOfRangeException"/> to be thrown.
        /// </summary>
        [Test]
        public void QueueEvent_SourceNone_ArgOutOfRange()
        {
            // Arrange
            var bus = BusFactory();

            // Act
            void Code() => bus.QueueEvent(EventSource.None, new TestEventArgs());

            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(Code);
        }

        /// <summary>
        /// Queued events are raised when the queue is processed.
        /// </summary>
        [Test]
        public void ProcessQueue_EventQueued_HandlerRaised()
        {
            // Arrange
            var bus = BusFactory();
            var subscriber = new TestEventSubscriber();

            int delCallCount = 0;
            void Handler(TestEventArgs ev) => delCallCount++;

            bus.SubscribeEvent<TestEventArgs>(EventSource.Local, subscriber, Handler);
            bus.QueueEvent(EventSource.Local, new TestEventArgs());

            // Act
            bus.ProcessEventQueue();

            // Assert
            Assert.That(delCallCount, Is.EqualTo(1));
        }

        [Test]
        public async Task AwaitEvent()
        {
            // Arrange
            var bus = BusFactory();
            var args = new TestEventArgs();

            // Act
            var task = bus.AwaitEvent<TestEventArgs>(EventSource.Local);
            bus.RaiseEvent(EventSource.Local, args);
            var result = await task;

            // Assert
            Assert.That(result, Is.EqualTo(args));
        }

        [Test]
        public void AwaitEvent_SourceNone_ArgOutOfRange()
        {
            // Arrange
            var bus = BusFactory();

            // Act
            void Code() => bus.AwaitEvent<TestEventArgs>(EventSource.None);

            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(Code);
        }

        [Test]
        public void AwaitEvent_DoubleTask_InvalidException()
        {
            // Arrange
            var bus = BusFactory();
            bus.AwaitEvent<TestEventArgs>(EventSource.Local);

            // Act
            void Code() => bus.AwaitEvent<TestEventArgs>(EventSource.Local);

            // Assert
            Assert.Throws<InvalidOperationException>(Code);
        }
    }

    internal class TestEventSubscriber : IEntityEventSubscriber { }

    internal class TestEventArgs : EntityEventArgs { }
    internal class TestEventTwoArgs : EntityEventArgs { }
}
