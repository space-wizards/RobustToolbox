using NUnit.Framework;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Client.UserInterface
{
    [TestFixture]
    [TestOf(typeof(Control))]
    public class ControlTest : RobustUnitTest
    {
        public override UnitTestProject Project => UnitTestProject.Client;

        [OneTimeSetUp]
        public void Setup()
        {
            IoCManager.Resolve<IUserInterfaceManagerInternal>().InitializeTesting();
        }

        [Test]
        public void TestMarginLayoutBasic()
        {
            var control = new Control {Size = new Vector2(100, 100)};
            var child = new Control
            {
                MarginRight = 5,
                MarginBottom = 5,
            };
            control.AddChild(child);
            Assert.That(child.Size, Is.EqualTo(new Vector2(5, 5)));
            Assert.That(child.Position, Is.EqualTo(Vector2.Zero));

            child.MarginTop = 3;
            child.MarginLeft = 3;
            Assert.That(child.Size, Is.EqualTo(new Vector2(2, 2)));
            Assert.That(child.Position, Is.EqualTo(new Vector2(3, 3)));
        }

        [Test]
        public void TestAnchorLayoutBasic()
        {
            var control = new Control {Size = new Vector2(100, 100)};
            var child = new Control {AnchorRight = 1, AnchorBottom = 1};
            control.AddChild(child);
            Assert.That(child.Size, Is.EqualTo(new Vector2(100, 100)));
            Assert.That(child.Position, Is.EqualTo(Vector2.Zero));

            child.AnchorLeft = 0.5f;
            Assert.That(child.Position, Is.EqualTo(new Vector2(50, 0)));
            Assert.That(child.Size, Is.EqualTo(new Vector2(50, 100)));
            child.AnchorTop = 0.5f;

            Assert.That(child.Position, Is.EqualTo(new Vector2(50, 50)));
            Assert.That(child.Size, Is.EqualTo(new Vector2(50, 50)));
        }

        [Test]
        public void TestMarginLayoutMinimumSize()
        {
            var control = new Control {Size = new Vector2(100, 100)};
            var child = new Control
            {
                CustomMinimumSize = new Vector2(50, 50),
                MarginRight = 20,
                MarginBottom = 20
            };

            control.AddChild(child);
            Assert.That(child.Size, Is.EqualTo(new Vector2(50, 50)));
            Assert.That(child.MarginRight, Is.EqualTo(20));
            Assert.That(child.MarginBottom, Is.EqualTo(20));
        }

        [Test]
        public void TestMarginAnchorLayout()
        {
            var control = new Control {Size = new Vector2(100, 100)};
            var child = new Control
            {
                MarginRight = -10,
                MarginBottom = -10,
                MarginTop = 10,
                MarginLeft = 10,
                AnchorRight = 1,
                AnchorBottom = 1
            };

            control.AddChild(child);
            Assert.That(child.Position, Is.EqualTo(new Vector2(10, 10)));
            Assert.That(child.Size, Is.EqualTo(new Vector2(80, 80)));
        }

        [Test]
        public void TestLayoutSet()
        {
            var control = new Control {Size = new Vector2(100, 100)};
            var child = new Control();

            control.AddChild(child);

            Assert.That(child.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(child.Size, Is.EqualTo(Vector2.Zero));

            child.Size = new Vector2(50, 50);
            Assert.That(child.Size, Is.EqualTo(new Vector2(50, 50)));
            Assert.That(child.Position, Is.EqualTo(Vector2.Zero));

            Assert.That(child.MarginTop, Is.EqualTo(0));
            Assert.That(child.MarginLeft, Is.EqualTo(0));
            Assert.That(child.MarginRight, Is.EqualTo(50));
            Assert.That(child.MarginBottom, Is.EqualTo(50));

            child.Position = new Vector2(50, 50);
            Assert.That(child.Size, Is.EqualTo(new Vector2(50, 50)));
            Assert.That(child.Position, Is.EqualTo(new Vector2(50, 50)));

            Assert.That(child.MarginTop, Is.EqualTo(50));
            Assert.That(child.MarginLeft, Is.EqualTo(50));
            Assert.That(child.MarginRight, Is.EqualTo(100));
            Assert.That(child.MarginBottom, Is.EqualTo(100));
        }

        [Test]
        public void TestLayoutSetMinSizeConstrained()
        {
            // Test changing a Control Size to a new value,
            // when the old value was minsize (due to margins trying to go lower)
            var control = new Control {Size = new Vector2(100, 100)};
            var child = new Control {CustomMinimumSize = new Vector2(30, 30)};
            control.AddChild(child);

            Assert.That(child.Size, Is.EqualTo(new Vector2(30, 30)));

            child.Size = new Vector2(50, 50);
            Assert.That(child.Size, Is.EqualTo(new Vector2(50, 50)));
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

        // Test that a control grows its size instead of position by default. (GrowDirection.End)
        [Test]
        public void TestGrowEnd()
        {
            var parent = new Control {Size = (50, 50)};
            var child = new Control();
            parent.AddChild(child);

            // Child should be at 0,0.
            Assert.That(child.Position, Is.EqualTo(Vector2.Zero));

            // Making the child have a bigger minimum size should grow it to the bottom left.
            // i.e. size should change, position should not.
            child.CustomMinimumSize = (100, 100);

            Assert.That(child.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(child.Size, Is.EqualTo(new Vector2(100, 100)));
        }

        // Test GrowDirection.Begin
        [Test]
        public void TestGrowBegin()
        {
            var parent = new Control {Size = (50, 50)};
            var child = new Control {GrowHorizontal = Control.GrowDirection.Begin};
            parent.AddChild(child);

            // Child should be at 0,0.
            Assert.That(child.Position, Is.EqualTo(Vector2.Zero));

            // Making the child have a bigger minimum size should grow it to the bottom right.
            // i.e. size should change, position should not.
            child.CustomMinimumSize = (100, 100);

            Assert.That(child.Position, Is.EqualTo(new Vector2(-100, 0)));
            Assert.That(child.Size, Is.EqualTo(new Vector2(100, 100)));
        }

        // Test GrowDirection.Both
        [Test]
        public void TestGrowBoth()
        {
            var parent = new Control {Size = (50, 50)};
            var child = new Control {GrowHorizontal = Control.GrowDirection.Both};
            parent.AddChild(child);

            // Child should be at 0,0.
            Assert.That(child.Position, Is.EqualTo(Vector2.Zero));

            child.CustomMinimumSize = (100, 100);

            Assert.That(child.Position, Is.EqualTo(new Vector2(-50, 0)));
            Assert.That(child.Size, Is.EqualTo(new Vector2(100, 100)));
        }

        // Test that changing a grow direction updates the position correctly.
        [Test]
        public void TestGrowDirectionChange()
        {
            var parent = new Control {Size = (50, 50)};
            var child = new Control();
            parent.AddChild(child);

            // Child should be at 0,0.
            Assert.That(child.Position, Is.EqualTo(Vector2.Zero));

            child.CustomMinimumSize = (100, 100);

            Assert.That(child.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(child.Size, Is.EqualTo(new Vector2(100, 100)));

            child.GrowHorizontal = Control.GrowDirection.Begin;

            Assert.That(child.Position, Is.EqualTo(new Vector2(-100, 0)));
        }
    }
}
