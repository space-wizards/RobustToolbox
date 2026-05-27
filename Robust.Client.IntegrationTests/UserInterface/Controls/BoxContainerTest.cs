using System.Numerics;
using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.UnitTesting;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Robust.Client.IntegrationTests.UserInterface.Controls;

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
        var control1 = new Control { MinSize = new Vector2(20, 20) };
        var control2 = new Control { MinSize = new Vector2(30, 30) };

        root.AddChild(boxContainer);

        boxContainer.AddChild(control1);
        boxContainer.AddChild(control2);

        root.Arrange(new UIBox2(0, 0, 50, 60));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(control1.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(control1.Size, Is.EqualTo(new Vector2(50, 20)));
            Assert.That(control2.Position, Is.EqualTo(new Vector2(0, 20)));
            Assert.That(control2.Size, Is.EqualTo(new Vector2(50, 30)));
            Assert.That(boxContainer.DesiredSize, Is.EqualTo(new Vector2(50, 60)));
        }
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
        var control2 = new Control { MinSize = new Vector2(30, 30) };

        boxContainer.AddChild(control1);
        boxContainer.AddChild(control2);

        root.AddChild(boxContainer);

        root.Arrange(new UIBox2(0, 0, 100, 100));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(control1.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(control1.Size, Is.EqualTo(new Vector2(50, 30)));
            Assert.That(control2.Position, Is.EqualTo(new Vector2(0, 30)));
            Assert.That(control2.Size, Is.EqualTo(new Vector2(50, 30)));
            Assert.That(boxContainer.DesiredSize, Is.EqualTo(new Vector2(50, 60)));
        }
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
        var control2 = new Control { MinSize = new Vector2(30, 50) };
        var control3 = new Control { MinSize = new Vector2(30, 50), Visible = false };

        boxContainer.AddChild(control1);
        boxContainer.AddChild(control2);
        boxContainer.AddChild(control3);

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
        var control3 = new Control { MinSize = new Vector2(0, 50) };
        var control4 = new Control { MinSize = new Vector2(0, 50), Visible = false };

        root.AddChild(boxContainer);

        boxContainer.AddChild(control1);
        boxContainer.AddChild(control3);
        boxContainer.AddChild(control2);
        boxContainer.AddChild(control4);

        root.Arrange(new UIBox2(0, 0, 250, 250));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(control1.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(control1.Size, Is.EqualTo(new Vector2(30, 15)));
            Assert.That(control3.Position, Is.EqualTo(new Vector2(0, 15)));
            Assert.That(control3.Size, Is.EqualTo(new Vector2(30, 50)));
            Assert.That(control2.Position, Is.EqualTo(new Vector2(0, 65)));
            Assert.That(control2.Size, Is.EqualTo(new Vector2(30, 15)));
            Assert.That(control4.Position, Is.EqualTo(new Vector2(0, 0)));
            Assert.That(control4.Size, Is.EqualTo(new Vector2(0, 0)));
        }
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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(boxContainer.GetChild(0).Width, Is.EqualTo(20));
            Assert.That(boxContainer.GetChild(1).Width, Is.EqualTo(80));
        }
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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(boxContainer.GetChild(0).Width, Is.EqualTo(30));
            Assert.That(boxContainer.GetChild(1).Width, Is.EqualTo(70));
        }
    }

    [Test]
    public void TestSeparationOverrides()
    {
        var boxContainer = new BoxContainer
        {
            SetSize = new Vector2(100, 10),
        };
        var child1 = new Control { SetSize = new Vector2(10, 10) };
        var child2 = new Control { SetSize = new Vector2(10, 10) };
        boxContainer.AddChild(child1);
        boxContainer.AddChild(child2);
        boxContainer.Arrange(UIBox2.FromDimensions(Vector2.Zero, boxContainer.SetSize));

        Assert.That(boxContainer.IsArrangeValid, Is.True);

        boxContainer.Stylesheet = new Stylesheet([
            Element<BoxContainer>()
                .Prop(StylePropertySeparation, 10)
        ]);
        boxContainer.ForceRunStyleUpdate();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(boxContainer.Separation, Is.EqualTo(10));
            Assert.That(boxContainer.IsMeasureValid, Is.False);
        }

        boxContainer.Arrange(UIBox2.FromDimensions(Vector2.Zero, boxContainer.SetSize));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(child1.Position, Is.EqualTo(new Vector2(0, 0)));
            Assert.That(child2.Position, Is.EqualTo(new Vector2(20, 0)));
        }

        boxContainer.Separation = 20;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(boxContainer.Separation, Is.EqualTo(20));
            Assert.That(boxContainer.IsMeasureValid, Is.False);
        }

        boxContainer.Arrange(UIBox2.FromDimensions(Vector2.Zero, boxContainer.SetSize));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(child1.Position, Is.EqualTo(new Vector2(0, 0)));
            Assert.That(child2.Position, Is.EqualTo(new Vector2(30, 0)));
        }

        boxContainer.Separation = 20;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(boxContainer.Separation, Is.EqualTo(20));
            Assert.That(boxContainer.IsMeasureValid, Is.True);
        }

        boxContainer.Arrange(UIBox2.FromDimensions(Vector2.Zero, boxContainer.SetSize));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(child1.Position, Is.EqualTo(new Vector2(0, 0)));
            Assert.That(child2.Position, Is.EqualTo(new Vector2(30, 0)));
        }

        boxContainer.Separation = null;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(boxContainer.Separation, Is.EqualTo(10));
            Assert.That(boxContainer.IsMeasureValid, Is.False);
        }

        boxContainer.Arrange(UIBox2.FromDimensions(Vector2.Zero, boxContainer.SetSize));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(child1.Position, Is.EqualTo(new Vector2(0, 0)));
            Assert.That(child2.Position, Is.EqualTo(new Vector2(20, 0)));
        }

        boxContainer.Stylesheet = new Stylesheet([]);
        boxContainer.ForceRunStyleUpdate();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(boxContainer.Separation, Is.Zero);
            Assert.That(boxContainer.IsMeasureValid, Is.False);
        }

        boxContainer.Arrange(UIBox2.FromDimensions(Vector2.Zero, boxContainer.SetSize));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(child1.Position, Is.EqualTo(new Vector2(0, 0)));
            Assert.That(child2.Position, Is.EqualTo(new Vector2(10, 0)));
        }
    }

    [Test]
    public void TestAlignOverrides()
    {
        var boxContainer = new BoxContainer
        {
            SetSize = new Vector2(100, 10),
        };
        var child = new Control { SetSize = new Vector2(10, 10) };
        boxContainer.AddChild(child);
        boxContainer.Arrange(UIBox2.FromDimensions(Vector2.Zero, boxContainer.SetSize));

        Assert.That(boxContainer.IsArrangeValid, Is.True);

        boxContainer.Stylesheet = new Stylesheet([
            Element<BoxContainer>()
                .Prop(StylePropertyAlignMode, AlignMode.Center)
        ]);
        boxContainer.ForceRunStyleUpdate();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(boxContainer.Align, Is.EqualTo(AlignMode.Center));
            Assert.That(boxContainer.IsArrangeValid, Is.False);
        }

        boxContainer.Arrange(UIBox2.FromDimensions(Vector2.Zero, boxContainer.SetSize));
        Assert.That(child.Position, Is.EqualTo(new Vector2(45, 0)));

        boxContainer.Align = AlignMode.End;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(boxContainer.Align, Is.EqualTo(AlignMode.End));
            Assert.That(boxContainer.IsMeasureValid, Is.True);
            Assert.That(boxContainer.IsArrangeValid, Is.False);
        }

        boxContainer.Arrange(UIBox2.FromDimensions(Vector2.Zero, boxContainer.SetSize));
        Assert.That(child.Position, Is.EqualTo(new Vector2(90, 0)));

        boxContainer.Align = AlignMode.End;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(boxContainer.Align, Is.EqualTo(AlignMode.End));
            Assert.That(boxContainer.IsMeasureValid, Is.True);
            Assert.That(boxContainer.IsArrangeValid, Is.True);
        }

        boxContainer.Arrange(UIBox2.FromDimensions(Vector2.Zero, boxContainer.SetSize));
        Assert.That(child.Position, Is.EqualTo(new Vector2(90, 0)));

        boxContainer.Align = (AlignMode)4;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(boxContainer.Align, Is.EqualTo((AlignMode)4));
        }

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            boxContainer.Arrange(UIBox2.FromDimensions(Vector2.Zero, boxContainer.SetSize)));

        boxContainer.Arrange(UIBox2.FromDimensions(Vector2.Zero, boxContainer.SetSize));
        Assert.That(child.Position, Is.EqualTo(new Vector2(90, 0)));

        boxContainer.Align = null;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(boxContainer.Align, Is.EqualTo(AlignMode.Center));
            Assert.That(boxContainer.IsMeasureValid, Is.True);
            Assert.That(boxContainer.IsArrangeValid, Is.False);
        }

        boxContainer.Arrange(UIBox2.FromDimensions(Vector2.Zero, boxContainer.SetSize));
        Assert.That(child.Position, Is.EqualTo(new Vector2(45, 0)));

        boxContainer.Stylesheet = new Stylesheet([]);
        boxContainer.ForceRunStyleUpdate();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(boxContainer.Align, Is.EqualTo(AlignMode.Begin));
            Assert.That(boxContainer.IsArrangeValid, Is.False);
        }

        boxContainer.Arrange(UIBox2.FromDimensions(Vector2.Zero, boxContainer.SetSize));
        Assert.That(child.Position, Is.EqualTo(new Vector2(0, 0)));
    }

    [Test]
    public void TestOrientationOverrides()
    {
        var boxContainer = new BoxContainer
        {
            SetSize = new Vector2(100, 100),
        };
        var child = new Control { SetSize = new Vector2(10, 10) };
        boxContainer.AddChild(child);
        boxContainer.Arrange(UIBox2.FromDimensions(Vector2.Zero, boxContainer.SetSize));

        Assert.That(boxContainer.IsArrangeValid, Is.True);

        boxContainer.Stylesheet = new Stylesheet([
            Element<BoxContainer>()
                .Prop(StylePropertyOrientation, LayoutOrientation.Vertical)
        ]);
        boxContainer.ForceRunStyleUpdate();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(boxContainer.Orientation, Is.EqualTo(LayoutOrientation.Vertical));
            Assert.That(boxContainer.IsMeasureValid, Is.False);
        }

        boxContainer.Arrange(UIBox2.FromDimensions(Vector2.Zero, boxContainer.SetSize));
        Assert.That(child.Position, Is.EqualTo(new Vector2(45, 0)));

        boxContainer.Orientation = LayoutOrientation.Horizontal;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(boxContainer.Orientation, Is.EqualTo(LayoutOrientation.Horizontal));
            Assert.That(boxContainer.IsMeasureValid, Is.False);
        }

        boxContainer.Arrange(UIBox2.FromDimensions(Vector2.Zero, boxContainer.SetSize));
        Assert.That(child.Position, Is.EqualTo(new Vector2(0, 45)));


        boxContainer.Orientation = LayoutOrientation.Horizontal;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(boxContainer.Orientation, Is.EqualTo(LayoutOrientation.Horizontal));
            Assert.That(boxContainer.IsMeasureValid, Is.True);
        }

        boxContainer.Arrange(UIBox2.FromDimensions(Vector2.Zero, boxContainer.SetSize));
        Assert.That(child.Position, Is.EqualTo(new Vector2(0, 45)));

        boxContainer.Orientation = null;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(boxContainer.Orientation, Is.EqualTo(LayoutOrientation.Vertical));
            Assert.That(boxContainer.IsMeasureValid, Is.False);
        }

        boxContainer.Arrange(UIBox2.FromDimensions(Vector2.Zero, boxContainer.SetSize));
        Assert.That(child.Position, Is.EqualTo(new Vector2(45, 0)));

        boxContainer.Stylesheet = new Stylesheet([]);
        boxContainer.ForceRunStyleUpdate();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(boxContainer.Orientation, Is.EqualTo(LayoutOrientation.Horizontal));
            Assert.That(boxContainer.IsMeasureValid, Is.False);
        }

        boxContainer.Arrange(UIBox2.FromDimensions(Vector2.Zero, boxContainer.SetSize));
        Assert.That(child.Position, Is.EqualTo(new Vector2(0, 45)));
    }

    [Test]
    public void TestSeparationOverride()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var boxContainer = new BoxContainer
        {
            SetSize = new Vector2(100, 10),
        };

        boxContainer.SeparationOverride = 10;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(boxContainer.Separation, Is.EqualTo(10));
            // Evil
            Assert.That(
                typeof(BoxContainer).GetProperty(nameof(boxContainer.SeparationOverride))!.GetValue(boxContainer),
                Is.EqualTo(10));
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
