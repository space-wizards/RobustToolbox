using System;
using BenchmarkDotNet.Attributes;
using Robust.Client.Graphics;
using Robust.Shared.Graphics;
using Robust.Shared.Maths;
using ClydeRenderer = Robust.Client.Graphics.Clyde.Clyde;

namespace Robust.Benchmarks.Graphics;

[MemoryDiagnoser]
public class AtlasTextureUvBenchmark
{
    private AtlasTexture _atlas = default!;
    private Texture _texture = default!;
    private UIBox2? _subRegion;

    [GlobalSetup]
    public void Setup()
    {
        var sourceTexture = new ClydeRenderer.ClydeTexture((ClydeHandle) 42, (256, 256), false, null!);
        GC.SuppressFinalize(sourceTexture);
        _atlas = new AtlasTexture(sourceTexture, UIBox2.FromDimensions(48, 80, 16, 16));
        _texture = _atlas;
        _subRegion = null;
    }

    [Benchmark(Baseline = true)]
    public DrawCall LegacyAtlasPath()
    {
        var sourceTexture = ExtractTexture(_texture, in _subRegion, out var region);
        return new DrawCall((long) sourceTexture.TextureId, CalculateUvs(sourceTexture, region));
    }

    [Benchmark]
    public DrawCall TextureCallerToAtlasOverload()
    {
        return DrawCached(_texture, in _subRegion);
    }

    [Benchmark]
    public DrawCall StaticallyTypedAtlasCaller()
    {
        return DrawCached(_atlas);
    }

    private static DrawCall DrawCached(Texture texture, in UIBox2? subRegion)
    {
        if (subRegion == null && texture is AtlasTexture atlas)
            return DrawCached(atlas);

        var fallbackTexture = ExtractTexture(texture, in subRegion, out var region);
        return new DrawCall((long) fallbackTexture.TextureId, CalculateUvs(fallbackTexture, region));
    }

    private static DrawCall DrawCached(AtlasTexture texture)
    {
        return new DrawCall((long) texture.ClydeTexture!.TextureId, texture.NormalizedSubRegion);
    }

    private static ClydeRenderer.ClydeTexture ExtractTexture(Texture texture, in UIBox2? subRegion, out UIBox2 region)
    {
        if (texture is AtlasTexture atlas)
        {
            texture = atlas.SourceTexture;
            if (subRegion.HasValue)
            {
                var offset = atlas.SubRegion.TopLeft;
                region = new UIBox2(subRegion.Value.TopLeft + offset, subRegion.Value.BottomRight + offset);
            }
            else
            {
                region = atlas.SubRegion;
            }
        }
        else
        {
            region = subRegion ?? new UIBox2(0, 0, texture.Width, texture.Height);
        }

        return (ClydeRenderer.ClydeTexture) texture;
    }

    private static Box2 CalculateUvs(Texture texture, UIBox2 region)
    {
        var (width, height) = texture.Size;
        return new Box2(
            region.Left / width,
            (height - region.Bottom) / height,
            region.Right / width,
            (height - region.Top) / height);
    }

    public readonly record struct DrawCall(long TextureId, Box2 TexCoords);
}
