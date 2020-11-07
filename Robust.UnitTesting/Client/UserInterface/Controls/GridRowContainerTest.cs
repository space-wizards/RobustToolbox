using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Client.UserInterface.Controls
{
    [TestFixture]
    [TestOf(typeof(GridRowContainer))]
    public class GridRowContainerTest : RobustUnitTest
    {
        public override UnitTestProject Project => UnitTestProject.Client;

        [Test]
        public void TestBasic()
        {
            var grid = new GridRowContainer {Rows = 2};
            var child1 = new Control {CustomMinimumSize = (50, 50)};
            var child2 = new Control {CustomMinimumSize = (50, 50)};
            var child3 = new Control {CustomMinimumSize = (50, 50)};
            var child4 = new Control {CustomMinimumSize = (50, 50)};
            var child5 = new Control {CustomMinimumSize = (50, 50)};

            grid.AddChild(child1);
            grid.AddChild(child2);
            grid.AddChild(child3);
            grid.AddChild(child4);
            grid.AddChild(child5);

            grid.ForceRunLayoutUpdate();

            Assert.That(grid.CombinedMinimumSize, Is.EqualTo(new Vector2(158, 104)));

            Assert.That(child1.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(child2.Position, Is.EqualTo(new Vector2(0, 54)));
            Assert.That(child3.Position, Is.EqualTo(new Vector2(54, 0)));
            Assert.That(child4.Position, Is.EqualTo(new Vector2(54, 54)));
            Assert.That(child5.Position, Is.EqualTo(new Vector2(108, 0)));
        }

        [Test]
        public void TestExpand()
        {
            var grid = new GridRowContainer {Rows = 2, Size = (200, 200)};
            var child1 = new Control {CustomMinimumSize = (50, 50), SizeFlagsVertical = Control.SizeFlags.FillExpand};
            var child2 = new Control {CustomMinimumSize = (50, 50)};
            var child3 = new Control {CustomMinimumSize = (50, 50)};
            var child4 = new Control {CustomMinimumSize = (50, 50), SizeFlagsHorizontal = Control.SizeFlags.FillExpand};
            var child5 = new Control {CustomMinimumSize = (50, 50)};

            grid.AddChild(child1);
            grid.AddChild(child2);
            grid.AddChild(child3);
            grid.AddChild(child4);
            grid.AddChild(child5);

            grid.ForceRunLayoutUpdate();

            Assert.That(child1.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(child1.Size, Is.EqualTo(new Vector2(50, 146)));
            Assert.That(child2.Position, Is.EqualTo(new Vector2(0, 150)));
            Assert.That(child2.Size, Is.EqualTo(new Vector2(50, 50)));
            Assert.That(child3.Position, Is.EqualTo(new Vector2(54, 0)));
            Assert.That(child3.Size, Is.EqualTo(new Vector2(92, 146)));
            Assert.That(child4.Position, Is.EqualTo(new Vector2(54, 150)));
            Assert.That(child4.Size, Is.EqualTo(new Vector2(92, 50)));
            Assert.That(child5.Position, Is.EqualTo(new Vector2(150, 0)));
            Assert.That(child5.Size, Is.EqualTo(new Vector2(50, 146)));
        }

        [Test]
        public void TestColumnCount()
        {
            var grid = new GridRowContainer {Rows = 2};
            var child1 = new Control {CustomMinimumSize = (50, 50)};
            var child2 = new Control {CustomMinimumSize = (50, 50)};
            var child3 = new Control {CustomMinimumSize = (50, 50)};
            var child4 = new Control {CustomMinimumSize = (50, 50)};
            var child5 = new Control {CustomMinimumSize = (50, 50)};

            grid.AddChild(child1);
            grid.AddChild(child2);
            grid.AddChild(child3);
            grid.AddChild(child4);
            grid.AddChild(child5);

            Assert.That(grid.Columns, Is.EqualTo(3));

            grid.RemoveChild(child5);

            Assert.That(grid.Columns, Is.EqualTo(2));

            grid.DisposeAllChildren();

            Assert.That(grid.Columns, Is.EqualTo(1));
        }
    }
}
