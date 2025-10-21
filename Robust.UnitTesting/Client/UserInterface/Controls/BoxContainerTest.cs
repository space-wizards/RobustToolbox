using System.Numerics;
using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Robust.UnitTesting.Client.UserInterface.Controls
{
    [TestFixture]
    [TestOf(typeof(BoxContainer))]
    public sealed class BoxContainerTest : RobustUnitTest
    {
        public override UnitTestProject Project => UnitTestProject.Client;

        [Test]
        public void TestLayoutBasic()
        {
            var root = new LayoutContainer();
            var boxContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                MinSize = new Vector2(50, 60)
            };
            var control1 = new Control {MinSize = new Vector2(20, 20)};
            var control2 = new Control {MinSize = new Vector2(30, 30)};

            root.AddChild(boxContainer);

            boxContainer.AddChild(control1);
            boxContainer.AddChild(control2);

            root.Arrange(new UIBox2(0, 0, 50, 60));

            Assert.That(control1.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(control1.Size, Is.EqualTo(new Vector2(50, 20)));
            Assert.That(control2.Position, Is.EqualTo(new Vector2(0, 20)));
            Assert.That(control2.Size, Is.EqualTo(new Vector2(50, 30)));
            Assert.That(boxContainer.DesiredSize, Is.EqualTo(new Vector2(50, 60)));
        }

        [Test]
        public void TestLayoutExpand()
        {
            var root = new LayoutContainer();
            var boxContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                MinSize = new Vector2(50, 60)
            };
            var control1 = new Control
            {
                VerticalExpand = true
            };
            var control2 = new Control {MinSize = new Vector2(30, 30)};

            boxContainer.AddChild(control1);
            boxContainer.AddChild(control2);

            root.AddChild(boxContainer);

            root.Arrange(new UIBox2(0, 0, 100, 100));

            Assert.That(control1.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(control1.Size, Is.EqualTo(new Vector2(50, 30)));
            Assert.That(control2.Position, Is.EqualTo(new Vector2(0, 30)));
            Assert.That(control2.Size, Is.EqualTo(new Vector2(50, 30)));
            Assert.That(boxContainer.DesiredSize, Is.EqualTo(new Vector2(50, 60)));
        }

        [Test]
        public void TestCalcMinSize()
        {
            var boxContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };
            var control1 = new Control
            {
                MinSize = new Vector2(50, 30)
            };
            var control2 = new Control {MinSize = new Vector2(30, 50)};

            boxContainer.AddChild(control1);
            boxContainer.AddChild(control2);

            boxContainer.Measure(new Vector2(100, 100));

            Assert.That(boxContainer.DesiredSize, Is.EqualTo(new Vector2(50, 80)));
        }

        [Test]
        public void TestTwoExpand()
        {
            var root = new LayoutContainer();
            var boxContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                MinSize = new Vector2(30, 80)
            };
            var control1 = new Control
            {
                VerticalExpand = true,
            };
            var control2 = new Control
            {
                VerticalExpand = true,
            };
            var control3 = new Control {MinSize = new Vector2(0, 50)};

            root.AddChild(boxContainer);

            boxContainer.AddChild(control1);
            boxContainer.AddChild(control3);
            boxContainer.AddChild(control2);

            root.Arrange(new UIBox2(0, 0, 250, 250));

            Assert.That(control1.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(control1.Size, Is.EqualTo(new Vector2(30, 15)));
            Assert.That(control3.Position, Is.EqualTo(new Vector2(0, 15)));
            Assert.That(control3.Size, Is.EqualTo(new Vector2(30, 50)));
            Assert.That(control2.Position, Is.EqualTo(new Vector2(0, 65)));
            Assert.That(control2.Size, Is.EqualTo(new Vector2(30, 15)));
        }

        [Test]
        public void TestTwoExpandRatio()
        {
            var boxContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                SetSize = new Vector2(100, 10),
                Children =
                {
                    new Control
                    {
                        MinWidth = 10,
                        HorizontalExpand = true,
                        SizeFlagsStretchRatio = 20,
                    },
                    new Control
                    {
                        MinWidth = 10,
                        HorizontalExpand = true,
                        SizeFlagsStretchRatio = 80
                    }
                }
            };

            boxContainer.Arrange(UIBox2.FromDimensions(Vector2.Zero, boxContainer.SetSize));

            Assert.Multiple(() =>
            {
                Assert.That(boxContainer.GetChild(0).Width, Is.EqualTo(20));
                Assert.That(boxContainer.GetChild(1).Width, Is.EqualTo(80));
            });
        }

        [Test]
        public void TestTwoExpandOneSmall()
        {
            var boxContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                SetSize = new Vector2(100, 10),
                Children =
                {
                    new Control
                    {
                        MinWidth = 30,
                        HorizontalExpand = true,
                        SizeFlagsStretchRatio = 20,
                    },
                    new Control
                    {
                        MinWidth = 30,
                        HorizontalExpand = true,
                        SizeFlagsStretchRatio = 80
                    }
                }
            };

            boxContainer.Arrange(UIBox2.FromDimensions(Vector2.Zero, boxContainer.SetSize));

            Assert.Multiple(() =>
            {
                Assert.That(boxContainer.GetChild(0).Width, Is.EqualTo(30));
                Assert.That(boxContainer.GetChild(1).Width, Is.EqualTo(70));
            });
        }
    }
}
