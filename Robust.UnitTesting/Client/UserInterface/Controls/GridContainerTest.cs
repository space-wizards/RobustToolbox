using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Client.UserInterface.Controls
{
    [TestFixture]
    [TestOf(typeof(GridContainer))]
    public class GridContainerTest : RobustUnitTest
    {
        public override UnitTestProject Project => UnitTestProject.Client;

        [TestCase(true)]
        [TestCase(false)]
        public void TestBasic(bool limitByCount)
        {
            var grid = limitByCount ? new GridContainer {Columns = 2} : new GridContainer { MaxWidth = 125};
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

            Assert.That(grid.CombinedMinimumSize, Is.EqualTo(new Vector2(104, 158)));

            Assert.That(child1.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(child2.Position, Is.EqualTo(new Vector2(54, 0)));
            Assert.That(child3.Position, Is.EqualTo(new Vector2(0, 54)));
            Assert.That(child4.Position, Is.EqualTo(new Vector2(54, 54)));
            Assert.That(child5.Position, Is.EqualTo(new Vector2(0, 108)));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestBasicRows(bool limitByCount)
        {
            var grid = limitByCount ? new GridContainer {Rows = 2}
                : new GridContainer {MaxHeight = 125};
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
        public void TestUnevenLimitSize()
        {
            // when uneven sizes are used and limiting by size, they should all be treated as equal size cells based on the
            // max minwidth / minheight among them.
            // Note that when limiting by count, the behavior is different - rows and columns are individually
            // expanded based on the max size of their elements
            var grid = new GridContainer { MaxWidth = 125};
            var child1 = new Control {CustomMinimumSize = (12, 24)};
            var child2 = new Control {CustomMinimumSize = (30, 50)};
            var child3 = new Control {CustomMinimumSize = (40, 20)};
            var child4 = new Control {CustomMinimumSize = (20, 12)};
            var child5 = new Control {CustomMinimumSize = (50, 10)};

            grid.AddChild(child1);
            grid.AddChild(child2);
            grid.AddChild(child3);
            grid.AddChild(child4);
            grid.AddChild(child5);

            grid.ForceRunLayoutUpdate();

            Assert.That(grid.CombinedMinimumSize, Is.EqualTo(new Vector2(104, 158)));

            Assert.That(child1.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(child2.Position, Is.EqualTo(new Vector2(54, 0)));
            Assert.That(child3.Position, Is.EqualTo(new Vector2(0, 54)));
            Assert.That(child4.Position, Is.EqualTo(new Vector2(54, 54)));
            Assert.That(child5.Position, Is.EqualTo(new Vector2(0, 108)));
        }

        [Test]
        public void TestUnevenLimitSizeRows()
        {
            var grid = new GridContainer {MaxHeight = 125};
            var child1 = new Control {CustomMinimumSize = (12, 2)};
            var child2 = new Control {CustomMinimumSize = (5, 23)};
            var child3 = new Control {CustomMinimumSize = (42, 4)};
            var child4 = new Control {CustomMinimumSize = (2, 50)};
            var child5 = new Control {CustomMinimumSize = (50, 34)};

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

        [TestCase(true)]
        [TestCase(false)]
        public void TestBasicBackwards(bool limitByCount)
        {
            var grid = limitByCount ? new GridContainer {Columns = 2, ExpandBackwards = true}
                : new GridContainer { MaxWidth = 125, ExpandBackwards = true};
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

            Assert.That(grid.CombinedMinimumSize, Is.EqualTo(new Vector2(104, 158)));

            Assert.That(child1.Position, Is.EqualTo(new Vector2(0, 108)));
            Assert.That(child2.Position, Is.EqualTo(new Vector2(54, 108)));
            Assert.That(child3.Position, Is.EqualTo(new Vector2(0, 54)));
            Assert.That(child4.Position, Is.EqualTo(new Vector2(54, 54)));
            Assert.That(child5.Position, Is.EqualTo(Vector2.Zero));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestBasicRowsBackwards(bool limitByCount)
        {
            var grid = limitByCount ? new GridContainer {Rows = 2, ExpandBackwards = true}
                : new GridContainer {MaxHeight = 125, ExpandBackwards = true};
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

            Assert.That(child1.Position, Is.EqualTo(new Vector2(108, 0)));
            Assert.That(child2.Position, Is.EqualTo(new Vector2(108, 54)));
            Assert.That(child3.Position, Is.EqualTo(new Vector2(54, 0)));
            Assert.That(child4.Position, Is.EqualTo(new Vector2(54, 54)));
            Assert.That(child5.Position, Is.EqualTo(Vector2.Zero));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestExpand(bool limitByCount)
        {
            // in the presence of a MaxWidth with expanding elements, the
            // pre-expanded size should be used to determine the size of each "cell", and then expansion
            // happens within the defined control size
            var grid = limitByCount ? new GridContainer {Columns = 2, Size = (200, 200)}
                : new GridContainer {MaxWidth = 125, Size = (200, 200)} ;
            var child1 = new Control {CustomMinimumSize = (50, 50), SizeFlagsHorizontal = Control.SizeFlags.FillExpand};
            var child2 = new Control {CustomMinimumSize = (50, 50)};
            var child3 = new Control {CustomMinimumSize = (50, 50)};
            var child4 = new Control {CustomMinimumSize = (50, 50), SizeFlagsVertical = Control.SizeFlags.FillExpand};
            var child5 = new Control {CustomMinimumSize = (50, 50)};

            grid.AddChild(child1);
            grid.AddChild(child2);
            grid.AddChild(child3);
            grid.AddChild(child4);
            grid.AddChild(child5);

            grid.ForceRunLayoutUpdate();

            Assert.That(child1.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(child1.Size, Is.EqualTo(new Vector2(146, 50)));
            Assert.That(child2.Position, Is.EqualTo(new Vector2(150, 0)));
            Assert.That(child2.Size, Is.EqualTo(new Vector2(50, 50)));
            Assert.That(child3.Position, Is.EqualTo(new Vector2(0, 54)));
            Assert.That(child3.Size, Is.EqualTo(new Vector2(146, 92)));
            Assert.That(child4.Position, Is.EqualTo(new Vector2(150, 54)));
            Assert.That(child4.Size, Is.EqualTo(new Vector2(50, 92)));
            Assert.That(child5.Position, Is.EqualTo(new Vector2(0, 150)));
            Assert.That(child5.Size, Is.EqualTo(new Vector2(146, 50)));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestExpandRows(bool limitByCount)
        {
            var grid = limitByCount ? new GridContainer {Rows = 2, Size = (200, 200)}
                : new GridContainer {MaxHeight = 125,  Size = (200, 200)};
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

        [TestCase(true)]
        [TestCase(false)]
        public void TestRowCount(bool limitByCount)
        {
            var grid = limitByCount ? new GridContainer {Columns = 2}
                : new GridContainer {MaxWidth = 125};
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

            Assert.That(grid.Rows, Is.EqualTo(3));

            grid.RemoveChild(child5);

            Assert.That(grid.Rows, Is.EqualTo(2));

            grid.DisposeAllChildren();

            Assert.That(grid.Rows, Is.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestColumnCountRows(bool limitByCount)
        {
            var grid = limitByCount ? new GridContainer {Rows = 2}
                : new GridContainer{MaxHeight = 125};
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
