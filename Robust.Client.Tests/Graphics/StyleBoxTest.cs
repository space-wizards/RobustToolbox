using System.Numerics;
using NUnit.Framework;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.Tests.Graphics
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    [TestOf(typeof(StyleBox))]
    internal sealed class StyleBoxTest
    {
        [Test]
        public void TestGetEnvelopBox()
        {
            var styleBox = new StyleBoxFlat();

            Assert.That(
                styleBox.GetEnvelopBox(Vector2.Zero, new Vector2(50, 50), 1),
                Is.EqualTo(new UIBox2(0, 0, 50, 50)));

            styleBox.ContentMarginLeftOverride = 3;
            styleBox.ContentMarginTopOverride = 5;
            styleBox.ContentMarginRightOverride = 7;
            styleBox.ContentMarginBottomOverride = 11;

            Assert.That(
                styleBox.GetEnvelopBox(Vector2.Zero, new Vector2(50, 50), 1),
                Is.EqualTo(new UIBox2(0, 0, 60, 66)));

            Assert.That(
                styleBox.GetEnvelopBox(new Vector2(10, 10), new Vector2(50, 50), 1),
                Is.EqualTo(new UIBox2(10, 10, 70, 76)));

            Assert.That(
                styleBox.GetEnvelopBox(new Vector2(10, 10), new Vector2(50, 50), 2.0f),
                Is.EqualTo(new UIBox2(10, 10, 80, 92)));
        }

        [Test]
        public void TestGetContentBoxClampsWhenMarginsExceedBaseBox()
        {
            var styleBox = new StyleBoxFlat
            {
                ContentMarginLeftOverride = 10,
                ContentMarginTopOverride = 20,
                ContentMarginRightOverride = 30,
                ContentMarginBottomOverride = 40,
            };

            var contentBox = styleBox.GetContentBox(new UIBox2(0, 0, 5, 5), 1);

            Assert.That(contentBox, Is.EqualTo(new UIBox2(10, 20, 10, 20)));
        }
    }
}
