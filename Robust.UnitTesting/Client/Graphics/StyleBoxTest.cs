using NUnit.Framework;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Client.Graphics
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    [TestOf(typeof(StyleBox))]
    public sealed class StyleBoxTest
    {
        [Test]
        public void TestGetEnvelopBox()
        {
            var styleBox = new StyleBoxFlat();

            Assert.That(
                styleBox.GetEnvelopBox(Vector2.Zero, new Vector2(50, 50)),
                Is.EqualTo(new UIBox2(0, 0, 50, 50)));

            styleBox.ContentMarginLeftOverride = 3;
            styleBox.ContentMarginTopOverride = 5;
            styleBox.ContentMarginRightOverride = 7;
            styleBox.ContentMarginBottomOverride = 11;

            Assert.That(
                styleBox.GetEnvelopBox(Vector2.Zero, new Vector2(50, 50)),
                Is.EqualTo(new UIBox2(0, 0, 60, 66)));

            Assert.That(
                styleBox.GetEnvelopBox(new Vector2(10, 10), new Vector2(50, 50)),
                Is.EqualTo(new UIBox2(10, 10, 70, 76)));
        }
    }
}
