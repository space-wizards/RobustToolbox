using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Utility
{
    [Parallelizable(ParallelScope.All)]
    [TestFixture]
    [TestOf(typeof(FormattedMessage))]
    public class FormattedMessage_Test
    {
        [Test]
        public static void TestParseMarkup()
        {
            var msg = FormattedMessage.FromMarkup("foo[color=#aabbcc]bar[/color]baz");

            Assert.That(msg.Tags.Count, Is.EqualTo(5));
            Assert.That(((FormattedMessage.TagText) msg.Tags[0]).Text, Is.EqualTo("foo"));
            Assert.That(((FormattedMessage.TagColor) msg.Tags[1]).Color, Is.EqualTo(Color.FromHex("#aabbcc")));
            Assert.That(((FormattedMessage.TagText) msg.Tags[2]).Text, Is.EqualTo("bar"));
            Assert.That(msg.Tags[3], Is.InstanceOf<FormattedMessage.TagPop>());
            Assert.That(((FormattedMessage.TagText) msg.Tags[4]).Text, Is.EqualTo("baz"));
        }

        [Test]
        public static void TestParseMarkupColorName()
        {
            var msg = FormattedMessage.FromMarkup("foo[color=orange]bar[/color]baz");

            Assert.That(msg.Tags.Count, Is.EqualTo(5));
            Assert.That(((FormattedMessage.TagText) msg.Tags[0]).Text, Is.EqualTo("foo"));
            Assert.That(((FormattedMessage.TagColor) msg.Tags[1]).Color, Is.EqualTo(Color.Orange));
            Assert.That(((FormattedMessage.TagText) msg.Tags[2]).Text, Is.EqualTo("bar"));
            Assert.That(msg.Tags[3], Is.InstanceOf<FormattedMessage.TagPop>());
            Assert.That(((FormattedMessage.TagText) msg.Tags[4]).Text, Is.EqualTo("baz"));
        }
    }
}
