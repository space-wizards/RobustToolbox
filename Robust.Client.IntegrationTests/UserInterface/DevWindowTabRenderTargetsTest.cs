#if TOOLS
using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Client.UserInterface;

[TestFixture]
[TestOf(typeof(DevWindowTabRenderTargets))]
public sealed class DevWindowTabRenderTargetsTest
{
    [TestCase(50, 25, 50, 25)]
    [TestCase(200, 100, 100, 50)]
    [TestCase(2048, 2, 100, 1)]
    [TestCase(2, 2048, 1, 50)]
    public void TestThumbnailSize(int width, int height, int expectedWidth, int expectedHeight)
    {
        var size = DevWindowTabRenderTargets.GetThumbnailSize(new Vector2i(width, height));

        Assert.That(size, Is.EqualTo(new Vector2i(expectedWidth, expectedHeight)));
    }
}
#endif
