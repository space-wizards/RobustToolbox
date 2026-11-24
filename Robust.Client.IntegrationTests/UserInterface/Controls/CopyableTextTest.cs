using System.Numerics;
using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Client.UserInterface.Controls;

/// <summary>
///     Integration tests for copyable selection in display-only UI controls.
/// </summary>
[TestFixture]
public sealed class CopyableTextTest : RobustUnitTest
{
    public override UnitTestProject Project => UnitTestProject.Client;

    [SetUp]
    public void ClearClipboard()
    {
        // Headless clipboard is in-memory, so ensure tests don't leak state via clipboard contents.
        IoCManager.Resolve<IClipboardManager>().SetText(string.Empty);
    }

    /// <summary>
    ///     Initializes UI testing infrastructure for these tests.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        IoCManager.Resolve<IUserInterfaceManagerInternal>().InitializeTesting();
    }

    /// <summary>
    ///     Copies the full label text when no selection exists.
    /// </summary>
    [Test]
    public async Task TestLabelCopyAll()
    {
        var label = new TestLabel
        {
            Copyable = true,
            Text = "Hello"
        };
        label.Arrange(new UIBox2(0, 0, 200, 50));

        var copy = new GUIBoundKeyEventArgs(
            EngineKeyFunctions.TextCopy,
            BoundKeyState.Down,
            default, true, Vector2.Zero, Vector2.Zero);
        label.KeyBindDown(copy);

        var clipboard = IoCManager.Resolve<IClipboardManager>();
        var text = await clipboard.GetText();
        Assert.That(text, Is.EqualTo("Hello"));
    }

    /// <summary>
    ///     Copies plain text from rich text labels with markup.
    /// </summary>
    [Test]
    public async Task TestRichTextLabelCopyPlainText()
    {
        var label = new TestRichTextLabel
        {
            Copyable = true
        };

        label.SetMessage(FormattedMessage.FromMarkupPermissive("Hello [color=red]World[/color]"));
        label.Arrange(new UIBox2(0, 0, 300, 50));

        var copy = new GUIBoundKeyEventArgs(
            EngineKeyFunctions.TextCopy,
            BoundKeyState.Down,
            default, true, Vector2.Zero, Vector2.Zero);
        label.KeyBindDown(copy);

        var clipboard = IoCManager.Resolve<IClipboardManager>();
        var text = await clipboard.GetText();
        Assert.That(text, Is.EqualTo("Hello World"));
    }

    /// <summary>
    ///     Copies the full output panel entry when no selection exists.
    /// </summary>
    [Test]
    public async Task TestOutputPanelCopyEntry()
    {
        var output = new TestOutputPanel
        {
            Copyable = true
        };

        output.AddMessage(FormattedMessage.FromUnformatted("Test"));
        output.Arrange(new UIBox2(0, 0, 300, 200));

        var click = new GUIBoundKeyEventArgs(
            EngineKeyFunctions.UIClick,
            BoundKeyState.Down,
            default, true, new Vector2(10, 10), new Vector2(10, 10));
        output.KeyBindDown(click);

        var copy = new GUIBoundKeyEventArgs(
            EngineKeyFunctions.TextCopy,
            BoundKeyState.Down,
            default, true, Vector2.Zero, Vector2.Zero);
        output.KeyBindDown(copy);

        var clipboard = IoCManager.Resolve<IClipboardManager>();
        var text = await clipboard.GetText();
        Assert.That(text, Is.EqualTo("Test"));
    }

    private sealed class TestLabel : Label
    {
        public override bool HasKeyboardFocus()
        {
            return true;
        }
    }

    private sealed class TestRichTextLabel : RichTextLabel
    {
        public override bool HasKeyboardFocus()
        {
            return true;
        }
    }

    private sealed class TestOutputPanel : OutputPanel
    {
        public override bool HasKeyboardFocus()
        {
            return true;
        }
    }
}
