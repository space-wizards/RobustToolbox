using System;
using System.Linq;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Utility
{
    [Parallelizable(ParallelScope.All)]
    [TestFixture]
    [TestOf(typeof(FormattedMessage))]
    public sealed class FormattedMessage_Test
    {
        [Test]
        public static void TestParseMarkup()
        {
            var msg = FormattedMessage.FromMarkupOrThrow("foo[color=#aabbcc]bar[/color]baz");

            Assert.That(msg.Nodes, NUnit.Framework.Is.EquivalentTo(new MarkupNode[]
            {
                new("foo"),
                new("color", new MarkupParameter(Color.FromHex("#aabbcc")), null),
                new("bar"),
                new("color", null, null, true),
                new("baz")
            }));
        }

        [Test]
        public static void TestParseMarkupColorName()
        {
            var msg = FormattedMessage.FromMarkupOrThrow("foo[color=orange]bar[/color]baz");

            Assert.That(msg.Nodes, NUnit.Framework.Is.EquivalentTo(new MarkupNode[]
            {
                new("foo"),
                new("color", new MarkupParameter(Color.Orange), null),
                new("bar"),
                new("color", null, null, true),
                new("baz")
            }));
        }

        [Test]
        [TestCase("foo[color=#aabbcc bar")]
        public static void TestParsePermissiveMarkup(string text)
        {
            var msg = FormattedMessage.FromMarkupPermissive(text);

            Assert.That(
                string.Join("", msg.Nodes.Where(p => p.Name == null).Select(p => p.Value.StringValue ?? "")),
                NUnit.Framework.Is.EqualTo(text));
        }

        [Test]
        [TestCase("Foo", ExpectedResult = "Foo")]
        [TestCase("[color=red]Foo[/color]", ExpectedResult = "Foo")]
        [TestCase("[color=red]Foo[/color]bar", ExpectedResult = "Foobar")]
        public string TestRemoveMarkup(string test)
        {
            return FormattedMessage.RemoveMarkupOrThrow(test);
        }

        [Test]
        [TestCase("Foo")]
        [TestCase("[color=#FF000000]Foo[/color]")]
        [TestCase("[color=lime]Foo[/color]bar")]
        public static void TestToMarkup(string text)
        {
            var message = FormattedMessage.FromMarkupOrThrow(text);
            Assert.That(message.ToMarkup(), NUnit.Framework.Is.EqualTo(text));
        }

        [Test]
        [TestCase("Foo")]
        [TestCase("[color=#FF000000]Foo[/color]")]
        [TestCase("[color=#00FF00FF]Foo[/color]bar")]
        [TestCase("honk honk [color=#00FF00FF]Foo[/color]bar")]
        public static void TestEnumerateRunes(string text)
        {
            var message = FormattedMessage.FromMarkupOrThrow(text);

            Assert.That(
                message.EnumerateRunes(),
                Is.EquivalentTo(message.ToString().EnumerateRunes()));
        }

        [Test]
        [TestCase("Foo[color=red] bar [/color] bar baz", "honk", 2, "Foo[color=red]honk bar [/color] bar baz")]
        [TestCase("Foo[color=red] bar [/color] bar baz", "honk", 0, "honkFoo[color=red] bar [/color] bar baz")]
        [TestCase("Foo[color=red] bar [/color] bar baz", "honk", 5, "Foo[color=red] bar [/color] bar bazhonk")]
        [TestCase("[color=red] bar [/color] baz", "honk", 4, "[color=red] bar [/color] bazhonk")]
        [TestCase("[color=red] bar [/color]", "honk", 3, "[color=red] bar [/color]honk")]
        [TestCase("[color=red] bar [/color]", "honk", 2, "[color=red] bar honk[/color]")]
        [TestCase("", "honk", 0, "honk")]
        public static void TestInsertAtIndex_ValidIndex_TextInserted(string original, string insertText, int insertIndex, string expected)
        {
            var message = FormattedMessage.FromMarkupOrThrow(original);

            message.InsertAtIndex(new MarkupNode(insertText), insertIndex);

            Assert.That(message.ToMarkup(), NUnit.Framework.Is.EqualTo(expected));
        }

        [Test]
        [TestCase("Foo[color=red] bar [/color] bar baz", -1)]
        [TestCase("Foo[color=red] bar [/color] bar baz", 6)]
        [TestCase("[color=red] bar [/color]", 4)]
        [TestCase("", 1)]
        public static void TestInsertAtIndex_InvalidIndex_Throws(string original,int insertIndex)
        {
            var message = FormattedMessage.FromMarkupOrThrow(original);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                message.InsertAtIndex(new MarkupNode("some text"), insertIndex)
            );
        }

        [Test]
        [TestCase("Foo[color=red] bar [/color] bar baz", "honk", 2, 3, "Foo[color=red][honk] bar [/honk][/color] bar baz")]
        [TestCase("Foo[color=red] bar [/color] bar baz", "honk", 0, 1, "[honk]Foo[/honk][color=red] bar [/color] bar baz")]
        [TestCase("Foo[color=red] bar [/color] bar baz", "honk", 4, 5, "Foo[color=red] bar [/color][honk] bar baz[/honk]")]
        [TestCase("[color=red] bar [/color] bar", "honk", 3, 4, "[color=red] bar [/color][honk] bar[/honk]")]
        [TestCase("[color=red] bar [/color]", "honk", 2, 3, "[color=red] bar [honk][/color][/honk]")]
        public static void TestInsertAtIndex_WithTagValidIndex_TextInserted(
            string original,
            string insertText,
            int insertStart,
            int insertEnd,
            string expected
        )
        {
            var message = FormattedMessage.FromMarkupOrThrow(original);

            message.InsertAtIndex(new MarkupNode(insertText, null, null), insertStart, insertEnd);

            Assert.That(message.ToMarkup(), NUnit.Framework.Is.EqualTo(expected));
        }

        [Test]
        [TestCase("Foo[color=red] bar [/color] bar baz", -1, 0)]
        [TestCase("Foo[color=red] bar [/color] bar baz", 5, 6)]
        [TestCase("[color=red] bar [/color] bar baz", 5, 6)]
        [TestCase("[color=red] bar [/color]", 4, 7)]
        public static void TestInsertAtIndex_WithTagInvalidIndex_Throws(string original, int insertStart, int insertEnd)
        {
            var message = FormattedMessage.FromMarkupOrThrow(original);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                message.InsertAtIndex(new MarkupNode("honk", null, null), insertStart, insertEnd)
            );
        }

        [Test]
        public static void TestInsertAtIndex_WithTagEndLesserThenStart_Throws()
        {
            var message = FormattedMessage.FromMarkupOrThrow("Foo[color=red] bar [/color] bar baz");
            Assert.Throws<ArgumentException>(() =>
                message.InsertAtIndex(new MarkupNode("honk", null, null), 2, 1)
            );
        }

        [Test]
        public static void TestInsertAroundMessage_InsertPlanText_Throws()
        {
            var message = FormattedMessage.FromMarkupOrThrow("Foo[color=red] bar [/color] bar baz");

            Assert.Throws<ArgumentException>(() =>
                message.InsertAroundMessage(new MarkupNode("some-plain-text"))
            );
        }

        [Test]
        [TestCase("Foo[color=red] bar [/color] bar baz", "[honk]Foo[color=red] bar [/color] bar baz[/honk]")]
        [TestCase("A[color=blue]B[color=red]C[/color]D[/color]E", "[honk]A[color=blue]B[color=red]C[/color]D[/color]E[/honk]")]
        [TestCase("Foo baz", "[honk]Foo baz[/honk]")]
        [TestCase("[color=red][/color]", "[honk][color=red][/color][/honk]")]
        [TestCase("[color=red]text[/color]", "[honk][color=red]text[/color][/honk]")]
        [TestCase("[honk=\"true\"]text[/honk]", "[honk][honk=\"true\"]text[/honk][/honk]")]
        [TestCase("", "[honk][/honk]")]
        public static void TestInsertAroundMessage_Tag_TagInserted(string original, string expected)
        {
            var message = FormattedMessage.FromMarkupOrThrow(original);

            message.InsertAroundMessage(new MarkupNode("honk", null, null));

            Assert.That(message.ToMarkup(), Is.EqualTo(expected));
        }

        [Test]
        [TestCase("Foo[color=red] bar [/color] bar baz", "[honk]Foo[color=red] bar [/color] bar baz[/honk]")]
        [TestCase("A[color=blue]B[color=red]C[/color]D[/color]E", "[honk]A[color=blue]B[color=red]C[/color]D[/color]E[/honk]")]
        [TestCase("Foo baz", "[honk]Foo baz[/honk]")]
        [TestCase("[color=red]text[/color]", "[color=red][honk]text[/honk][/color]")]
        [TestCase("[honk=\"true\"]text[/honk]", "[honk=\"true\"][honk]text[/honk][/honk]")]
        public static void InsertAroundText(string original, string expected)
        {
            var message = FormattedMessage.FromMarkupOrThrow(original);

            message.InsertAroundText(new MarkupNode("honk", null, null));

            Assert.That(message.ToMarkup(), Is.EqualTo(expected));
        }
        
        [Test]
        public static void InsertAroundText_PassPlainTextNode()
        {
            var message = FormattedMessage.FromMarkupOrThrow("Foo[color=red] bar [/color] bar baz");

            Assert.Throws<ArgumentException>(() => message.InsertAroundText(new MarkupNode("honk")));
        }

        [Test]
        [TestCase("[color=red][/color]")]
        [TestCase("")]
        public static void InsertAroundText_NoTextNodeInMessage(string original)
        {
            var message = FormattedMessage.FromMarkupOrThrow(original);

            Assert.Throws<ArgumentOutOfRangeException>(()=>message.InsertAroundText(new MarkupNode("honk", null, null)));
        }

        [Test]
        [TestCase("Foo[color=red] bar [/color] bar baz", "bar", "Foo[color=red] [honk]bar[/honk] [/color] [honk]bar[/honk] baz", false)]
        [TestCase("b[color=red]ar[/color]", "bar", "b[color=red]ar[/color]", false)]
        [TestCase("Foo baz", "baZ", "Foo baz", true)]
        [TestCase("Foo baz", "baZ", "Foo [honk]baz[/honk]", false)]
        [TestCase("[color=red]Text[/color]", "text", "[color=red]Text[/color]", true)]
        [TestCase("[color=red]Text[/color]", "text", "[color=red][honk]Text[/honk][/color]", false)]
        public static void InsertAroundString(string original, string textToWrap, string expected, bool matchCase)
        {
            var message = FormattedMessage.FromMarkupOrThrow(original);

            message.InsertAroundString(new MarkupNode("honk", null, null), textToWrap, matchCase);

            Assert.That(message.ToMarkup(), Is.EqualTo(expected));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public static void InsertAroundString_PassPlainText(bool matchCase)
        {
            var message = FormattedMessage.FromMarkupOrThrow("Foo[color=red] bar [/color] bar baz");

            Assert.Throws<ArgumentException>(() => message.InsertAroundString(new MarkupNode("honk"), "bar", matchCase));
        }

        [Test]
        [TestCase("b[color=red]ar[/color]", "color", "b[honk][/honk][color=red]ar[/color]")]
        [TestCase("Foo baz", "color", "Foo baz")]
        [TestCase("[color=red]Text[/color]", "color", "[honk][/honk][color=red]Text[/color]")]
        [TestCase(
            "[color=red]Text[/color] asd [color=red]Other[/color]",
            "color",
            "[honk][/honk][color=red]Text[/color] asd [honk][/honk][color=red]Other[/color]")]
        [TestCase("[bold][/bold][color=red]Text[/color]", "color", "[bold][/bold][honk][/honk][color=red]Text[/color]")]
        [TestCase(
            "[color=red]T [bold]ex[/bold] t[/color]",
            "color",
            "[honk][/honk][color=red]T [bold]ex[/bold] t[/color]")]
        [TestCase("[color=red][/color]", "color", "[honk][/honk][color=red][/color]")]
        [TestCase("[bold] color [/bold]", "color", "[bold] color [/bold]")]
        [TestCase("[honk=\"true\"] color [/honk]", "honk", "[honk][/honk][honk=\"true\"] color [/honk]")]
        [TestCase(
            "[honk=\"true\"] color [/honk][honk=\"true\"] color [/honk]",
            "honk",
            "[honk][/honk][honk=\"true\"] color [/honk][honk][/honk][honk=\"true\"] color [/honk]")]
        [TestCase("", "color", "")]
        public static void InsertBeforeTag(string original, string tagToInsertInto, string expected)
        {   
            var message = FormattedMessage.FromMarkupOrThrow(original);

            message.InsertBeforeTag(new MarkupNode("honk", null, null), tagToInsertInto);

            Assert.That(message.ToMarkup(), Is.EqualTo(expected));
        }


        [Test]
        [TestCase("b[color=red]ar[/color]", "color", "b[color=red]ar[/color][honk][/honk]")]
        [TestCase("Foo baz", "color", "Foo baz")]
        [TestCase("[color=red]Text[/color]", "color", "[color=red]Text[/color][honk][/honk]")]
        [TestCase(
            "[color=red]Text[/color] asd [color=red]Other[/color]",
            "color",
            "[color=red]Text[/color][honk][/honk] asd [color=red]Other[/color][honk][/honk]")]
        [TestCase("[color=red]Text[/color][bold][/bold]", "color", "[color=red]Text[/color][honk][/honk][bold][/bold]")]
        [TestCase(
            "[color=red]T [bold]ex[/bold] t[/color]",
            "color",
            "[color=red]T [bold]ex[/bold] t[/color][honk][/honk]")]
        [TestCase("[color=red][/color]", "color", "[color=red][/color][honk][/honk]")]
        [TestCase("[bold] color [/bold]", "color", "[bold] color [/bold]")]
        [TestCase("[honk=\"true\"] color [/honk]", "honk", "[honk=\"true\"] color [/honk][honk][/honk]")]
        [TestCase(
            "[honk=\"true\"] color [/honk][honk=\"true\"] color [/honk]",
            "honk",
            "[honk=\"true\"] color [/honk][honk][/honk][honk=\"true\"] color [/honk][honk][/honk]")]
        [TestCase("", "color", "")]
        public static void InsertAfterTag(string original, string tagToInsertInto, string expected)
        {
            var message = FormattedMessage.FromMarkupOrThrow(original);

            message.InsertAfterTag(new MarkupNode("honk", null, null), tagToInsertInto);

            Assert.That(message.ToMarkup(), Is.EqualTo(expected));
        }

        [Test]
        [TestCase("b[color=red]ar[/color]", "color", "b[honk][color=red]ar[/color][/honk]")]
        [TestCase("Foo baz", "color", "Foo baz")]
        [TestCase("[color=red]Text[/color]", "color", "[honk][color=red]Text[/color][/honk]")]
        [TestCase(
            "[color=red]Text[/color] asd [color=red]Other[/color]",
            "color",
            "[honk][color=red]Text[/color][/honk] asd [honk][color=red]Other[/color][/honk]")]
        [TestCase("[color=red]Text[/color][bold][/bold]", "color", "[honk][color=red]Text[/color][/honk][bold][/bold]")]
        [TestCase("[color=red]T [bold]ex[/bold] t[/color]", "color", "[honk][color=red]T [bold]ex[/bold] t[/color][/honk]")]
        [TestCase("[color=red][/color]", "color", "[honk][color=red][/color][/honk]")]
        [TestCase("[bold] color [/bold]", "color", "[bold] color [/bold]")]
        [TestCase("", "color", "")]
        [TestCase(
            "[honk=\"true\"] color [/honk][honk=\"true\"] color [/honk]",
            "honk",
            "[honk][honk=\"true\"] color [/honk][/honk][honk][honk=\"true\"] color [/honk][/honk]")]
        [TestCase("", "color", "")]
        public static void InsertOutsideTag(string original, string tagToInsertInto, string expected)
        {
            var message = FormattedMessage.FromMarkupOrThrow(original);

            message.InsertOutsideTag(new MarkupNode("honk", null, null), tagToInsertInto);

            Assert.That(message.ToMarkup(), Is.EqualTo(expected));
        }

        [Test]
        public static void InsertOutsideTag_PlainTextNode_Throws()
        {
            var message = FormattedMessage.FromMarkupOrThrow("b[color=red]ar[/color]");

            Assert.Throws<ArgumentException>(() =>
                message.InsertOutsideTag(new MarkupNode("honk"), "color")
            );
        }

        [Test]
        public static void InsertOutsideTag_InvalidMessageMarkup()
        {
            var message = FormattedMessage.FromMarkupOrThrow("[bold]b[color=red]ar[/color]");

            Assert.Throws<InvalidOperationException>(() =>
                message.InsertOutsideTag(new MarkupNode("honk", null, null), "bold")
            );
        }

        [Test]
        [TestCase("b[color=red]ar[/color]", "color", "b[color=red][honk]ar[/honk][/color]")]
        [TestCase("Foo baz", "color", "Foo baz")]
        [TestCase("[color=red]Text[/color]", "color", "[color=red][honk]Text[/honk][/color]")]
        [TestCase(
            "[color=red]Text[/color] asd [color=red]Other[/color]",
            "color",
            "[color=red][honk]Text[/honk][/color] asd [color=red][honk]Other[/honk][/color]")]
        [TestCase("[color=red]Text[/color][bold][/bold]", "color", "[color=red][honk]Text[/honk][/color][bold][/bold]")]
        [TestCase("[color=red]T [bold]ex[/bold] t[/color]", "color", "[color=red][honk]T [bold]ex[/bold] t[/honk][/color]")]
        [TestCase("[color=red][/color]", "color", "[color=red][honk][/honk][/color]")]
        [TestCase("[bold] color [/bold]", "color", "[bold] color [/bold]")]
        [TestCase(
            "[honk=\"true\"] color [/honk][honk=\"true\"] color [/honk]",
            "honk",
            "[honk=\"true\"][honk] color [/honk][/honk][honk=\"true\"][honk] color [/honk][/honk]")]
        [TestCase("", "color", "")]
        public static void InsertInsideTag(string original, string tagToInsertInto, string expected)
        {
            var message = FormattedMessage.FromMarkupOrThrow(original);

            message.InsertInsideTag(new MarkupNode("honk", null, null), tagToInsertInto);

            Assert.That(message.ToMarkup(), Is.EqualTo(expected));
        }

        [Test]
        public static void InsertInsideTag_PlainTextNode_Throws()
        {
            var message = FormattedMessage.FromMarkupOrThrow("b[color=red]ar[/color]");

            Assert.Throws<ArgumentException>(() =>
                message.InsertInsideTag(new MarkupNode("honk"), "color")
            );
        }

        [Test]
        public static void InsertInsideTag_InvalidMessageMarkup()
        {
            var message = FormattedMessage.FromMarkupOrThrow("[bold]b[color=red]ar[/color]");

            Assert.Throws<InvalidOperationException>(() =>
                message.InsertInsideTag(new MarkupNode("honk", null, null), "bold")
            );
        }

        [Test]
        [TestCase("Foo[color=red] bar [/color] bar baz", "[honk][/honk]Foo[color=red] bar [/color] bar baz")]
        [TestCase("b[color=red]ar[/color]", "[honk][/honk]b[color=red]ar[/color]")]
        [TestCase("Foo baz", "[honk][/honk]Foo baz")]
        [TestCase("[color=red]Text[/color]", "[honk][/honk][color=red]Text[/color]")]
        [TestCase("[color=red][/color]", "[honk][/honk][color=red][/color]")]
        [TestCase("", "[honk][/honk]")]
        public static void InsertBeforeMessage(string original, string expected)
        {
            var message = FormattedMessage.FromMarkupOrThrow(original);

            message.InsertBeforeMessage(new MarkupNode("honk", null, null));

            Assert.That(message.ToMarkup(), Is.EqualTo(expected));
        }

        [Test]
        [TestCase("Foo[color=red] bar [/color] bar baz", "some textFoo[color=red] bar [/color] bar baz")]
        [TestCase("b[color=red]ar[/color]", "some textb[color=red]ar[/color]")]
        [TestCase("Foo baz", "some textFoo baz")]
        [TestCase("[color=red]Text[/color]", "some text[color=red]Text[/color]")]
        [TestCase("[color=red][/color]", "some text[color=red][/color]")]
        [TestCase("", "some text")]
        public static void InsertBeforeMessage_PlainTextNode(string original, string expected)
        {
            var message = FormattedMessage.FromMarkupOrThrow(original);

            message.InsertBeforeMessage(new MarkupNode("some text"));

            Assert.That(message.ToMarkup(), Is.EqualTo(expected));
        }

        [Test]
        [TestCase("Foo[color=red] bar [/color] bar baz", "Foo[color=red] bar [/color] bar baz[honk][/honk]")]
        [TestCase("b[color=red]ar[/color]", "b[color=red]ar[/color][honk][/honk]")]
        [TestCase("Foo baz", "Foo baz[honk][/honk]")]
        [TestCase("[color=red]Text[/color]", "[color=red]Text[/color][honk][/honk]")]
        [TestCase("[color=red][/color]", "[color=red][/color][honk][/honk]")]
        [TestCase("", "[honk][/honk]")]
        public static void InsertAfterMessage(string original, string expected)
        {
            var message = FormattedMessage.FromMarkupOrThrow(original);

            message.InsertAfterMessage(new MarkupNode("honk", null, null));

            Assert.That(message.ToMarkup(), Is.EqualTo(expected));
        }
        [Test]
        [TestCase("Foo[color=red] bar [/color] bar baz", "Foo[color=red] bar [/color] bar bazsome text")]
        [TestCase("b[color=red]ar[/color]", "b[color=red]ar[/color]some text")]
        [TestCase("Foo baz", "Foo bazsome text")]
        [TestCase("[color=red]Text[/color]", "[color=red]Text[/color]some text")]
        [TestCase("[color=red][/color]", "[color=red][/color]some text")]
        [TestCase("", "some text")]
        public static void InsertAfterMessage_PlainTextNode(string original, string expected)
        {
            var message = FormattedMessage.FromMarkupOrThrow(original);

            message.InsertAfterMessage(new MarkupNode("some text"));

            Assert.That(message.ToMarkup(), Is.EqualTo(expected));
        }

        [Test]
        [TestCase("[honk]Foo[color=red] bar[/color][/honk] bar baz", "honk", "[honk]Foo[color=red] bar[/color][/honk]")]
        [TestCase("[honk]Foo[/honk] bar baz", "honk", "[honk]Foo[/honk]")]
        [TestCase("[honk][color=red][/color][/honk] bar baz", "honk", "[honk][color=red][/color][/honk]")]
        [TestCase(
            "[honk][color=red] [honk]text[/honk] [/color][/honk] bar baz",
            "honk",
            "[honk][color=red] [honk]text[/honk] [/color][/honk]")]
        public static void TryGetMessageInsideTag(string original, string extractTag, string expected)
        {
            var message = FormattedMessage.FromMarkupOrThrow(original);

            var result = message.TryGetMessageInsideTag(out var messageInside, extractTag);
            Assert.That(result, NUnit.Framework.Is.True);
            Assert.That(messageInside!.ToMarkup(), NUnit.Framework.Is.EqualTo(expected));
        }

        [Test]
        [TestCase("[color=red][/color] bar baz", "honk")]
        [TestCase("", "honk")]
        public static void TryGetMessageInsideTag_NoClosingTag_Throws(string original, string extractTag)
        {
            var message = FormattedMessage.FromMarkupOrThrow(original);

            var result = message.TryGetMessageInsideTag(out var messageInside, extractTag);
            Assert.That(result, NUnit.Framework.Is.False);
            Assert.That(messageInside, NUnit.Framework.Is.Null);
        }
    }
}
