using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Client.UserInterface.Controls
{
    [TestFixture]
    [TestOf(typeof(BoxContainer))]
    public class BoxContainerTest : RobustUnitTest
    {
        public override UnitTestProject Project => UnitTestProject.Client;

        [Test]
        public void TestLayoutBasic()
        {
            var root = new LayoutContainer();
            var boxContainer = new VBoxContainer {CustomMinimumSize = (50, 60)};
            var control1 = new Control {CustomMinimumSize = (20, 20)};
            var control2 = new Control {CustomMinimumSize = (30, 30)};

            root.AddChild(boxContainer);

            boxContainer.AddChild(control1);
            boxContainer.AddChild(control2);

            root.ForceRunLayoutUpdate();

            Assert.That(control1.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(control1.Size, Is.EqualTo(new Vector2(50, 20)));
            // Keep the separation in mind!
            Assert.That(control2.Position, Is.EqualTo(new Vector2(0, 21)));
            Assert.That(control2.Size, Is.EqualTo(new Vector2(50, 30)));
            Assert.That(boxContainer.CombinedMinimumSize, Is.EqualTo(new Vector2(50, 60)));
        }

        [Test]
        public void TestLayoutExpand()
        {
            var root = new LayoutContainer();
            var boxContainer = new VBoxContainer {CustomMinimumSize = (50, 60)};
            var control1 = new Control
            {
                SizeFlagsVertical = Control.SizeFlags.FillExpand
            };
            var control2 = new Control {CustomMinimumSize = (30, 30)};

            boxContainer.AddChild(control1);
            boxContainer.AddChild(control2);

            root.AddChild(boxContainer);

            root.ForceRunLayoutUpdate();

            Assert.That(control1.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(control1.Size, Is.EqualTo(new Vector2(50, 29)));
            // Keep the separation in mind!
            Assert.That(control2.Position, Is.EqualTo(new Vector2(0, 30)));
            Assert.That(control2.Size, Is.EqualTo(new Vector2(50, 30)));
            Assert.That(boxContainer.CombinedMinimumSize, Is.EqualTo(new Vector2(50, 60)));
        }

        [Test]
        public void TestCalcMinSize()
        {
            var boxContainer = new VBoxContainer();
            var control1 = new Control
            {
                CustomMinimumSize = (50, 30)
            };
            var control2 = new Control {CustomMinimumSize = (30, 50)};

            boxContainer.AddChild(control1);
            boxContainer.AddChild(control2);

            Assert.That(boxContainer.CombinedMinimumSize, Is.EqualTo(new Vector2(50, 81)));
        }

        [Test]
        public void TestTwoExpand()
        {
            var root = new LayoutContainer();
            var boxContainer = new VBoxContainer {CustomMinimumSize = (30, 82)};
            var control1 = new Control
            {
                SizeFlagsVertical = Control.SizeFlags.FillExpand
            };
            var control2 = new Control
            {
                SizeFlagsVertical = Control.SizeFlags.FillExpand
            };
            var control3 = new Control {CustomMinimumSize = (0, 50)};

            root.AddChild(boxContainer);

            boxContainer.AddChild(control1);
            boxContainer.AddChild(control3);
            boxContainer.AddChild(control2);

            root.ForceRunLayoutUpdate();

            Assert.That(control1.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(control1.Size, Is.EqualTo(new Vector2(30, 15)));
            Assert.That(control3.Position, Is.EqualTo(new Vector2(0, 16)));
            Assert.That(control3.Size, Is.EqualTo(new Vector2(30, 50)));
            Assert.That(control2.Position, Is.EqualTo(new Vector2(0, 67)));
            Assert.That(control2.Size, Is.EqualTo(new Vector2(30, 15)));
        }
    }
}
