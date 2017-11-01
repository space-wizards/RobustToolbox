using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics;
using SS14.Client.Graphics.Sprites;

namespace SS14.Client.UserInterface.Controls
{
    /// <summary>
    ///     Panel that displays read-only rich text
    /// </summary>
    class RichTextPanel : Panel
    {
        public Font Font { get; set; }

        public string Text { get; set; }

        public RichTextPanel()
        {

        }

        private class FormattedTextStream
        {
            private Font _font;
            
            public FormattedTextStream(Font font)
            {
                
            }

            public static int GetLineSpacing(Font font, int size)
            {
                throw new NotImplementedException();
            }

            public int GetGlyphWidth(char glyph)
            {
                throw new NotImplementedException();
            }

            private struct MyStruct
            {
                
            }
        }
    }
}
