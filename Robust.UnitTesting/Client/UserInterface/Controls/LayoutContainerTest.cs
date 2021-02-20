using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Client.UserInterface.Controls
{
    [TestFixture]
    [TestOf(typeof(LayoutContainer))]
    public class LayoutContainerTest : RobustUnitTest
    {
        public override UnitTestProject Project => UnitTestProject.Client;

        [Test]
        public void TestMarginLayoutBasic()
        {
            var control = new LayoutContainer {Size = (100, 100)};
            var child = new Control();

            LayoutContainer.SetMarginRight(child, 5);
            LayoutContainer.SetMarginBottom(child, 5);
            control.AddChild(child);

            control.Arrange(new UIBox2(0, 0, 100, 100));

            Assert.That(child.Size, Is.EqualTo(new Vector2(5, 5)));
            Assert.That(child.Position, Is.EqualTo(Vector2.Zero));

            LayoutContainer.SetMarginTop(child, 3);
            LayoutContainer.SetMarginLeft(child, 3);

            control.InvalidateArrange();
            control.Arrange(new UIBox2(0, 0, 100, 100));

            Assert.That(child.Size, Is.EqualTo(new Vector2(2, 2)));
            Assert.That(child.Position, Is.EqualTo(new Vector2(3, 3)));
        }

        [Test]
        public void TestAnchorLayoutBasic()
        {
            var control = new LayoutContainer {Size = new Vector2(100, 100)};
            var child = new Control();
            LayoutContainer.SetAnchorRight(child, 1);
            LayoutContainer.SetAnchorBottom(child, 1);
            control.AddChild(child);

            control.Arrange(new UIBox2(0, 0, 100, 100));

            Assert.That(child.Size, Is.EqualTo(new Vector2(100, 100)));
            Assert.That(child.Position, Is.EqualTo(Vector2.Zero));

            LayoutContainer.SetAnchorLeft(child, 0.5f);
            control.InvalidateArrange();
            control.Arrange(new UIBox2(0, 0, 100, 100));
            Assert.That(child.Position, Is.EqualTo(new Vector2(50, 0)));
            Assert.That(child.Size, Is.EqualTo(new Vector2(50, 100)));
            LayoutContainer.SetAnchorTop(child, 0.5f);
            control.InvalidateArrange();
            control.Arrange(new UIBox2(0, 0, 100, 100));

            Assert.That(child.Position, Is.EqualTo(new Vector2(50, 50)));
            Assert.That(child.Size, Is.EqualTo(new Vector2(50, 50)));
        }

        [Test]
        public void TestMarginLayoutMinimumSize()
        {
            var control = new LayoutContainer {Size = new Vector2(100, 100)};
            var child = new Control
            {
                MinSize = new Vector2(50, 50),
            };

            LayoutContainer.SetMarginRight(child, 20);
            LayoutContainer.SetMarginBottom(child, 20);

            control.AddChild(child);
            control.Arrange(new UIBox2(0, 0, 100, 100));

            Assert.That(child.Size, Is.EqualTo(new Vector2(50, 50)));
        }

        [Test]
        public void TestMarginAnchorLayout()
        {
            var control = new LayoutContainer {Size = new Vector2(100, 100)};
            var child = new Control();

            LayoutContainer.SetMarginRight(child, -10);
            LayoutContainer.SetMarginBottom(child, -10);
            LayoutContainer.SetMarginTop(child, 10);
            LayoutContainer.SetMarginLeft(child, 10);
            LayoutContainer.SetAnchorRight(child, 1);
            LayoutContainer.SetAnchorBottom(child, 1);

            control.AddChild(child);
            control.InvalidateArrange();

            Assert.That(child.Position, Is.EqualTo(new Vector2(10, 10)));
            Assert.That(child.Size, Is.EqualTo(new Vector2(80, 80)));
        }

        // Test that a control grows its size instead of position by default. (GrowDirection.End)
        [Test]
        public void TestGrowEnd()
        {
            var parent = new LayoutContainer {Size = (50, 50)};
            var child = new Control();
            parent.AddChild(child);
            parent.Arrange(new UIBox2(0, 0, 50, 50));

            // Child should be at 0,0.
            Assert.That(child.Position, Is.EqualTo(Vector2.Zero));

            // Making the child have a bigger minimum size should grow it to the bottom size.
            // i.e. size should change, position should not.
            child.MinSize = (100, 100);
            parent.InvalidateArrange();
            parent.Arrange(new UIBox2(0, 0, 50, 50));

            Assert.That(child.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(child.Size, Is.EqualTo(new Vector2(100, 100)));
        }

        // Test GrowDirection.Begin
        [Test]
        public void TestGrowBegin()
        {
            var parent = new LayoutContainer();
            var child = new Control {SetSize = (100, 100)};

            LayoutContainer.SetGrowHorizontal(child, LayoutContainer.GrowDirection.Begin);
            LayoutContainer.SetAnchorRight(child, 1);

            parent.AddChild(child);
            parent.Arrange(new UIBox2(0, 0, 100, 100));

            // Child should be at 0,0.
            Assert.That(child.Position, Is.EqualTo(Vector2.Zero));

            // Right margin should make the child not have enough space and grow left.
            LayoutContainer.SetMarginRight(child, -100);
            parent.InvalidateArrange();
            parent.Arrange(new UIBox2(0, 0, 100, 100));

            Assert.That(child.Position, Is.EqualTo(new Vector2(-100, 0)));
            Assert.That(child.Size, Is.EqualTo(new Vector2(100, 100)));
        }

        // Test GrowDirection.Both
        [Test]
        public void TestGrowBoth()
        {
            var parent = new LayoutContainer {MinSize = (100, 100)};
            var child = new Control {SetSize = (100, 100)};

            LayoutContainer.SetGrowHorizontal(child, LayoutContainer.GrowDirection.Both);
            LayoutContainer.SetAnchorRight(child, 1);

            parent.AddChild(child);
            parent.Arrange(new UIBox2(0, 0, 100, 100));

            // Child should be at 0,0.
            Assert.That(child.Position, Is.EqualTo(Vector2.Zero));

            // Right margin should make the child not have enough space and grow left.
            LayoutContainer.SetMarginRight(child, -100);
            parent.InvalidateArrange();
            parent.Arrange(new UIBox2(0, 0, 100, 100));

            Assert.That(child.Position, Is.EqualTo(new Vector2(-50, 0)));
            Assert.That(child.Size, Is.EqualTo(new Vector2(100, 100)));
        }

        // Test that changing a grow direction updates the position correctly.
        [Test]
        public void TestGrowDirectionChange()
        {
            var parent = new LayoutContainer {MinSize = (100, 100)};
            var child = new Control();
            parent.AddChild(child);
            parent.Arrange(new UIBox2(0, 0, 100, 100));

            // Child should be at -100,0.
            Assert.That(child.Position, Is.EqualTo(new Vector2(0, 0)));

            child.MinSize = (100, 100);
            parent.InvalidateMeasure();
            parent.Arrange(new UIBox2(0, 0, 100, 100));

            Assert.That(child.Position, Is.EqualTo(new Vector2(0, 0)));
            Assert.That(child.Size, Is.EqualTo(new Vector2(100, 100)));

            LayoutContainer.SetGrowHorizontal(child, LayoutContainer.GrowDirection.Begin);
            parent.InvalidateArrange();
            parent.Arrange(new UIBox2(0, 0, 100, 100));

            Assert.That(child.Position, Is.EqualTo(new Vector2(-100, 0)));
        }
    }
}
