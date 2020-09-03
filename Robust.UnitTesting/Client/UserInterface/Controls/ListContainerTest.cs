using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Client.UserInterface.Controls
{
    [TestFixture]
    [TestOf(typeof(BoxContainer))]
    public class ListContainerTest : RobustUnitTest
    {
        public override UnitTestProject Project => UnitTestProject.Client;

        [Test]
        public void TestAddOneChild()
        {
            var root = new Control
            {
                Name = "UIRoot",
                MouseFilter = Control.MouseFilterMode.Ignore,
                IsInsideTree = true
            };
            var listContainer = new ListContainer {CustomMinimumSize = (20, 10)};
            var control = new Control {CustomMinimumSize = (20, 10)};
            root.AddChild(listContainer);
            root.ForceRunLayoutUpdate();

            Assert.That(listContainer.CombinedMinimumSize, Is.EqualTo(new Vector2(20, 10)));

            Assert.That(listContainer.ChildCount, Is.EqualTo(1)); // scrollBar

            listContainer.AddChild(control);
            root.ForceRunLayoutUpdate();

            Assert.That(listContainer.ChildCount, Is.EqualTo(2)); // scrollBar and control
            Assert.That(listContainer.StartIndex, Is.EqualTo(0));
            Assert.That(listContainer.EndIndex, Is.EqualTo(0));
            Assert.That(control.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(control.Size, Is.EqualTo(new Vector2(20, 10)));
        }

        [Test]
        public void TestAddTwoChildren()
        {
            var root = new Control
            {
                Name = "UIRoot",
                MouseFilter = Control.MouseFilterMode.Ignore,
                IsInsideTree = true
            };
            var listContainer = new ListContainer {CustomMinimumSize = (20, 20)};
            var control1 = new Control {CustomMinimumSize = (20, 10)};
            var control2 = new Control {CustomMinimumSize = (20, 10)};
            root.AddChild(listContainer);
            root.ForceRunLayoutUpdate();

            Assert.That(listContainer.CombinedMinimumSize, Is.EqualTo(new Vector2(20, 20)));

            listContainer.AddChild(control1);
            listContainer.AddChild(control2);
            root.ForceRunLayoutUpdate();

            Assert.That(listContainer.ChildCount, Is.EqualTo(3)); // scrollBar and control
            Assert.That(listContainer.StartIndex, Is.EqualTo(0));
            Assert.That(listContainer.EndIndex, Is.EqualTo(1));
            Assert.That(control1.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(control1.Size, Is.EqualTo(new Vector2(20, 10)));
            Assert.That(control2.Position, Is.EqualTo(new Vector2(0, 10)));
            Assert.That(control2.Size, Is.EqualTo(new Vector2(20, 10)));
            Assert.That(listContainer.VScrollBar.Visible, Is.False);
        }

        [Test]
        public void TestItemRemoval()
        {
            var root = new Control
            {
                Name = "UIRoot",
                MouseFilter = Control.MouseFilterMode.Ignore,
                IsInsideTree = true
            };
            var listContainer = new ListContainer {CustomMinimumSize = (20, 20)};
            var control1 = new Control {CustomMinimumSize = (20, 10)};
            var control2 = new Control {CustomMinimumSize = (20, 10)};
            root.AddChild(listContainer);
            root.ForceRunLayoutUpdate();

            listContainer.AddChild(control1);
            listContainer.AddChild(control2);
            root.ForceRunLayoutUpdate();

            listContainer.RemoveItem(control1);
            listContainer.RemoveItem(control2);
            root.ForceRunLayoutUpdate();

            Assert.That(listContainer.ChildCount, Is.EqualTo(1));
            Assert.That(listContainer.ItemCount, Is.Zero);
            Assert.That(listContainer.VScrollBar.Visible, Is.False);
        }

        [Test]
        public void TestChildRemoval()
        {
            var root = new Control
            {
                Name = "UIRoot",
                MouseFilter = Control.MouseFilterMode.Ignore,
                IsInsideTree = true
            };
            var listContainer = new ListContainer {CustomMinimumSize = (20, 20)};
            var control1 = new Control {CustomMinimumSize = (20, 10)};
            var control2 = new Control {CustomMinimumSize = (20, 10)};
            root.AddChild(listContainer);
            root.ForceRunLayoutUpdate();

            listContainer.AddChild(control1);
            listContainer.AddChild(control2);
            root.ForceRunLayoutUpdate();

            listContainer.RemoveChild(control1);
            listContainer.RemoveChild(control2);
            root.ForceRunLayoutUpdate();

            Assert.That(listContainer.ChildCount, Is.EqualTo(1));
            Assert.That(listContainer.ItemCount, Is.Zero);
            Assert.That(listContainer.VScrollBar.Visible, Is.False);
        }

        [Test]
        public void TestScroll()
        {
            var root = new Control
            {
                Name = "UIRoot",
                MouseFilter = Control.MouseFilterMode.Ignore,
                IsInsideTree = true
            };
            var listContainer = new ListContainer {CustomMinimumSize = (20, 20)};
            var control1 = new Control {CustomMinimumSize = (20, 10)};
            var control2 = new Control {CustomMinimumSize = (20, 10)};
            var control3 = new Control {CustomMinimumSize = (20, 10)};
            var control4 = new Control {CustomMinimumSize = (20, 10)};
            root.AddChild(listContainer);
            root.ForceRunLayoutUpdate();

            Assert.That(listContainer.CombinedMinimumSize, Is.EqualTo(new Vector2(20, 20)));

            listContainer.AddChild(control1);
            listContainer.AddChild(control2);
            listContainer.AddChild(control3);
            listContainer.AddChild(control4);
            root.ForceRunLayoutUpdate();

            Assert.That(listContainer.ItemCount, Is.EqualTo(4));
            Assert.That(listContainer.ChildCount, Is.EqualTo(3)); // scrollBar and two visible
            Assert.That(listContainer.StartIndex, Is.EqualTo(0));
            Assert.That(listContainer.EndIndex, Is.EqualTo(1));
            Assert.That(control1.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(control1.Size, Is.EqualTo(new Vector2(20, 10)));
            Assert.That(control2.Position, Is.EqualTo(new Vector2(0, 10)));
            Assert.That(control2.Size, Is.EqualTo(new Vector2(20, 10)));
            Assert.That(control3.IsInsideTree, Is.False);
            Assert.That(control4.IsInsideTree, Is.False);
            Assert.That(listContainer.VScrollBar.Visible, Is.True);

            listContainer.VScrollBar.Value = 15;
            root.ForceRunLayoutUpdate();

            Assert.That(listContainer.ChildCount, Is.EqualTo(4)); // scrollBar and three visible
            Assert.That(listContainer.StartIndex, Is.EqualTo(1));
            Assert.That(listContainer.EndIndex, Is.EqualTo(3));
            Assert.That(control1.IsInsideTree, Is.False);
            Assert.That(control2.Position, Is.EqualTo(new Vector2(0, -5)));
            Assert.That(control3.Position, Is.EqualTo(new Vector2(0, 5)));
            Assert.That(control4.Position, Is.EqualTo(new Vector2(0, 15)));
            Assert.That(listContainer.VScrollBar.Visible, Is.True);
        }

        [Test]
        public void TestPadding()
        {
            var root = new Control
            {
                Name = "UIRoot",
                MouseFilter = Control.MouseFilterMode.Ignore,
                IsInsideTree = true
            };
            var listContainer = new ListContainer {
                CustomMinimumSize = (20, 20),
                SeparationOverride = 2
            };
            var control1 = new Control {CustomMinimumSize = (20, 10)};
            var control2 = new Control {CustomMinimumSize = (20, 10)};
            var control3 = new Control {CustomMinimumSize = (20, 10)};
            root.AddChild(listContainer);
            root.ForceRunLayoutUpdate();

            Assert.That(listContainer.CombinedMinimumSize, Is.EqualTo(new Vector2(20, 20)));

            listContainer.AddChild(control1);
            listContainer.AddChild(control2);
            listContainer.AddChild(control3);
            root.ForceRunLayoutUpdate();

            Assert.That(control1.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(control1.Size, Is.EqualTo(new Vector2(20, 10)));
            Assert.That(control2.Position, Is.EqualTo(new Vector2(0, 12)));
            Assert.That(control2.Size, Is.EqualTo(new Vector2(20, 10)));
            Assert.That(control3.IsInsideTree, Is.False);
            Assert.That(listContainer.VScrollBar.Visible, Is.True);

            listContainer.VScrollBar.Value = 5;
            root.ForceRunLayoutUpdate();

            Assert.That(listContainer.ChildCount, Is.EqualTo(4)); // scrollBar and three visible
            Assert.That(listContainer.StartIndex, Is.EqualTo(0));
            Assert.That(listContainer.EndIndex, Is.EqualTo(2));
            Assert.That(control1.Position, Is.EqualTo(new Vector2(0, -5)));
            Assert.That(control2.Position, Is.EqualTo(new Vector2(0, 7)));
            Assert.That(control3.Position, Is.EqualTo(new Vector2(0, 19)));
            Assert.That(listContainer.VScrollBar.Visible, Is.True);
        }
    }
}
