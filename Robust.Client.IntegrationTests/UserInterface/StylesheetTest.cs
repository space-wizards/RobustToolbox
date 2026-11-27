using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.UnitTesting;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Robust.Client.IntegrationTests.UserInterface;

[TestFixture]
public sealed class StylesheetTest : RobustUnitTest
{
    public override UnitTestProject Project => UnitTestProject.Client;

    [OneTimeSetUp]
    public void Setup()
    {
        IoCManager.Resolve<IUserInterfaceManagerInternal>().InitializeTesting();
    }

    [Test]
    public void TestSelectors()
    {
        var selectorElementLabel = new SelectorElement(typeof(Label), null, null, null);

        var label = new Label();
        var panel = new PanelContainer { StyleIdentifier = "bar" };
        Assert.That(selectorElementLabel.Matches(label), Is.True);
        Assert.That(selectorElementLabel.Matches(panel), Is.False);

        selectorElementLabel = new SelectorElement(typeof(Label), ["foo"], null, null);
        Assert.That(selectorElementLabel.Matches(label), Is.False);
        Assert.That(selectorElementLabel.Matches(panel), Is.False);

        Assert.That(label.HasStyleClass("foo"), Is.False);
        label.AddStyleClass("foo");
        Assert.That(selectorElementLabel.Matches(label), Is.True);
        Assert.That(label.HasStyleClass("foo"));
        // Make sure it doesn't throw.
        label.AddStyleClass("foo");
        label.RemoveStyleClass("foo");
        Assert.That(selectorElementLabel.Matches(label), Is.False);
        Assert.That(label.HasStyleClass("foo"), Is.False);
        // Make sure it doesn't throw.
        label.RemoveStyleClass("foo");

        selectorElementLabel = new(null, null, "bar", null);
        Assert.That(selectorElementLabel.Matches(label), Is.False);
        Assert.That(selectorElementLabel.Matches(panel), Is.True);
    }

    [Test]
    public void TestStyleProperties()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var sheet = new Stylesheet(new[]
        {
            new StyleRule(new SelectorElement(typeof(Label), null, "baz", null),
                new[]
                {
                    new StyleProperty("foo", "honk"),
                }),
            new StyleRule(new SelectorElement(typeof(Label), null, null, null),
                new[]
                {
                    new StyleProperty("foo", "heh"),
                }),
            new StyleRule(new SelectorElement(typeof(Label), null, null, null),
                new[]
                {
                    new StyleProperty("foo", "bar"),
                }),
        });

        var uiMgr = IoCManager.Resolve<IUserInterfaceManager>();
        uiMgr.Stylesheet = sheet;

        var control = new Label();

        uiMgr.StateRoot.AddChild(control);
        control.ForceRunStyleUpdate();

        control.TryGetStyleProperty("foo", out string? value);
        Assert.That(value, Is.EqualTo("bar"));

        control.StyleIdentifier = "baz";
        control.ForceRunStyleUpdate();

        control.TryGetStyleProperty("foo", out value);
        Assert.That(value, Is.EqualTo("honk"));
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Test]
    public void TestStylesheetOverride()
    {
        StyleProp<string> foo = "foo";

        var sheetA = new Stylesheet([
            Element().Class("A").Prop(foo, "bar"),
        ]);

        var sheetB = new Stylesheet([
            Element().Class("A").Prop(foo, "honk!"),
        ]);

        // Set style sheet to null, property shouldn't exist.

        var uiMgr = IoCManager.Resolve<IUserInterfaceManager>();
        uiMgr.Stylesheet = null;

        var baseControl = new Control();
        baseControl.AddStyleClass("A");
        var childA = new Control();
        childA.AddStyleClass("A");
        var childB = new Control();
        childB.AddStyleClass("A");

        uiMgr.StateRoot.AddChild(baseControl);

        baseControl.AddChild(childA);
        childA.AddChild(childB);

        baseControl.ForceRunStyleUpdate();

        Assert.That(baseControl.TryGetStyleProperty(foo, out _), Is.False);
#pragma warning disable CS0618 // Type or member is obsolete
        Assert.That(baseControl.TryGetStyleProperty(foo.Name, out object? _), Is.False);
#pragma warning restore CS0618 // Type or member is obsolete

        uiMgr.RootControl.Stylesheet = sheetA;
        childA.Stylesheet = sheetB;

        // Assign sheets.
        baseControl.ForceRunStyleUpdate();

        baseControl.TryGetStyleProperty(foo, out var str);
        Assert.That(str, Is.EqualTo("bar"));

        childA.TryGetStyleProperty(foo, out str);
        Assert.That(str, Is.EqualTo("honk!"));

        childB.TryGetStyleProperty(foo, out str);
        Assert.That(str, Is.EqualTo("honk!"));

#pragma warning disable CS0618 // Type or member is obsolete
        baseControl.TryGetStyleProperty(foo.Name, out object? value);
        Assert.That(value, Is.EqualTo("bar"));

        childA.TryGetStyleProperty(foo.Name, out value);
        Assert.That(value, Is.EqualTo("honk!"));

        childB.TryGetStyleProperty(foo.Name, out value);
        Assert.That(value, Is.EqualTo("honk!"));
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Test]
    public void TestStylesheetNullables()
    {
        StyleProp<string?> foobar = "foobar";
        StyleProp<string> foobar2 = "foobar";

        var sheet = new Stylesheet([
            Element().Prop(foobar, null)
        ]);

        var control = new Control
        {
            Stylesheet = sheet
        };
        control.ForceRunStyleUpdate();

        Assert.That(control.TryGetStyleProperty(foobar, out var value), Is.True);
        Assert.That(value, Is.Null);

        Assert.That(control.TryGetStyleProperty(foobar2, out _), Is.False);
    }
}
