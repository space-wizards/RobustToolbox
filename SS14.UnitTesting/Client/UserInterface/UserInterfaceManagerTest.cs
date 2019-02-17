using System;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using SS14.Client.Input;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.UserInterface;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace SS14.UnitTesting.Client.UserInterface
{
    [TestFixture]
    public class UserInterfaceManagerTest : SS14UnitTest
    {
        public override UnitTestProject Project => UnitTestProject.Client;

        private IUserInterfaceManagerInternal _userInterfaceManager;

        [OneTimeSetUp]
        public void Setup()
        {
            _userInterfaceManager = IoCManager.Resolve<IUserInterfaceManagerInternal>();
            _userInterfaceManager.InitializeTesting();
        }

        [Test]
        [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
        public void TestMouseDown()
        {
            // We create 4 controls.
            // Control 2 is set to stop mouse events,
            // Control 3 pass,
            // Control 4 ignore.
            // We check that 4 and 1 do not receive events, that 3 receives before 2, and that positions are correct.
            var control1 = new Control
            {
                CustomMinimumSize = new Vector2(50, 50)
            };
            var control2 = new Control
            {
                CustomMinimumSize = new Vector2(50, 50),
                MouseFilter = Control.MouseFilterMode.Stop
            };
            var control3 = new Control
            {
                CustomMinimumSize = new Vector2(50, 50),
                MouseFilter = Control.MouseFilterMode.Pass
            };
            var control4 = new Control
            {
                CustomMinimumSize = new Vector2(50, 50),
                MouseFilter = Control.MouseFilterMode.Ignore
            };

            _userInterfaceManager.RootControl.AddChild(control1);
            control1.AddChild(control2);
            // Offsets to test relative positioning on the events.
            control2.Position = new Vector2(5, 5);
            control2.AddChild(control3);
            control3.Position = new Vector2(5, 5);
            control3.AddChild(control4);
            control4.Position = new Vector2(5, 5);

            var mouseEvent = new MouseButtonEventArgs(Mouse.Button.Left, false, Mouse.ButtonMask.None,
                new Vector2(30, 30), false, false, false, false);

            var control2Fired = false;
            var control3Fired = false;

            control1.OnMouseDown += _ => Assert.Fail("Control 1 should not get a mouse event.");

            void Control2MouseDown(GUIMouseButtonEventArgs ev)
            {
                Assert.That(control2Fired, Is.False);
                Assert.That(control3Fired, Is.True);

                Assert.That(ev.RelativePosition, Is.EqualTo(new Vector2(25, 25)));

                control2Fired = true;
            }

            control2.OnMouseDown += Control2MouseDown;

            control3.OnMouseDown += ev =>
            {
                Assert.That(control2Fired, Is.False);
                Assert.That(control3Fired, Is.False);

                Assert.That(ev.RelativePosition, Is.EqualTo(new Vector2(20, 20)));

                control3Fired = true;
            };

            control4.OnMouseDown += _ => Assert.Fail("Control 4 should not get a mouse event.");

            _userInterfaceManager.MouseDown(mouseEvent);

            Assert.Multiple(() =>
            {
                Assert.That(control2Fired, Is.True);
                Assert.That(control3Fired, Is.True);
            });

            // Step two: instead of relying on stop for control2 to prevent the event reaching control1,
            // handle the event in control2.

            control2Fired = false;
            control3Fired = false;

            control2.OnMouseDown -= Control2MouseDown;
            control2.OnMouseDown += ev =>
            {
                Assert.That(control2Fired, Is.False);
                Assert.That(control3Fired, Is.True);

                Assert.That(ev.RelativePosition, Is.EqualTo(new Vector2(25, 25)));

                control2Fired = true;
                ev.Handle();
            };
            control2.MouseFilter = Control.MouseFilterMode.Pass;

            _userInterfaceManager.MouseDown(mouseEvent);

            Assert.Multiple(() =>
            {
                Assert.That(control2Fired, Is.True);
                Assert.That(control3Fired, Is.True);
            });
        }
    }
}
