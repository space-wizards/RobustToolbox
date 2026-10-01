using NUnit.Framework;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.Tests.Graphics;

[TestFixture]
public sealed class AtlasTextureTest
{
    [Test]
    public void AllowsNonClydeSourceTextures()
    {
        var source = new TestTexture((64, 64));
        var atlas = new AtlasTexture(source, UIBox2.FromDimensions(8, 16, 32, 32));

        Assert.That(atlas.SourceTexture, Is.SameAs(source));
        Assert.That(atlas.ClydeTexture, Is.Null);
    }

    private sealed class TestTexture(Vector2i size) : Texture(size)
    {
        public override Color GetPixel(int x, int y)
        {
            return Color.Black;
        }
    }
}
