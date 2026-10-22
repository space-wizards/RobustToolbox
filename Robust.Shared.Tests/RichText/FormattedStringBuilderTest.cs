using System.Text;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.RichText;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.RichText;

[Parallelizable(ParallelScope.All)]
[TestFixture, TestOf(typeof(FormattedStringBuilder))]
public static class FormattedStringBuilderTest
{
    [Test]
    public static void TestPlainText()
    {
        var fsb = new FormattedStringBuilder();
        fsb.AppendText("Foobar");
        fsb.AppendLine();
        fsb.AppendMarkup("Wawa");

        AssertMarkup(fsb, "Foobar\nWawa");
    }

    [Test]
    public static void TestPlainTextExistingBuilder()
    {
        var sb = new StringBuilder();
        sb.Append("Guh");
        var fsb = new FormattedStringBuilder(sb);
        fsb.AppendText("Foobar");
        fsb.AppendLine();
        fsb.AppendMarkup("Wawa");

        AssertMarkup(fsb, "GuhFoobar\nWawa");
    }

    [Test]
    public static void TestBasicTag()
    {
        var fsb = new FormattedStringBuilder();
        fsb.BeginTag("bold");
        fsb.FinishTagOpen();

        fsb.AppendText("bar");
        fsb.PopTag();

        AssertMarkup(fsb, "[bold]bar[/bold]");
    }

    [Test]
    public static void TestBasicTagFormattedString()
    {
        var fsb = new FormattedStringBuilder();
        fsb.BeginTag("bold");
        fsb.FinishTagOpen();

        fsb.AppendText("bar");
        fsb.PopTag();

        Assert.That((FormattedMessage)fsb.ToFormattedString(), Is.EqualTo(FormattedMessage.FromMarkupOrThrow("[bold]bar[/bold]")));
    }

    [Test]
    public static void TestSelfClosingTag()
    {
        var fsb = new FormattedStringBuilder();
        fsb.BeginTag("bold");
        fsb.FinishTagSelfClosed();

        AssertMarkup(fsb, "[bold /]");
    }

    [Test]
    public static void TestTagValueLong()
    {
        var fsb = new FormattedStringBuilder();
        fsb.BeginTag("bold", 10);
        fsb.FinishTagSelfClosed();

        AssertMarkup(fsb, "[bold=10 /]");
    }

    [Test]
    public static void TestTagValueString()
    {
        var fsb = new FormattedStringBuilder();
        fsb.BeginTag("bold", "wawa");
        fsb.FinishTagSelfClosed();

        AssertMarkup(fsb, "[bold=\"wawa\" /]");
    }

    [Test]
    public static void TestTagValueColor()
    {
        var fsb = new FormattedStringBuilder();
        fsb.BeginTag("bold", Color.FromHex("#AAA"));
        fsb.FinishTagSelfClosed();

        AssertMarkup(fsb, "[bold=#AAA /]");
    }

    [Test]
    public static void TestTagAttributeString()
    {
        AssertMarkup(
            Fsb().BeginTag("bold").TagAttribute("a", "b").FinishTagSelfClosed(),
            "[bold a=\"b\" /]");
    }

    [Test]
    public static void TestTagAttributeLong()
    {
        AssertMarkup(
            Fsb().BeginTag("bold").TagAttribute("a", 10).FinishTagSelfClosed(),
            "[bold a=10 /]");
    }

    [Test]
    public static void TestTagAttributeColor()
    {
        AssertMarkup(
            Fsb().BeginTag("bold").TagAttribute("a", Color.FromHex("#AAA")).FinishTagSelfClosed(),
            "[bold a=#AAA /]");
    }

    [Test]
    public static void TestAppendMarkup()
    {
        AssertMarkup(
            Fsb().AppendMarkup("[bold /]"),
            "[bold /]");
    }

    [Test]
    public static void TestAppendMarkupLine()
    {
        AssertMarkup(
            Fsb().AppendMarkupLine("[bold /]"),
            "[bold /]\n");
    }

    [Test]
    public static void TestBeginInvalid()
    {
        var fsb = Fsb().BeginTag("a");

        Assert.That(() => fsb.BeginTag("a"), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public static void TestBeginValueInvalid()
    {
        var fsb = Fsb().BeginTag("a");

        Assert.That(() => fsb.BeginTag("a", "b"), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public static void TestTagAttributeInvalid()
    {
        var fsb = Fsb();

        Assert.That(() => fsb.TagAttribute("a", "b"), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public static void TestFinishTagSelfClosedInvalid()
    {
        var fsb = Fsb();

        Assert.That(() => fsb.FinishTagSelfClosed(), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public static void TestFinishTagOpenInvalid()
    {
        var fsb = Fsb();

        Assert.That(() => fsb.FinishTagOpen(), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public static void TestPopTagEmpty()
    {
        var fsb = Fsb();

        Assert.That(() => fsb.PopTag(), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public static void TestPopTagInvalid()
    {
        var fsb = Fsb().BeginTag("a");

        Assert.That(() => fsb.PopTag(), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public static void TestAppendTextInvalid()
    {
        var fsb = Fsb().BeginTag("a");

        Assert.That(() => fsb.AppendText("A"), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public static void TestAppendMarkupInvalid()
    {
        var fsb = Fsb().BeginTag("a");

        Assert.That(() => fsb.AppendMarkup("A"), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public static void TestAppendMarkupInvalidMarkup()
    {
        var fsb = Fsb();

        Assert.That(() => fsb.AppendMarkup("[wawa"), Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public static void TestAppendLineInvalid()
    {
        var fsb = Fsb().BeginTag("a");

        Assert.That(() => fsb.AppendLine(), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public static void TestAppendMarkupLineInvalid()
    {
        var fsb = Fsb().BeginTag("a");

        Assert.That(() => fsb.AppendMarkupLine("guh"), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public static void TestAppendMarkupLineInvalidMarkup()
    {
        var fsb = Fsb();

        Assert.That(() => fsb.AppendMarkupLine("[guh"), Throws.TypeOf<ArgumentException>());
    }

    private static void AssertMarkup(FormattedStringBuilder fsb, string expected)
    {
        Assert.That(
            FormattedMessage.FromMarkupOrThrow(fsb.ToString()),
            Is.EqualTo(FormattedMessage.FromMarkupOrThrow(expected)));
    }

    private static FormattedStringBuilder Fsb() => new FormattedStringBuilder();
}
