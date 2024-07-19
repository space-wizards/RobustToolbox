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
        private static (FormattedMessage Message, bool Trimmed)[] _trimMessages = new[]
        {
            (FormattedMessage.FromUnformatted("weh"), false),
            (FormattedMessage.FromUnformatted("weh "), true),
            (FormattedMessage.Empty, false),
            (FormattedMessage.FromUnformatted("weh\n"), true),
        };

        [Test, TestCaseSource(nameof(_trimMessages))]
        public static void TestTrimEnd((FormattedMessage message, bool shouldTrim) test)
        {
            var copy = FormattedMessage.FromUnformatted(test.message.ToString());
            copy.TrimEnd();

            if (test.shouldTrim)
            {
                Assert.That(copy.ToString(), Is.Not.EqualTo(test.message.ToString()));
            }
            else
            {
                Assert.That(copy.ToString(), Is.EqualTo(test.message.ToString()));
            }
        }

        [Test]
        public static void TestParseMarkup()
        {
            var msg = FormattedMessage.FromMarkup("foo[color=#aabbcc]bar[/color]baz");

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
            var msg = FormattedMessage.FromMarkup("foo[color=orange]bar[/color]baz");

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
            return FormattedMessage.RemoveMarkup(test);
        }

        [Test]
        [TestCase("Foo")]
        [TestCase("[color=#FF000000]Foo[/color]")]
        [TestCase("[color=lime]Foo[/color]bar")]
        public static void TestToMarkup(string text)
        {
            var message = FormattedMessage.FromMarkup(text);
            Assert.That(message.ToMarkup(), NUnit.Framework.Is.EqualTo(text));
        }

        [Test]
        [TestCase("Foo")]
        [TestCase("[color=#FF000000]Foo[/color]")]
        [TestCase("[color=#00FF00FF]Foo[/color]bar")]
        [TestCase("honk honk [color=#00FF00FF]Foo[/color]bar")]
        public static void TestEnumerateRunes(string text)
        {
            var message = FormattedMessage.FromMarkup(text);

            Assert.That(
                message.EnumerateRunes(),
                Is.EquivalentTo(message.ToString().EnumerateRunes()));
        }
    }
}
