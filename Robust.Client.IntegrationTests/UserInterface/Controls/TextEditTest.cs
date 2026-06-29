using System.Numerics;
using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Client.UserInterface.Controls;

[TestFixture]
[TestOf(typeof(TextEdit))]
public sealed class TextEditTest : RobustUnitTest
{
    public override UnitTestProject Project => UnitTestProject.Client;

    [OneTimeSetUp]
    public void Setup()
    {
        IoCManager.Resolve<IUserInterfaceManagerInternal>().InitializeTesting();
    }

    // Regression test for https://github.com/space-wizards/RobustToolbox/issues/4953
    // It was possible to move the cursor up/down if there was multi-line placeholder text.
    [Test]
    public void TestInvalidMoveInPlaceholder()
    {
        var textEdit = new TextEdit { Placeholder = new Rope.Leaf("Foo\nBar") };
        textEdit.Arrange(new UIBox2(0, 0, 200, 200));

        var click = new GUIBoundKeyEventArgs(EngineKeyFunctions.TextCursorDown, BoundKeyState.Down, new ScreenCoordinates(), true, Vector2.Zero, Vector2.Zero);
        textEdit.KeyBindDown(click);
        textEdit.KeyBindUp(click);

        Assert.That(textEdit.CursorPosition.Index, Is.Zero);
    }

    // Regression test for https://github.com/space-wizards/RobustToolbox/issues/4957
    // Moving left (with the arrow keys) in an empty TextEdit would cause an exception.
    [Test]
    public void TestEmptyMoveLeft()
    {
        var textEdit = new TextEdit();
        textEdit.Arrange(new UIBox2(0, 0, 200, 200));

        var click = new GUIBoundKeyEventArgs(EngineKeyFunctions.TextCursorLeft, BoundKeyState.Down, new ScreenCoordinates(), true, Vector2.Zero, Vector2.Zero);
        textEdit.KeyBindDown(click);
        textEdit.KeyBindUp(click);
    }
}
