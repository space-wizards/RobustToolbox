using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using Robust.Client.Utility;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SharpFont;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Robust.Client.Graphics
{
    internal sealed class FontManager : IFontManagerInternal
    {
        private const int SheetWidth = 256;
        private const int SheetHeight = 256;

        private readonly IClyde _clyde;

        private uint _baseFontDpi = 96;

        private readonly Library _library;

        private readonly Dictionary<(FontFaceHandle, int fontSize), FontInstanceHandle> _loadedInstances =
            new();

        public FontManager(IClyde clyde)
        {
            _clyde = clyde;
            _library = new Library();
        }

        public IFontFaceHandle Load(Stream stream)
        {
            var face = new Face(_library, stream.CopyToArray(), 0);
            var handle = new FontFaceHandle(face);
            return handle;
        }

        void IFontManagerInternal.SetFontDpi(uint fontDpi)
        {
            _baseFontDpi = fontDpi;
        }

        public IFontInstanceHandle MakeInstance(IFontFaceHandle handle, int size)
        {
            var fontFaceHandle = (FontFaceHandle) handle;
            if (_loadedInstances.TryGetValue((fontFaceHandle, size), out var instance))
            {
                return instance;
            }

            instance = new FontInstanceHandle(this, size, fontFaceHandle);

            _loadedInstances.Add((fontFaceHandle, size), instance);
            return instance;
        }

        private ScaledFontData _generateScaledDatum(FontInstanceHandle instance, float scale)
        {
            var ftFace = instance.FaceHandle.Face;
            ftFace.SetCharSize(0, instance.Size, 0, (uint) (_baseFontDpi * scale));

            var ascent = ftFace.Size.Metrics.Ascender.ToInt32();
            var descent = -ftFace.Size.Metrics.Descender.ToInt32();
            var lineHeight = ftFace.Size.Metrics.Height.ToInt32();

            var data = new ScaledFontData(ascent, descent, ascent + descent, lineHeight);


            return data;
        }

        private void CacheGlyph(FontInstanceHandle instance, ScaledFontData scaled, float scale, uint glyph)
        {
            // Check if already cached.
            if (scaled.AtlasData.ContainsKey(glyph))
                return;

            var face = instance.FaceHandle.Face;
            face.SetCharSize(0, instance.Size, 0, (uint) (_baseFontDpi * scale));
            face.LoadGlyph(glyph, LoadFlags.Default, LoadTarget.Normal);
            face.Glyph.RenderGlyph(RenderMode.Normal);

            var glyphMetrics = face.Glyph.Metrics;
            var metrics = new CharMetrics(glyphMetrics.HorizontalBearingX.ToInt32(),
                glyphMetrics.HorizontalBearingY.ToInt32(),
                glyphMetrics.HorizontalAdvance.ToInt32(),
                glyphMetrics.Width.ToInt32(),
                glyphMetrics.Height.ToInt32());

            using var bitmap = face.Glyph.Bitmap;
            if (bitmap.Pitch < 0)
            {
                throw new NotImplementedException();
            }

            if (bitmap.Pitch != 0)
            {
                Image<A8> img;
                switch (bitmap.PixelMode)
                {
                    case PixelMode.Mono:
                    {
                        img = MonoBitMapToImage(bitmap);
                        break;
                    }

                    case PixelMode.Gray:
                    {
                        ReadOnlySpan<A8> span;
                        unsafe
                        {
                            span = new ReadOnlySpan<A8>((void*) bitmap.Buffer, bitmap.Pitch * bitmap.Rows);
                        }

                        img = new Image<A8>(bitmap.Width, bitmap.Rows);

                        span.Blit(
                            bitmap.Pitch,
                            UIBox2i.FromDimensions(0, 0, bitmap.Pitch, bitmap.Rows),
                            img,
                            (0, 0));

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

                OwnedTexture sheet;
                if (scaled.AtlasTextures.Count == 0)
                    sheet = GenSheet();
                else
                    sheet = scaled.AtlasTextures[^1];

                var (sheetW, sheetH) = sheet.Size;

                if (sheetW - scaled.CurSheetX < img.Width)
                {
                    scaled.CurSheetX = 0;
                    scaled.CurSheetY = scaled.CurSheetMaxY;
                }

                if (sheetH - scaled.CurSheetY < img.Height)
                {
                    // Make new sheet.
                    scaled.CurSheetY = 0;
                    scaled.CurSheetX = 0;
                    scaled.CurSheetMaxY = 0;

                    sheet = GenSheet();
                }

                sheet.SetSubImage((scaled.CurSheetX, scaled.CurSheetY), img);

                var atlasTexture = new AtlasTexture(
                    sheet,
                    UIBox2.FromDimensions(
                        scaled.CurSheetX,
                        scaled.CurSheetY,
                        bitmap.Width,
                        bitmap.Rows));

                scaled.AtlasData.Add(glyph, atlasTexture);

                scaled.CurSheetMaxY = Math.Max(scaled.CurSheetMaxY, scaled.CurSheetY + bitmap.Rows);
                scaled.CurSheetX += bitmap.Width;
            }
            else
            {
                scaled.AtlasData.Add(glyph, null);
            }

            scaled.MetricsMap.Add(glyph, metrics);

            OwnedTexture GenSheet()
            {
                var sheet = _clyde.CreateBlankTexture<A8>((SheetWidth, SheetHeight),
                    $"font-{face.FamilyName}-{instance.Size}-{(uint) (_baseFontDpi * scale)}-sheet{scaled.AtlasTextures.Count}");
                scaled.AtlasTextures.Add(sheet);
                return sheet;
            }
        }

        private static Image<A8> MonoBitMapToImage(FTBitmap bitmap)
        {
            DebugTools.Assert(bitmap.PixelMode == PixelMode.Mono);
            DebugTools.Assert(bitmap.Pitch > 0);

            ReadOnlySpan<byte> span;
            unsafe
            {
                span = new ReadOnlySpan<byte>((void*) bitmap.Buffer, bitmap.Rows * bitmap.Pitch);
            }

            var bitmapImage = new Image<A8>(bitmap.Width, bitmap.Rows);
            for (var y = 0; y < bitmap.Rows; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    var byteIndex = y * bitmap.Pitch + (x / 8);
                    var bitIndex = x % 8;

                    var bit = (span[byteIndex] & (1 << (7 - bitIndex))) != 0;
                    bitmapImage[x, y] = new A8(bit ? byte.MaxValue : byte.MinValue);
                }
            }

            return bitmapImage;
        }

        private sealed class FontFaceHandle : IFontFaceHandle
        {
            public Face Face { get; }

            public FontFaceHandle(Face face)
            {
                Face = face;
            }
        }

        [PublicAPI]
        private sealed class FontInstanceHandle : IFontInstanceHandle
        {
            public FontFaceHandle FaceHandle { get; }
            public int Size { get; }
            private readonly Dictionary<float, ScaledFontData> _scaledData = new();
            private readonly FontManager _fontManager;
            public readonly Dictionary<Rune, uint> GlyphMap;

            public FontInstanceHandle(FontManager fontManager, int size, FontFaceHandle faceHandle)
            {
                GlyphMap = new Dictionary<Rune, uint>();
                _fontManager = fontManager;
                Size = size;
                FaceHandle = faceHandle;
            }

            public Texture? GetCharTexture(Rune codePoint, float scale)
            {
                var glyph = GetGlyph(codePoint);
                if (glyph == 0)
                    return null;

                var scaled = GetScaleDatum(scale);
                _fontManager.CacheGlyph(this, scaled, scale, glyph);

                scaled.AtlasData.TryGetValue(glyph, out var texture);
                return texture;
            }

            public CharMetrics? GetCharMetrics(Rune codePoint, float scale)
            {
                var glyph = GetGlyph(codePoint);
                if (glyph == 0)
                {
                    return null;
                }

                var scaled = GetScaleDatum(scale);
                _fontManager.CacheGlyph(this, scaled, scale, glyph);

                return scaled.MetricsMap[glyph];
            }

            public int GetAscent(float scale)
            {
                var scaled = GetScaleDatum(scale);
                return scaled.Ascent;
            }

            public int GetDescent(float scale)
            {
                var scaled = GetScaleDatum(scale);
                return scaled.Descent;
            }

            public int GetHeight(float scale)
            {
                var scaled = GetScaleDatum(scale);
                return scaled.Height;
            }

            public int GetLineHeight(float scale)
            {
                var scaled = GetScaleDatum(scale);
                return scaled.LineHeight;
            }

            private uint GetGlyph(Rune chr)
            {
                if (GlyphMap.TryGetValue(chr, out var glyph))
                {
                    return glyph;
                }

                // Check FreeType to see if it exists.
                var index = FaceHandle.Face.GetCharIndex((uint) chr.Value);

                GlyphMap.Add(chr, index);

                return index;
            }

            private ScaledFontData GetScaleDatum(float scale)
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

        private sealed class ScaledFontData
        {
            public ScaledFontData(int ascent, int descent, int height, int lineHeight)
            {
                Ascent = ascent;
                Descent = descent;
                Height = height;
                LineHeight = lineHeight;
            }

            public readonly List<OwnedTexture> AtlasTextures = new();
            public readonly Dictionary<uint, AtlasTexture?> AtlasData = new();
            public readonly Dictionary<uint, CharMetrics> MetricsMap = new();
            public readonly int Ascent;
            public readonly int Descent;
            public readonly int Height;
            public readonly int LineHeight;

            public int CurSheetX;
            public int CurSheetY;
            public int CurSheetMaxY;
        }
    }
}
