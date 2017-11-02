using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using SS14.Client.Graphics.Sprites;
using SS14.Client.ResourceManagement;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    /// <summary>
    ///     Panel that displays read-only rich text
    /// </summary>
    class RichTextPanel : Panel
    {
        public Font Font { get; set; }

        public FormattedTextStream Text { get; }

        public RichTextPanel()
        {
            var font = _resourceCache.GetResource<FontResource>(String.Empty);
            Text = new FormattedTextStream(font);
        }

        protected override void DrawContents()
        {
            base.DrawContents();

            // draw the text
            Text.Draw(Position.X, Position.Y);
        }

        protected override void OnCalcRect()
        {
            base.OnCalcRect();

            // recalculate size & line endings
            Text.Width = Width;
            Text.Height = Height;
            Text.RecalculateLineBreaks();
        }

        internal class FormattedTextStream
        {
            // guesstimate of some buffer sizes
            private const int maxLineChars = 80;
            private const int maxLines = 24;
            private const int maxCapacity = 80 * 24;

            private readonly Font _font;

            private readonly List<char> _charBuffer;
            private readonly List<int> _lineBreaks;

            private int _scrOffX;
            private int _scrOffY;
            private readonly uint _fntSize;

            private readonly TextSprite _textSprite;
            private readonly StringBuilder _sb;

            public int MaxCapacity { get; }

            public int Width { get; set; }
            public int Height { get; set; }
            public int LineOffset { get; set; }
            
            public FormattedTextStream(Font font)
            {
                _font = font;
                _fntSize = 16;
                MaxCapacity = maxCapacity;
                _charBuffer = new List<char>(maxCapacity);
                _lineBreaks = new List<int>(maxLines);

                _textSprite = new TextSprite(String.Empty, _font, _fntSize);

                _sb = new StringBuilder(maxLineChars);
            }

            public int GetLineSpacing()
            {
               return (int) _font.GetLineSpacing((uint)_fntSize);
            }

            public Box2 GetGlyphBounds(char glyph, bool bold, float outline)
            {
                return _font.GetGlyph(glyph, _fntSize, bold, outline).Bounds;
            }

            public void Append(char character)
            {
                
            }

            public void Append(string str)
            {
                var length = str.Length;
                var lines = str.Split('\n');

                // remove at least length chars from top of buffer so we don't go over

                // add our lines and returns
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    _charBuffer.AddRange(line);
                    _lineBreaks.Add(_charBuffer.Count);
                }
            }

            public void Append(TextSprite.Styles style)
            {
                
            }

            public void Append(Color4 color)
            {
                
            }

            public void Draw(int scrX, int scrY)
            {
                _scrOffX = scrX;
                _scrOffY = scrY;

                var curX = 0;
                var curY = 0;

                var blockX = 0;
                var lbIndex = 0;

                var lbPos = _lineBreaks.Count > 0 ? _lineBreaks[lbIndex] : 0;

                // let the parsing begin!
                var length = _charBuffer.Count;
                for (var i = 0; i < length && curY < Height; i++)
                {
                    var chr = _charBuffer[i];
                    var chrSz = ChrWidth(chr);

                    // check linebreak
                    if (i == lbPos)
                    {
                        DrawString(blockX, curY, _sb.ToString());
                        _sb.Clear();
                        AddLine(ref curX, ref curY);
                        blockX = 0;

                        if (_lineBreaks.Count > lbIndex + 2)
                        {
                            lbIndex++;
                            lbPos = _lineBreaks[lbIndex];
                        }
                        else
                        {
                            // position will nerver be reached
                            lbPos = length + 1;
                        }

                        i--;
                        continue;
                    }

                    // draw line
                    if (curX + chrSz > Width)
                    {
                        DrawString(blockX, curY, _sb.ToString());
                        _sb.Clear();
                        AddLine(ref curX, ref curY);
                        blockX = 0;
                        i--; // backup to re-process char
                        continue;
                    }

                    // Default: append char
                    curX += chrSz;
                    _sb.Append(chr);

                    // end of buffer
                    if (i == length - 1)
                    {
                        DrawString(blockX, curY, _sb.ToString());
                    }
                }

                _sb.Clear();

            }

            public void RecalculateLineBreaks()
            {
                
            }

            private void AddLine(ref int x, ref int y)
            {
                x = 0;
                y += GetLineSpacing();
            }

            private void DrawString(int x, int y, string str)
            {
                _textSprite.Text = str;
                _textSprite.Position = new Shared.Maths.Vector2(x + _scrOffX, y + _scrOffY);
                _textSprite.FillColor = Color.DarkSlateBlue;
                _textSprite.Draw();
            }

            private int ChrWidth(char chr)
            {
                return (int) _font.GetGlyph(chr, _fntSize, false, 0).Advance;
            }

            private struct FormattingMark
            {
                
            }
        }
    }
}
