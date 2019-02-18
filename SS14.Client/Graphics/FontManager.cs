using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SharpFont;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using SS14.Client.Interfaces.Graphics;
using SS14.Shared.Configuration;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Utility;

namespace SS14.Client.Graphics
{
    internal sealed class FontManager : IFontManagerInternal, IPostInjectInit
    {
        [Dependency] private readonly IConfigurationManager _configuration;

        private uint FontDPI;

        private readonly Library _library;

        private readonly Dictionary<(FontFaceHandle, int fontSize), FontInstanceHandle> _loadedInstances =
            new Dictionary<(FontFaceHandle, int), FontInstanceHandle>();

        public FontManager()
        {
            _library = new Library();
        }

        public IFontFaceHandle Load(ReadOnlySpan<byte> data)
        {
            Face face;
            unsafe
            {
                fixed (byte* ptr = data)
                {
                    face = new Face(_library, (IntPtr) ptr, data.Length, 0);
                }
            }

            var handle = new FontFaceHandle(face);
            return handle;
        }

        public IFontInstanceHandle MakeInstance(IFontFaceHandle handle, int size)
        {
            var fontFaceHandle = (FontFaceHandle) handle;
            if (_loadedInstances.TryGetValue((fontFaceHandle, size), out var instance))
            {
                return instance;
            }

            var face = fontFaceHandle.Face;
            var (atlasData, glyphMap, metricsMap) = _generateAtlas(face, size);
            var ascent = face.Size.Metrics.Ascender.ToInt32();
            var descent = -face.Size.Metrics.Descender.ToInt32();
            var height = face.Size.Metrics.Height.ToInt32();
            var instanceHandle = new FontInstanceHandle(this, atlasData, size, fontFaceHandle.Face, glyphMap, ascent,
                descent, height, metricsMap);
            _loadedInstances.Add((fontFaceHandle, size), instanceHandle);
            return instanceHandle;
        }

        void IFontManagerInternal.Initialize()
        {
            FontDPI = (uint) _configuration.GetCVar<int>("display.fontdpi");
        }

        private (FontTextureAtlas, Dictionary<char, uint> glyphMap, Dictionary<uint, CharMetrics> metricsMap)
            _generateAtlas(Face face, int size)
        {
            // TODO: This could use a better box packing algorithm.
            // Right now we treat each glyph bitmap as having the max size among all glyphs.
            // So we can divide the atlas into equal-size rectangles.
            // This wastes a lot of space though because there's a lot of tiny glyphs.
            face.SetCharSize(0, size, 0, FontDPI);
            var maxGlyphSize = Vector2i.Zero;
            var count = 0;

            // TODO: Render more than ASCII, somehow. Does it make sense to just render every glyph in the font?
            // Render all the normal ASCII characters.
            const uint startIndex = 32;
            const uint endIndex = 127;
            for (var i = startIndex; i <= endIndex; i++)
            {
                face.LoadChar(i, LoadFlags.Default, LoadTarget.Normal);

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
            var atlas = new Image<Rgba32>(atlasEntriesHorizontal * maxGlyphSize.X,
                atlasEntriesVertical * maxGlyphSize.Y);

            var glyphMap = new Dictionary<char, uint>();
            var metricsMap = new Dictionary<uint, CharMetrics>();
            var atlasRegions = new Dictionary<uint, UIBox2>();
            count = 0;
            for (var i = startIndex; i <= endIndex; i++)
            {
                var glyphIndex = face.GetCharIndex(i);
                if (glyphIndex == 0)
                {
                    count += 1;
                    continue;
                }
                glyphMap.Add((char) i, glyphIndex);
                face.LoadGlyph(glyphIndex, LoadFlags.Default, LoadTarget.Normal);
                face.Glyph.RenderGlyph(RenderMode.Normal);
                var glyphMetrics = face.Glyph.Metrics;
                var metrics = new CharMetrics(glyphMetrics.HorizontalBearingX.ToInt32(),
                    glyphMetrics.HorizontalBearingY.ToInt32(),
                    glyphMetrics.HorizontalAdvance.ToInt32());
                metricsMap.Add(glyphIndex, metrics);

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

                Image<Alpha8> bitmapImage;
                switch (bitmap.PixelMode)
                {
                    case PixelMode.Mono:
                    {
                        bitmapImage = MonoBitMapToImage(bitmap);
                        break;
                    }
                    case PixelMode.Gray:
                    {
                        ReadOnlySpan<Alpha8> span;
                        unsafe
                        {
                            span = new ReadOnlySpan<Alpha8>((void*) bitmap.Buffer, bitmap.Pitch * bitmap.Rows);
                        }

                        bitmapImage = Image.LoadPixelData(span, bitmap.Width, bitmap.Rows);
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

                var column = count % atlasEntriesHorizontal;
                var row = count / atlasEntriesVertical;
                var offsetX = column * maxGlyphSize.X;
                var offsetY = row * maxGlyphSize.Y;
                atlas.Mutate(x => x.DrawImage(bitmapImage, new Point(column * maxGlyphSize.X, row * maxGlyphSize.Y),
                    PixelColorBlendingMode.Overlay, 1));
                count += 1;
                atlasRegions[glyphIndex] = UIBox2i.FromDimensions(offsetX, offsetY, bitmap.Width, bitmap.Rows);
            }

            for (var x = 0; x < atlas.Width; x++)
            {
                for (var y = 0; y < atlas.Height; y++)
                {
                    var a = atlas[x, y].A;
                    if (a != 0)
                    {
                        atlas[x, y] = new Rgba32(255, 255, 255, a);
                    }
                }
            }

            var atlasDictionary = new Dictionary<uint, AtlasTexture>();
            var texture = Texture.LoadFromImage(atlas, $"font-{face.FamilyName}-{size}");

            foreach (var (glyph, region) in atlasRegions)
            {
                atlasDictionary.Add(glyph, new AtlasTexture(texture, region));
            }

            return (new FontTextureAtlas(texture, atlasDictionary), glyphMap, metricsMap);
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
            public Face Face { get; }
            public int Size { get; }
            private readonly Dictionary<char, uint> _glyphMap;
            private readonly Dictionary<uint, CharMetrics> _metricsMap;
            public int Ascent { get; }
            public int Descent { get; }
            public int Height { get; }
            public int LineHeight { get; }
            private readonly FontManager _fontManager;

            public FontInstanceHandle(FontManager manager, FontTextureAtlas atlas, int size, Face face,
                Dictionary<char, uint> glyphMap,
                int ascent, int descent, int lineHeight, Dictionary<uint, CharMetrics> metricsMap)
            {
                _fontManager = manager;
                Atlas = atlas;
                Size = size;
                Face = face;
                _glyphMap = glyphMap;
                Ascent = ascent;
                Descent = descent;
                LineHeight = lineHeight;
                Height = ascent + descent;
                _metricsMap = metricsMap;
            }

            public FontTextureAtlas Atlas { get; }

            public Texture GetCharTexture(char chr)
            {
                var glyph = _getGlyph(chr);
                Atlas.AtlasData.TryGetValue(glyph, out var ret);
                return ret;
            }

            public CharMetrics? GetCharMetrics(char chr)
            {
                var glyph = _getGlyph(chr);
                if (glyph == 0)
                {
                    return null;
                }

                _metricsMap.TryGetValue(glyph, out var metrics);
                return metrics;
            }

            private uint _getGlyph(char chr)
            {
                if (_glyphMap.TryGetValue(chr, out var glyph))
                {
                    return glyph;
                }

                return 0;
            }
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
            _configuration.RegisterCVar("display.fontdpi", 72);
        }
    }
}
