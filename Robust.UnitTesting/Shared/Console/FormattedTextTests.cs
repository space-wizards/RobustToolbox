using NUnit.Framework;
using Robust.Shared.Console;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Console
{
    [TestFixture, Parallelizable, TestOf(typeof(FormattedText))]
    public class FormattedTextTests
    {
        [Test]
        public void PushPopColor()
        {
            string text = new FormattedText()
                .PushColor(Color.Yellow)
                .AddString("|TEST|")
                .PopColor()
                .GetString();

            const string control = "§cFF0|TEST|¶c";
            Assert.That(text, Is.EqualTo(control));
        }
    }
}
