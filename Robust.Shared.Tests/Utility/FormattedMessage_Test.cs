using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Tests.Utility
{
    [Parallelizable(ParallelScope.All)]
    [TestFixture]
    [TestOf(typeof(FormattedMessage))]
    internal sealed class FormattedMessage_Test
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

        /// <summary>
        /// Test that the given formatted message string provides equal result when output & parsed again.
        /// </summary>
        [Test]
        [TestCase("\\[whaaaaa")]
        public static void TestRoundTrip(string markup)
        {
            var message = FormattedMessage.FromMarkupOrThrow(markup);
            var secondMessage = FormattedMessage.FromMarkupOrThrow(message.ToMarkup());

            Assert.That(secondMessage, NUnit.Framework.Is.EqualTo(message));
        }
    }
}
