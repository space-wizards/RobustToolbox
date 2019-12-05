using System;
using NUnit.Framework;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Client.UserInterface
{
    [TestFixture]
    [TestOf(typeof(Control))]
    public class ControlTest : RobustUnitTest
    {
        private static readonly AttachedProperty _refTypeAttachedProperty
            = AttachedProperty.Create("_refType", typeof(ControlTest), typeof(string), "foo", v => (string) v != "bar");

        private static readonly AttachedProperty _valueTypeAttachedProperty
            = AttachedProperty.Create("_valueType", typeof(ControlTest), typeof(float));

        private static readonly AttachedProperty _nullableAttachedProperty
            = AttachedProperty.Create("_nullable", typeof(ControlTest), typeof(float?));

        public override UnitTestProject Project => UnitTestProject.Client;

        [OneTimeSetUp]
        public void Setup()
        {
            IoCManager.Resolve<IUserInterfaceManagerInternal>().InitializeTesting();
        }

        /// <summary>
        ///     Test that you can't parent a control to its (grand)child.
        /// </summary>
        [Test]
        public void TestNoRecursion()
        {
            var control1 = new Control();
            var control2 = new Control();
            var control3 = new Control();

            control1.AddChild(control2);
            // Test direct parent/child.
            Assert.That(() => control2.AddChild(control1), Throws.ArgumentException);

            control2.AddChild(control3);
            // Test grand child.
            Assert.That(() => control3.AddChild(control1), Throws.ArgumentException);
        }

        [Test]
        public void TestVisibleInTree()
        {
            var control1 = new Control();

            // Not visible because not parented to root control.
            Assert.That(control1.Visible, Is.True);
            Assert.That(control1.VisibleInTree, Is.False);

            control1.UserInterfaceManager.RootControl.AddChild(control1);
            Assert.That(control1.Visible, Is.True);
            Assert.That(control1.VisibleInTree, Is.True);

            control1.Visible = false;
            Assert.That(control1.Visible, Is.False);
            Assert.That(control1.VisibleInTree, Is.False);
            control1.Visible = true;

            var control2 = new Control();
            Assert.That(control2.VisibleInTree, Is.False);

            control1.AddChild(control2);
            Assert.That(control2.VisibleInTree, Is.True);

            control1.Visible = false;
            Assert.That(control2.VisibleInTree, Is.False);

            control2.Visible = false;
            Assert.That(control2.VisibleInTree, Is.False);

            control1.Visible = true;
            Assert.That(control2.VisibleInTree, Is.False);

            control1.Dispose();
        }

        [Test]
        public void TestAttachedPropertiesBasic()
        {
            var control = new Control();

            control.SetValue(_refTypeAttachedProperty, "honk");

            Assert.AreEqual(control.GetValue(_refTypeAttachedProperty), "honk");
        }

        [Test]
        public void TestAttachedPropertiesValidate()
        {
            var control = new Control();

            Assert.Throws<ArgumentException>(() => control.SetValue(_refTypeAttachedProperty, "bar"));
        }

        [Test]
        public void TestAttachedPropertiesInvalidType()
        {
            var control = new Control();

            Assert.Throws<ArgumentException>(() => control.SetValue(_refTypeAttachedProperty, new object()));
            Assert.Throws<ArgumentException>(() => control.SetValue(_valueTypeAttachedProperty, new object()));
        }

        [Test]
        public void TestAttachedPropertiesInvalidNull()
        {
            var control = new Control();

            Assert.Throws<ArgumentNullException>(() => control.SetValue(_valueTypeAttachedProperty, null));
        }

        [Test]
        public void TestAttachedPropertiesValidNull()
        {
            var control = new Control();

            control.SetValue(_nullableAttachedProperty, null);
        }
    }
}
