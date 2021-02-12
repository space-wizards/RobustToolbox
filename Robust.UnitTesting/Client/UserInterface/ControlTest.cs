using System;
using NUnit.Framework;
using Robust.Client.Animations;
using Robust.Client.UserInterface;
using Robust.Shared.Animations;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Robust.UnitTesting.Client.UserInterface
{
    [TestFixture]
    [TestOf(typeof(Control))]
    public class ControlTest : RobustUnitTest
    {
        private static readonly AttachedProperty _refTypeAttachedProperty
            = AttachedProperty.Create("_refType", typeof(ControlTest), typeof(string), "foo", v => (string?) v != "bar");

        private static readonly AttachedProperty _valueTypeAttachedProperty
            = AttachedProperty.Create("_valueType", typeof(ControlTest), typeof(float));

        private static readonly AttachedProperty _nullableAttachedProperty
            = AttachedProperty.Create("_nullable", typeof(ControlTest), typeof(float?));

        private static readonly AttachedProperty<int> _genericProperty =
            AttachedProperty<int>.Create("generic", typeof(ControlTest), 5, i => i % 2 == 1);

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

            Assert.That(control.GetValue(_refTypeAttachedProperty), Is.EqualTo("honk"));
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

        [Test]
        public void TestAttachedPropertiesGeneric()
        {
            var control = new Control();

            Assert.That(control.GetValue(_genericProperty), Is.EqualTo(5));

            control.SetValue(_genericProperty, 11);

            Assert.That(control.GetValue(_genericProperty), Is.EqualTo(11));

            Assert.That(() => control.SetValue(_genericProperty, 10), Throws.ArgumentException);
        }

        [Test]
        public void TestAnimations()
        {
            var control = new TestControl();
            var animation = new Animation
            {
                Length = TimeSpan.FromSeconds(3),
                AnimationTracks =
                {
                    new AnimationTrackControlProperty
                    {
                        Property = nameof(TestControl.Foo),
                        KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(1f, 1f),
                            new AnimationTrackProperty.KeyFrame(3f, 2f)
                        }
                    }
                }
            };

            control.PlayAnimation(animation, "foo");
            control.DoFrameUpdate(new FrameEventArgs(0.5f));

            Assert.That(control.Foo, new ApproxEqualityConstraint(0f)); // Should still be 0.

            control.DoFrameUpdate(new FrameEventArgs(0.5001f));

            Assert.That(control.Foo, new ApproxEqualityConstraint(1f, 0.01)); // Should now be 1.

            control.DoFrameUpdate(new FrameEventArgs(0.5f));

            Assert.That(control.Foo, new ApproxEqualityConstraint(1.5f, 0.01)); // Should now be 1.5.

            control.DoFrameUpdate(new FrameEventArgs(1.0f));

            Assert.That(control.Foo, new ApproxEqualityConstraint(2.5f, 0.01)); // Should now be 2.5.

            control.DoFrameUpdate(new FrameEventArgs(0.5f));

            Assert.That(control.Foo, new ApproxEqualityConstraint(3f, 0.01)); // Should now be 3.

            control.DoFrameUpdate(new FrameEventArgs(0.5f));

            Assert.That(control.Foo, new ApproxEqualityConstraint(3f, 0.01)); // Should STILL be 3.
        }

        private sealed class TestControl : Control
        {
            [Animatable] public float Foo { get; set; }
        }
    }
}
