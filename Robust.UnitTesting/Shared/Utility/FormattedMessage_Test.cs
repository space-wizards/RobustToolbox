using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.Utility.Markup;

namespace Robust.UnitTesting.Shared.Utility
{
    [Parallelizable(ParallelScope.All)]
    [TestFixture]
    [TestOf(typeof(Basic))]
    public class MarkupBasic_Test
    {
        [Test]
        public static void TestParseMarkup()
        {
            var msg = new Basic();
            msg.AddMarkup("foo[color=#aabbcc]bar[/color]baz");

            Assert.That(msg.Render().Sections, NUnit.Framework.Is.EquivalentTo(new[]
            {
                new Section { Content="foo" },
                new Section { Content="bar", Color=unchecked ((int) 0xFFAABBCC) },
                new Section { Content="baz" }
            }));
        }

        [Test]
        public static void TestParseMarkupColorName()
        {
            var msg = new Basic();
            msg.AddMarkup("foo[color=orange]bar[/color]baz");

            Assert.That(msg.Render().Sections, NUnit.Framework.Is.EquivalentTo(new[]
            {
                new Section { Content="foo" },
                new Section { Content="bar", Color=Color.Orange.ToArgb() },
                new Section { Content="baz" }
            }));
        }

        [Test]
        [TestCase("foo[color=#aabbcc bar")]
        [TestCase("foo[color #aabbcc] bar")]
        [TestCase("foo[stinky] bar")]
        public static void TestParsePermissiveMarkup(string text)
        {
            var msg = new Basic();
            msg.AddMarkupPermissive(text);

            Assert.That(
                msg.Render().ToString(),
                NUnit.Framework.Is.EqualTo(text));
        }

        [Test]
        [TestCase("Foo")]
        [TestCase("[color=#FF000000]Foo[/color]")]
        [TestCase("[color=#00FF00FF]Foo[/color]bar")]
        public static void TestToMarkup(string text)
        {
            var message = new Basic();
            message.AddMarkup(text);
            Assert.That(message.Render().ToMarkup(), NUnit.Framework.Is.EqualTo(text));
        }
    }
}
