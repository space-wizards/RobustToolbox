using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Client.UserInterface
{
    [TestFixture]
    public class UserInterfaceManagerTest : RobustUnitTest
    {
        public override UnitTestProject Project => UnitTestProject.Client;

        private IUserInterfaceManagerInternal _userInterfaceManager = default!;

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
            var control1 = new LayoutContainer
            {
                MinSize = new Vector2(50, 50)
            };
            var control2 = new LayoutContainer
            {
                MinSize = new Vector2(50, 50),
                MouseFilter = Control.MouseFilterMode.Stop
            };
            var control3 = new LayoutContainer
            {
                MinSize = new Vector2(50, 50),
                MouseFilter = Control.MouseFilterMode.Pass
            };
            var control4 = new LayoutContainer
            {
                MinSize = new Vector2(50, 50),
                MouseFilter = Control.MouseFilterMode.Ignore
            };

            _userInterfaceManager.RootControl.AddChild(control1);
            control1.AddChild(control2);
            // Offsets to test relative positioning on the events.
            LayoutContainer.SetPosition(control2, (5, 5));
            control2.AddChild(control3);
            LayoutContainer.SetPosition(control3, (5, 5));
            control3.AddChild(control4);
            LayoutContainer.SetPosition(control4, (5, 5));

            control1.Arrange(new UIBox2(0, 0, 50, 50));

            var mouseEvent = new BoundKeyEventArgs(EngineKeyFunctions.Use, BoundKeyState.Down,
                new ScreenCoordinates(30, 30, WindowId.Main), true);

            var control2Fired = false;
            var control3Fired = false;

            control1.OnKeyBindDown += _ => Assert.Fail("Control 1 should not get a mouse event.");

            void Control2MouseDown(GUIBoundKeyEventArgs ev)
            {
                Assert.That(control2Fired, NUnit.Framework.Is.False);
                Assert.That(control3Fired, NUnit.Framework.Is.True);

                Assert.That(ev.RelativePosition, NUnit.Framework.Is.EqualTo(new Vector2(25, 25)));

                control2Fired = true;
            }

            control2.OnKeyBindDown += Control2MouseDown;

            control3.OnKeyBindDown += ev =>
            {
                Assert.That(control2Fired, NUnit.Framework.Is.False);
                Assert.That(control3Fired, NUnit.Framework.Is.False);

                Assert.That(ev.RelativePosition, NUnit.Framework.Is.EqualTo(new Vector2(20, 20)));

                control3Fired = true;
            };

            control4.OnKeyBindDown += _ => Assert.Fail("Control 4 should not get a mouse event.");

            _userInterfaceManager.KeyBindDown(mouseEvent);

            Assert.Multiple(() =>
            {
                Assert.That(control2Fired, NUnit.Framework.Is.True);
                Assert.That(control3Fired, NUnit.Framework.Is.True);
            });

            // Step two: instead of relying on stop for control2 to prevent the event reaching control1,
            // handle the event in control2.

            control2Fired = false;
            control3Fired = false;

            control2.OnKeyBindDown -= Control2MouseDown;
            control2.OnKeyBindDown += ev =>
            {
                Assert.That(control2Fired, NUnit.Framework.Is.False);
                Assert.That(control3Fired, NUnit.Framework.Is.True);

                Assert.That(ev.RelativePosition, NUnit.Framework.Is.EqualTo(new Vector2(25, 25)));

                control2Fired = true;
                ev.Handle();
            };
            control2.MouseFilter = Control.MouseFilterMode.Pass;

            _userInterfaceManager.KeyBindDown(mouseEvent);

            Assert.Multiple(() =>
            {
                Assert.That(control2Fired, NUnit.Framework.Is.True);
                Assert.That(control3Fired, NUnit.Framework.Is.True);
            });

            control1.Dispose();
            control2.Dispose();
            control3.Dispose();
            control4.Dispose();
        }

        [Test]
        public void TestGrabKeyboardFocus()
        {
            Assert.That(_userInterfaceManager.KeyboardFocused, NUnit.Framework.Is.Null);
            var control1 = new Control {CanKeyboardFocus = true};
            var control2 = new Control {CanKeyboardFocus = true};

            control1.GrabKeyboardFocus();
            Assert.That(_userInterfaceManager.KeyboardFocused, NUnit.Framework.Is.EqualTo(control1));
            Assert.That(control1.HasKeyboardFocus(), NUnit.Framework.Is.EqualTo(true));

            control1.ReleaseKeyboardFocus();
            Assert.That(_userInterfaceManager.KeyboardFocused, NUnit.Framework.Is.Null);

            control1.Dispose();
            control2.Dispose();
        }

        [Test]
        public void TestGrabKeyboardFocusSteal()
        {
            Assert.That(_userInterfaceManager.KeyboardFocused, NUnit.Framework.Is.Null);
            var control1 = new Control {CanKeyboardFocus = true};
            var control2 = new Control {CanKeyboardFocus = true};

            control1.GrabKeyboardFocus();
            control2.GrabKeyboardFocus();
            Assert.That(_userInterfaceManager.KeyboardFocused, NUnit.Framework.Is.EqualTo(control2));
            control2.ReleaseKeyboardFocus();
            Assert.That(_userInterfaceManager.KeyboardFocused, NUnit.Framework.Is.Null);

            control1.Dispose();
            control2.Dispose();
        }

        [Test]
        public void TestGrabKeyboardFocusOtherRelease()
        {
            Assert.That(_userInterfaceManager.KeyboardFocused, NUnit.Framework.Is.Null);
            var control1 = new Control {CanKeyboardFocus = true};
            var control2 = new Control {CanKeyboardFocus = true};

            control1.GrabKeyboardFocus();
            control2.ReleaseKeyboardFocus();
            Assert.That(_userInterfaceManager.KeyboardFocused, NUnit.Framework.Is.EqualTo(control1));
            _userInterfaceManager.ReleaseKeyboardFocus();
            Assert.That(_userInterfaceManager.KeyboardFocused, NUnit.Framework.Is.Null);

            control1.Dispose();
            control2.Dispose();
        }

        [Test]
        public void TestGrabKeyboardFocusNull()
        {
            Assert.That(() => _userInterfaceManager.GrabKeyboardFocus(null!), Throws.ArgumentNullException);
            Assert.That(() => _userInterfaceManager.ReleaseKeyboardFocus(null!), Throws.ArgumentNullException);
        }

        [Test]
        public void TestGrabKeyboardFocusBlocked()
        {
            var control = new Control();
            Assert.That(() => _userInterfaceManager.GrabKeyboardFocus(control), Throws.ArgumentException);
        }

        [Test]
        public void TestGrabKeyboardFocusOnClick()
        {
            var control = new Control
            {
                CanKeyboardFocus = true,
                KeyboardFocusOnClick = true,
                MinSize = new Vector2(50, 50),
                MouseFilter = Control.MouseFilterMode.Stop
            };

            _userInterfaceManager.RootControl.AddChild(control);

            _userInterfaceManager.RootControl.Arrange(new UIBox2(0, 0, 50, 50));

            _userInterfaceManager.HandleCanFocusDown(new ScreenCoordinates(30, 30, WindowId.Main), out _);

            Assert.That(_userInterfaceManager.KeyboardFocused, NUnit.Framework.Is.EqualTo(control));
            _userInterfaceManager.ReleaseKeyboardFocus();
            Assert.That(_userInterfaceManager.KeyboardFocused, NUnit.Framework.Is.Null);

            control.Dispose();
        }

        /// <summary>
        ///     Assert that indeed nothing happens when the control has focus modes off.
        /// </summary>
        [Test]
        public void TestNotGrabKeyboardFocusOnClick()
        {
            var control = new Control
            {
                MinSize = new Vector2(50, 50),
                MouseFilter = Control.MouseFilterMode.Stop
            };

            _userInterfaceManager.RootControl.AddChild(control);

            var pos = new ScreenCoordinates(30, 30, WindowId.Main);

            var mouseEvent = new GUIBoundKeyEventArgs(EngineKeyFunctions.Use, BoundKeyState.Down,
                pos, true, pos.Position / 1 - control.GlobalPosition, pos.Position - control.GlobalPixelPosition);

            _userInterfaceManager.KeyBindDown(mouseEvent);

            Assert.That(_userInterfaceManager.KeyboardFocused, NUnit.Framework.Is.Null);

            control.Dispose();
        }
    }
}
