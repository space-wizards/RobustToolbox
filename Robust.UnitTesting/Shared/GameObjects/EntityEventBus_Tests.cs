using System;
using NUnit.Framework;
using Robust.Shared.GameObjects;

namespace Robust.UnitTesting.Shared.GameObjects
{
    [TestFixture, Parallelizable, TestOf(typeof(EntityEventBus))]
    public class EntityEventBus_Tests
    {
        /// <summary>
        /// Trying to subscribe a null handler causes a <see cref="ArgumentNullException"/> to be thrown.
        /// </summary>
        [Test]
        public void SubscribeEvent_NullHandler_NullArgumentException()
        {
            // Arrange
            var bus = new EntityEventBus();
            var subscriber = new TestEventSubscriber();

            // Act
            void Code() => bus.SubscribeEvent((EntityEventHandler<TestEventArgs>) null, subscriber);
            
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
            var bus = new EntityEventBus();

            // Act
            void Code() => bus.SubscribeEvent<TestEventArgs>((sender, ev) => {}, null);

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
            var bus = new EntityEventBus();
            var subscriber = new TestEventSubscriber();

            int delegateCallCount = 0;
            void Handler(object sender, TestEventArgs ev) => delegateCallCount++;

            // 2 subscriptions 1 handler
            bus.SubscribeEvent<TestEventArgs>(Handler, subscriber);
            bus.SubscribeEvent<TestEventArgs>(Handler, subscriber);

            // Act
            bus.RaiseEvent(null, new TestEventArgs());

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
            var bus = new EntityEventBus();
            var subscriber = new TestEventSubscriber();

            int delFooCount = 0;
            int delBarCount = 0;

            bus.SubscribeEvent<TestEventArgs>((sender, ev) => delFooCount++, subscriber);
            bus.SubscribeEvent<TestEventArgs>((sender, ev) => delBarCount++, subscriber);

            // Act
            bus.RaiseEvent(null, new TestEventArgs());

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
            var bus = new EntityEventBus();
            var subscriber = new TestEventSubscriber();

            int delFooCount = 0;
            int delBarCount = 0;

            bus.SubscribeEvent<TestEventArgs>((sender, ev) => delFooCount++, subscriber);
            bus.SubscribeEvent<TestEventTwoArgs>((sender, ev) => delBarCount++, subscriber);

            // Act & Assert
            bus.RaiseEvent(null, new TestEventArgs());
            Assert.That(delFooCount, Is.EqualTo(1));
            Assert.That(delBarCount, Is.EqualTo(0));

            delFooCount = delBarCount = 0;

            bus.RaiseEvent(null, new TestEventTwoArgs());
            Assert.That(delFooCount, Is.EqualTo(0));
            Assert.That(delBarCount, Is.EqualTo(1));
        }

        /// <summary>
        /// Unsubscribing a handler twice does nothing.
        /// </summary>
        [Test]
        public void UnsubscribeEvent_DoubleUnsubscribe_Nop()
        {
            // Arrange
            var bus = new EntityEventBus();
            var subscriber = new TestEventSubscriber();

            void Handler(object sender, TestEventArgs ev) { }

            bus.SubscribeEvent<TestEventArgs>(Handler, subscriber);
            bus.UnsubscribeEvent<TestEventArgs>(subscriber);

            // Act
            bus.UnsubscribeEvent<TestEventArgs>(subscriber);

            // Assert: Does not throw
        }

        /// <summary>
        /// Unsubscribing a handler that was never subscribed in the first place does nothing.
        /// </summary>
        [Test]
        public void UnsubscribeEvent_NoSubscription_Nop()
        {
            // Arrange
            var bus = new EntityEventBus();
            var subscriber = new TestEventSubscriber();

            // Act
            bus.UnsubscribeEvent<TestEventArgs>(subscriber);

            // Assert: Does not throw
        }

        /// <summary>
        /// Trying to unsubscribe with a null subscriber causes a <see cref="ArgumentNullException"/> to be thrown.
        /// </summary>
        [Test]
        public void UnsubscribeEvent_NullSubscriber_NullArgumentException()
        {
            // Arrange
            var bus = new EntityEventBus();

            // Act
            void Code() => bus.UnsubscribeEvent<TestEventArgs>(null);

            // Assert
            Assert.Throws<ArgumentNullException>(Code);
        }

        /// <summary>
        /// Trying to queue a null event causes a <see cref="ArgumentNullException"/> to be thrown.
        /// </summary>
        [Test]
        public void RaiseEvent_NullEvent_ArgumentNullException()
        {
            // Arrange
            var bus = new EntityEventBus();

            // Act
            void Code() => bus.RaiseEvent(null, null);

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
            var bus = new EntityEventBus();
            var subscriber = new TestEventSubscriber();

            int delCalledCount = 0;
            bus.SubscribeEvent<TestEventTwoArgs>(((sender, ev) => delCalledCount++), subscriber);

            // Act
            bus.RaiseEvent(null, new TestEventArgs());

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
            var bus = new EntityEventBus();
            var subscriber = new TestEventSubscriber();

            int delCallCount = 0;
            void Handler(object sender, TestEventArgs ev) => delCallCount++;

            bus.SubscribeEvent<TestEventArgs>(Handler, subscriber);
            bus.UnsubscribeEvent<TestEventArgs>(subscriber);

            // Act
            bus.RaiseEvent(null, new TestEventArgs());

            // Assert
            Assert.That(delCallCount, Is.EqualTo(0));
        }

        /// <summary>
        /// Trying to unsubscribe all of a null subscriber's events causes a <see cref="ArgumentNullException"/> to be thrown.
        /// </summary>
        [Test]
        public void UnsubscribeEvents_NullSubscriber_NullArgumentException()
        {
            // Arrange
            var bus = new EntityEventBus();

            // Act
            void Code() => bus.UnsubscribeEvents(null);

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
            var bus = new EntityEventBus();
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
            var bus = new EntityEventBus();
            var subscriber = new TestEventSubscriber();

            int delCallCount = 0;
            void Handler(object sender, TestEventArgs ev) => delCallCount++;

            bus.SubscribeEvent<TestEventArgs>(Handler, subscriber);
            bus.UnsubscribeEvents(subscriber);

            // Act
            bus.RaiseEvent(null, new TestEventArgs());

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
            var bus = new EntityEventBus();

            // Act
            void Code() => bus.QueueEvent(null, null);

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
            var bus = new EntityEventBus();
            var subscriber = new TestEventSubscriber();

            int delCallCount = 0;
            void Handler(object sender, TestEventArgs ev) => delCallCount++;

            bus.SubscribeEvent<TestEventArgs>(Handler, subscriber);

            // Act
            bus.QueueEvent(null, new TestEventArgs());

            // Assert
            Assert.That(delCallCount, Is.EqualTo(0));
        }

        /// <summary>
        /// Queued events are raised when the queue is processed.
        /// </summary>
        [Test]
        public void ProcessQueue_EventQueued_HandlerRaised()
        {
            // Arrange
            var bus = new EntityEventBus();
            var subscriber = new TestEventSubscriber();

            int delCallCount = 0;
            void Handler(object sender, TestEventArgs ev) => delCallCount++;

            bus.SubscribeEvent<TestEventArgs>(Handler, subscriber);
            bus.QueueEvent(null, new TestEventArgs());

            // Act
            bus.ProcessEventQueue();

            // Assert
            Assert.That(delCallCount, Is.EqualTo(1));
        }
    }

    internal class TestEventSubscriber : IEntityEventSubscriber { }

    internal class TestEventArgs : EntityEventArgs { }
    internal class TestEventTwoArgs : EntityEventArgs { }
}
