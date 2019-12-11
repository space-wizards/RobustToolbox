using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Utility;
using Robust.Shared;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SharpFont;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace Robust.Client.Graphics
{
    internal sealed class FontManager : IFontManagerInternal, IPostInjectInit
    {
#pragma warning disable 649
        [Dependency] private readonly IConfigurationManager _configuration;
#pragma warning restore 649

        private uint BaseFontDPI;

        private readonly Library _library;

        private readonly Dictionary<(FontFaceHandle, int fontSize), FontInstanceHandle> _loadedInstances =
            new Dictionary<(FontFaceHandle, int), FontInstanceHandle>();

        public FontManager()
        {
            _library = new Library();
        }

        public IFontFaceHandle Load(Stream stream)
        {
            var face = new Face(_library, stream.CopyToArray(), 0);
            var handle = new FontFaceHandle(face);
            return handle;
        }

        void IFontManagerInternal.Initialize()
        {
            BaseFontDPI = (uint) _configuration.GetCVar<int>("display.fontdpi");
        }

        public IFontInstanceHandle MakeInstance(IFontFaceHandle handle, int size)
        {
            var fontFaceHandle = (FontFaceHandle) handle;
            if (_loadedInstances.TryGetValue((fontFaceHandle, size), out var instance))
            {
                return instance;
            }

            var glyphMap = _generateGlyphMap(fontFaceHandle.Face);
            instance = new FontInstanceHandle(this, size, glyphMap, fontFaceHandle);

            _loadedInstances.Add((fontFaceHandle, size), instance);
            return instance;
        }

        private ScaledFontData _generateScaledDatum(FontInstanceHandle instance, float scale)
        {
            var ftFace = instance.FaceHandle.Face;
            ftFace.SetCharSize(0, instance.Size, 0, (uint) (BaseFontDPI * scale));

            var ascent = ftFace.Size.Metrics.Ascender.ToInt32();
            var descent = -ftFace.Size.Metrics.Descender.ToInt32();
            var lineHeight = ftFace.Size.Metrics.Height.ToInt32();

            var (atlas, metricsMap) = _generateAtlas(instance, scale);

            return new ScaledFontData(metricsMap, ascent, descent, ascent + descent, lineHeight, atlas);
        }

        private (FontTextureAtlas, Dictionary<uint, CharMetrics> metricsMap)
            _generateAtlas(FontInstanceHandle instance, float scale)
        {
            // TODO: This could use a better box packing algorithm.
            // Right now we treat each glyph bitmap as having the max size among all glyphs.
            // So we can divide the atlas into equal-size rectangles.
            // This wastes a lot of space though because there's a lot of tiny glyphs.

            var face = instance.FaceHandle.Face;

            var maxGlyphSize = Vector2i.Zero;
            var count = 0;

            var metricsMap = new Dictionary<uint, CharMetrics>();

            foreach (var glyph in instance.GlyphMap.Values)
            {
                if (metricsMap.ContainsKey(glyph))
                {
                    continue;
                }

                face.LoadGlyph(glyph, LoadFlags.Default, LoadTarget.Normal);
                face.Glyph.RenderGlyph(RenderMode.Normal);

                var glyphMetrics = face.Glyph.Metrics;
                var metrics = new CharMetrics(glyphMetrics.HorizontalBearingX.ToInt32(),
                    glyphMetrics.HorizontalBearingY.ToInt32(),
                    glyphMetrics.HorizontalAdvance.ToInt32(),
                    glyphMetrics.Width.ToInt32(),
                    glyphMetrics.Height.ToInt32());
                metricsMap.Add(glyph, metrics);

                maxGlyphSize = Vector2i.ComponentMax(maxGlyphSize,
                    new Vector2i(face.Glyph.Bitmap.Width, face.Glyph.Bitmap.Rows));

                count += 1;
            }

            // Make atlas.
            // This is the same algorithm used for RSIs. Tries to keep width and height as close as possible,
            //  but preferring to increase width if necessary.
            var atlasEntriesHorizontal = (int) Math.Ceiling(Math.Sqrt(count));
            var atlasEntriesVertical =
                (int) Math.Ceiling(count / (float) atlasEntriesHorizontal);
            var atlasDimX =
                (int) Math.Round(atlasEntriesHorizontal * maxGlyphSize.X / 4f, MidpointRounding.AwayFromZero) * 4;
            var atlasDimY =
                (int) Math.Round(atlasEntriesVertical * maxGlyphSize.Y / 4f, MidpointRounding.AwayFromZero) * 4;

            using (var atlas = new Image<Alpha8>(atlasDimX, atlasDimY))
            {
                var atlasRegions = new Dictionary<uint, UIBox2>();
                count = 0;
                foreach (var glyph in metricsMap.Keys)
                {
                    face.LoadGlyph(glyph, LoadFlags.Default, LoadTarget.Normal);
                    face.Glyph.RenderGlyph(RenderMode.Normal);

                    var bitmap = face.Glyph.Bitmap;
                    if (bitmap.Pitch == 0)
                    {
                        count += 1;
                        continue;
                    }

                    if (bitmap.Pitch < 0)
                    {
                        throw new NotImplementedException();
                    }

                    var column = count % atlasEntriesHorizontal;
                    var row = count / atlasEntriesVertical;
                    var offsetX = column * maxGlyphSize.X;
                    var offsetY = row * maxGlyphSize.Y;
                    count += 1;
                    atlasRegions.Add(glyph, UIBox2i.FromDimensions(offsetX, offsetY, bitmap.Width, bitmap.Rows));

                    switch (bitmap.PixelMode)
                    {
                        case PixelMode.Mono:
                        {
                            using (var bitmapImage = MonoBitMapToImage(bitmap))
                            {
                                bitmapImage.Blit(new UIBox2i(0, 0, bitmapImage.Width, bitmapImage.Height), atlas,
                                    (offsetX, offsetY));
                            }

                            break;
                        }

                        case PixelMode.Gray:
                        {
                            ReadOnlySpan<Alpha8> span;
                            unsafe
                            {
                                span = new ReadOnlySpan<Alpha8>((void*) bitmap.Buffer, bitmap.Pitch * bitmap.Rows);
                            }

                            span.Blit(bitmap.Pitch, UIBox2i.FromDimensions(0, 0, bitmap.Pitch, bitmap.Rows), atlas,
                                (offsetX, offsetY));
                            break;
                        }

                        case PixelMode.Gray2:
                        case PixelMode.Gray4:
                        case PixelMode.Lcd:
                        case PixelMode.VerticalLcd:
                        case PixelMode.Bgra:
                            throw new NotImplementedException();
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                var atlasDictionary = new Dictionary<uint, AtlasTexture>();
                var texture = Texture.LoadFromImage(atlas, $"font-{face.FamilyName}-{instance.Size}-{(uint) (BaseFontDPI * scale)}");

                foreach (var (glyph, region) in atlasRegions)
                {
                    atlasDictionary.Add(glyph, new AtlasTexture(texture, region));
                }

                return (new FontTextureAtlas(texture, atlasDictionary), metricsMap);
            }
        }

        private static Image<Alpha8> MonoBitMapToImage(FTBitmap bitmap)
        {
            DebugTools.Assert(bitmap.PixelMode == PixelMode.Mono);
            DebugTools.Assert(bitmap.Pitch > 0);

            ReadOnlySpan<byte> span;
            unsafe
            {
                span = new ReadOnlySpan<byte>((void*) bitmap.Buffer, bitmap.Rows * bitmap.Pitch);
            }

            var bitmapImage = new Image<Alpha8>(bitmap.Width, bitmap.Rows);
            for (var y = 0; y < bitmap.Rows; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    var byteIndex = y * bitmap.Pitch + (x / 8);
                    var bitIndex = x % 8;

                    var bit = (span[byteIndex] & (1 << (7 - bitIndex))) != 0;
                    bitmapImage[x, y] = new Alpha8(bit ? byte.MaxValue : byte.MinValue);
                }
            }

            return bitmapImage;
        }

        private Dictionary<char, uint> _generateGlyphMap(Face face)
        {
            var map = new Dictionary<char, uint>();

            // TODO: Render more than extended ASCII + Cyrillic, somehow.
            // Does it make sense to just render every glyph in the font?

            // Render all the extended ASCII characters.
            // Yeah I know "extended ASCII" isn't a real thing get off my back.
            for (var i = 32u; i <= 255; i++)
            {
                _addGlyph(i, face, map);
            }

            // Render basic cyrillic.
            for (var i = 0x0410u; i <= 0x044F; i++)
            {
                _addGlyph(i, face, map);
            }

            return map;
        }

        private static void _addGlyph(uint codePoint, Face face, Dictionary<char, uint> map)
        {
            var glyphIndex = face.GetCharIndex(codePoint);
            if (glyphIndex == 0)
            {
                return;
            }

            map.Add((char) codePoint, glyphIndex);
        }

        private class FontFaceHandle : IFontFaceHandle
        {
            public Face Face { get; }

            public FontFaceHandle(Face face)
            {
                Face = face;
            }
        }

        [PublicAPI]
        private class FontInstanceHandle : IFontInstanceHandle
        {
            public FontFaceHandle FaceHandle { get; }
            public int Size { get; }
            private readonly Dictionary<float, ScaledFontData> _scaledData = new Dictionary<float, ScaledFontData>();
            public readonly IReadOnlyDictionary<char, uint> GlyphMap;
            private readonly FontManager _fontManager;

            public FontInstanceHandle(FontManager fontManager, int size, IReadOnlyDictionary<char, uint> glyphMap,
                FontFaceHandle faceHandle)
            {
                _fontManager = fontManager;
                Size = size;
                GlyphMap = glyphMap;
                FaceHandle = faceHandle;
            }

            public Texture GetCharTexture(char chr, float scale)
            {
                var glyph = _getGlyph(chr);
                if (glyph == 0)
                {
                    return null;
                }

                var scaled = _getScaleDatum(scale);
                scaled.Atlas.AtlasData.TryGetValue(glyph, out var texture);
                return texture;
            }

            public CharMetrics? GetCharMetrics(char chr, float scale)
            {
                var glyph = _getGlyph(chr);
                if (glyph == 0)
                {
                    return null;
                }

                var scaled = _getScaleDatum(scale);
                return scaled.MetricsMap[glyph];
            }

            public int GetAscent(float scale)
            {
                var scaled = _getScaleDatum(scale);
                return scaled.Ascent;
            }

            public int GetDescent(float scale)
            {
                var scaled = _getScaleDatum(scale);
                return scaled.Descent;
            }

            public int GetHeight(float scale)
            {
                var scaled = _getScaleDatum(scale);
                return scaled.Height;
            }

            public int GetLineHeight(float scale)
            {
                var scaled = _getScaleDatum(scale);
                return scaled.LineHeight;
            }

            private uint _getGlyph(char chr)
            {
                if (GlyphMap.TryGetValue(chr, out var glyph))
                {
                    return glyph;
                }

                return 0;
            }

            private ScaledFontData _getScaleDatum(float scale)
            {
                if (_scaledData.TryGetValue(scale, out var datum))
                {
                    return datum;
                }

                datum = _fontManager._generateScaledDatum(this, scale);
                _scaledData.Add(scale, datum);
                return datum;
            }
        }

        private class ScaledFontData
        {
            public ScaledFontData(IReadOnlyDictionary<uint, CharMetrics> metricsMap, int ascent, int descent,
                int height, int lineHeight, FontTextureAtlas atlas)
            {
                MetricsMap = metricsMap;
                Ascent = ascent;
                Descent = descent;
                Height = height;
                LineHeight = lineHeight;
                Atlas = atlas;
            }

            public IReadOnlyDictionary<uint, CharMetrics> MetricsMap { get; }
            public int Ascent { get; }
            public int Descent { get; }
            public int Height { get; }
            public int LineHeight { get; }
            public FontTextureAtlas Atlas { get; }
        }

        private class FontTextureAtlas
        {
            public FontTextureAtlas(Texture mainTexture, Dictionary<uint, AtlasTexture> atlasData)
            {
                MainTexture = mainTexture;
                AtlasData = atlasData;
            }

            public Texture MainTexture { get; }

            // Maps glyph index to atlas.
            public Dictionary<uint, AtlasTexture> AtlasData { get; }
        }

        void IPostInjectInit.PostInject()
        {
            _configuration.RegisterCVar("display.fontdpi", 96);
        }
    }
}
