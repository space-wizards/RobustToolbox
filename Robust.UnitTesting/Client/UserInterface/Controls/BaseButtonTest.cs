using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Client.UserInterface.Controls;

[TestFixture]
public sealed class BaseButtonTest : RobustUnitTest
{
    public override UnitTestProject Project => UnitTestProject.Client;

    private string UIRightClick = "UIRightClick";
    private string OpenContextMenu = "UseSecondary";

    private sealed class TestButton : BaseButton { }

    [OneTimeSetUp]
    public void Setup()
    {
        IoCManager.Resolve<IUserInterfaceManagerInternal>().InitializeTesting();
    }

    [Test]
    public void TestToggleButtonToggle()
    {
        var button = new TestButton
        {
            ToggleMode = true
        };
        var toggled = false;

        button.OnToggled += _ =>
        {
            toggled = true;
        };

        var click = new GUIBoundKeyEventArgs(EngineKeyFunctions.UIClick, BoundKeyState.Down, new ScreenCoordinates(), true, Vector2.Zero, Vector2.Zero);
        button.KeyBindDown(click);
        button.KeyBindUp(click);
        Assert.That(button.Pressed, Is.EqualTo(true));
        Assert.That(toggled, Is.EqualTo(true));

        toggled = false;
        button.KeyBindDown(click);
        button.KeyBindUp(click);
        Assert.That(button.Pressed, Is.EqualTo(false));
        Assert.That(toggled, Is.EqualTo(true));
    }

    [Test]
    public void TestToggleButtonAllKeybinds()
    {
        var button = new TestButton
        {
            EnableAllKeybinds = true,
            ToggleMode = true
        };

        var uiRightClickPressed = false;
        var openContextMenuPressed = false;
        var toggled = false;
        button.OnPressed += args =>
        {
            if (args.Event.Function == UIRightClick)
                uiRightClickPressed = true;
            else if (args.Event.Function == OpenContextMenu)
                openContextMenuPressed = true;
        };
        button.OnToggled += _ =>
        {
            toggled = true;
        };

        var uiRightClick = new GUIBoundKeyEventArgs(UIRightClick, BoundKeyState.Down, new ScreenCoordinates(), true, Vector2.Zero, Vector2.Zero);
        var openContextMenu = new GUIBoundKeyEventArgs(OpenContextMenu, BoundKeyState.Down, new ScreenCoordinates(), true, Vector2.Zero, Vector2.Zero);
        button.KeyBindDown(uiRightClick);
        button.KeyBindDown(openContextMenu);

        Assert.That(button.Pressed, Is.EqualTo(false));
        Assert.That(toggled, Is.EqualTo(false));
        button.KeyBindUp(uiRightClick);
        Assert.That(button.Pressed, Is.EqualTo(false));
        Assert.That(toggled, Is.EqualTo(false));
        button.KeyBindUp(openContextMenu);

        Assert.That(uiRightClickPressed, Is.EqualTo(true));
        Assert.That(openContextMenuPressed, Is.EqualTo(true));
        Assert.That(button.Pressed, Is.EqualTo(false));
        Assert.That(toggled, Is.EqualTo(false));
    }
}
