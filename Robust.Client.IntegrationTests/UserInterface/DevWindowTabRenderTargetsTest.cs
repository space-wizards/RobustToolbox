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
    public void TestThumbnailSize(int width, int height, int expectedWidth, int expectedHeight)
    {
        var result = DevWindowTabRenderTargets.TryGetThumbnailSize(new Vector2i(width, height), out var size);

        Assert.That(result, Is.True);
        Assert.That(size, Is.EqualTo(new Vector2i(expectedWidth, expectedHeight)));
    }

    [TestCase(2048, 2)]
    [TestCase(2, 2048)]
    public void TestEmptyThumbnailSize(int width, int height)
    {
        var result = DevWindowTabRenderTargets.TryGetThumbnailSize(new Vector2i(width, height), out _);

        Assert.That(result, Is.False);
    }
}
#endif
