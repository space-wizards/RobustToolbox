using System;
using System.Text;
using Robust.Shared.Maths;

namespace Robust.Shared.Console
{
    public class FormattedText
    {
        public const int InitialSize = 256;

        public const char CHAR_SET = '§';
        public const char CHAR_POP = '¶';

        public const char CODE_COLOR  = 'c';
        public const char CODE_SIZE = 'p';
        public const char CODE_BOLD   = 'b';
        public const char CODE_ITALIC = 'i';
        public const char CODE_UNDERLINED = 'u';
        public const char CODE_STRIKETHROUGH = 's';
        public const char CODE_IMAGE = 'i';
        public const char CODE_URL = 'h';
        public const char CODE_GLOW = 'g';
        public const char CODE_FLASH = 'f';
        public const char CODE_WAVE = 'w';
        public const char CODE_SCROLL = 'm';
        
        private readonly StringBuilder _sb;

        public FormattedText()
        {
            _sb = new StringBuilder(InitialSize);
        }

        public FormattedText(string text)
        {
            _sb = new StringBuilder(text);
        }

        public FormattedText(FormattedText other)
        {
            _sb = new StringBuilder(other.GetString());
        }

        public static string StripFormatting(string text)
        {
            //TODO: Iterate string and remove all tags
            // requires parsing
            throw new NotImplementedException();
        }

        public static FormattedText FromString(string text)
        {
            return new FormattedText(text);
        }

        public string GetString()
        {
            return _sb.ToString();
        }

        public FormattedText AddText(FormattedText text)
        {
            _sb.Append(text.GetString());
            return this;
        }

        public FormattedText AddString(string text)
        {
            _sb.Append(text);
            return this;
        }

        public FormattedText PushColor(Color color)
        {
            _sb.Append(CHAR_SET);
            _sb.Append(CODE_COLOR);

            // 24bit color to 12bit color, just take the high bits
            // 0b_RRRR_RRRR_GGGG_GGGG_BBBB_BBBB -> 0b_RRRR_GGGG_BBBB
            var hexColor =
                ((color.RByte >> 4) << 8) +
                ((color.GByte >> 4) << 4) +
                (color.BByte >> 4);

            _sb.AppendFormat("{0:X3}", hexColor);

            return this;
        }

        public FormattedText PopColor()
        {
            _sb.Append(CHAR_POP);
            _sb.Append(CODE_COLOR);

            return this;
        }

    }
}
