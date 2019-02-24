using NUnit.Framework;
using SS14.Client.Graphics.Drawing;
using SS14.Shared.Maths;

namespace SS14.UnitTesting.Client.Graphics
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    [TestOf(typeof(StyleBox))]
    public class StyleBoxTest
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
                Is.EqualTo(new UIBox2(3, 5, 60, 66)));
        }
    }
}
