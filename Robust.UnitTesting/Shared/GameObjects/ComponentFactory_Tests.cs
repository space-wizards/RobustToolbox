using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.GameObjects
{
    [TestFixture]
    [TestOf(typeof(ComponentFactory))]
    public sealed class ComponentFactory_Tests : RobustUnitTest
    {
        private const string TestComponentName = "A";
        private const string LowercaseTestComponentName = "a";
        private const string NonexistentComponentName = "B";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            IoCManager.Resolve<IComponentFactory>().RegisterClass<TestComponent>();
        }

        [Test]
        public void GetComponentAvailabilityTest()
        {
            var componentFactory = IoCManager.Resolve<IComponentFactory>();

            // Should not exist
            Assert.That(componentFactory.GetComponentAvailability(NonexistentComponentName), Is.EqualTo(ComponentAvailability.Unknown));
            Assert.That(componentFactory.GetComponentAvailability(NonexistentComponentName, true), Is.EqualTo(ComponentAvailability.Unknown));

            // Normal casing, do not ignore case, should exist
            Assert.That(componentFactory.GetComponentAvailability(TestComponentName), Is.EqualTo(ComponentAvailability.Available));

            // Normal casing, ignore case, should exist
            Assert.That(componentFactory.GetComponentAvailability(TestComponentName, true), Is.EqualTo(ComponentAvailability.Available));

            // Lower casing, do not ignore case, should not exist
            Assert.That(componentFactory.GetComponentAvailability(LowercaseTestComponentName), Is.EqualTo(ComponentAvailability.Unknown));

            // Lower casing, ignore case, should exist
            Assert.That(componentFactory.GetComponentAvailability(LowercaseTestComponentName, true), Is.EqualTo(ComponentAvailability.Available));
        }

        [Test]
        public void GetComponentTest()
        {
            var componentFactory = IoCManager.Resolve<IComponentFactory>();

            // Should not exist
            Assert.Throws<UnknownComponentException>(() => componentFactory.GetComponent(NonexistentComponentName));
            Assert.Throws<UnknownComponentException>(() => componentFactory.GetComponent(NonexistentComponentName, true));

            // Normal casing, do not ignore case, should exist
            Assert.IsInstanceOf<TestComponent>(componentFactory.GetComponent(TestComponentName));

            // Normal casing, ignore case, should exist
            Assert.IsInstanceOf<TestComponent>(componentFactory.GetComponent(TestComponentName, true));

            // Lower casing, do not ignore case, should not exist
            Assert.Throws<UnknownComponentException>(() => componentFactory.GetComponent(LowercaseTestComponentName));

            // Lower casing, ignore case, should exist
            Assert.IsInstanceOf<TestComponent>(componentFactory.GetComponent(LowercaseTestComponentName, true));
        }

        [Test]
        public void GetRegistrationTest()
        {
            var componentFactory = IoCManager.Resolve<IComponentFactory>();

            // Should not exist
            Assert.Throws<UnknownComponentException>(() => componentFactory.GetRegistration(NonexistentComponentName));
            Assert.Throws<UnknownComponentException>(() => componentFactory.GetRegistration(NonexistentComponentName, true));

            // Normal casing, do not ignore case, should exist
            Assert.DoesNotThrow(() => componentFactory.GetRegistration(TestComponentName));

            // Normal casing, ignore case, should exist
            Assert.DoesNotThrow(() => componentFactory.GetRegistration(TestComponentName, true));

            // Lower casing, do not ignore case, should not exist
            Assert.Throws<UnknownComponentException>(() => componentFactory.GetRegistration(LowercaseTestComponentName));

            // Lower casing, ignore case, should exist
            Assert.DoesNotThrow(() => componentFactory.GetRegistration(LowercaseTestComponentName, true));
        }

        [Test]
        public void TryGetRegistrationTest()
        {
            var componentFactory = IoCManager.Resolve<IComponentFactory>();

            // Should not exist
            Assert.False(componentFactory.TryGetRegistration(NonexistentComponentName, out _));
            Assert.False(componentFactory.TryGetRegistration(NonexistentComponentName, out _, true));

            // Normal casing, do not ignore case, should exist
            Assert.True(componentFactory.TryGetRegistration(TestComponentName, out _));

            // Normal casing, ignore case, should exist
            Assert.True(componentFactory.TryGetRegistration(TestComponentName, out _, true));

            // Lower casing, do not ignore case, should not exist
            Assert.False(componentFactory.TryGetRegistration(LowercaseTestComponentName, out _));

            // Lower casing, ignore case, should exist
            Assert.True(componentFactory.TryGetRegistration(LowercaseTestComponentName, out _, true));
        }

        [ComponentProtoName(TestComponentName)]
        private sealed class TestComponent : Component
        {
        }
    }
}
