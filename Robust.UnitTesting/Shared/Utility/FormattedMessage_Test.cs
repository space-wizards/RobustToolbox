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

            Assert.That(msg.Tags, NUnit.Framework.Is.EquivalentTo(new FormattedMessage.Tag[]
            {
                new FormattedMessage.TagText("foo"),
                new FormattedMessage.TagColor(Color.FromHex("#aabbcc")),
                new FormattedMessage.TagText("bar"),
                FormattedMessage.TagPop.Instance,
                new FormattedMessage.TagText("baz")
            }));
        }

        [Test]
        public static void TestParseMarkupColorName()
        {
            var msg = FormattedMessage.FromMarkup("foo[color=orange]bar[/color]baz");

            Assert.That(msg.Tags, NUnit.Framework.Is.EquivalentTo(new FormattedMessage.Tag[]
            {
                new FormattedMessage.TagText("foo"),
                new FormattedMessage.TagColor(Color.Orange),
                new FormattedMessage.TagText("bar"),
                FormattedMessage.TagPop.Instance,
                new FormattedMessage.TagText("baz")
            }));
        }
    }
}
