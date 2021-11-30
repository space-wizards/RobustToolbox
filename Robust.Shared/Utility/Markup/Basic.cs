using System;
using System.Collections.Generic;
using Pidgin;
using Robust.Shared.Maths;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Robust.Shared.Utility.Markup
{
    public class Basic
    {
        internal record tag;
        internal record tagText(string text) : tag;
        internal record tagColor(Color color) : tag;
        internal record tagPop() : tag;

        private List<tag> _tags = new();

        // wtf I love parser combinators now.
        private const char TagBegin = '[';
        private const char TagEnd = ']';

        private static readonly Parser<char, char> ParseEscapeSequence =
            Char('\\').Then(OneOf(
                Char('\\'),
                Char(TagBegin),
                Char(TagEnd)));

        private static readonly Parser<char, tagText> ParseTagText =
            ParseEscapeSequence.Or(Token(c => c != TagBegin && c != '\\'))
                .AtLeastOnceString()
                .Select(s => new tagText(s));

        private static readonly Parser<char, tagColor> ParseTagColor =
            String("color")
                .Then(Char('='))
                .Then(Token(ValidColorNameContents).AtLeastOnceString()
                    .Select(s =>
                    {
                        if (Color.TryFromName(s, out var color))
                            return new tagColor(color);

                        return new tagColor(Color.FromHex(s));
                    }));

        private static readonly Parser<char, tagPop> ParseTagPop =
            Char('/')
            .Then(String("color"))
            .ThenReturn(new tagPop());

        private static readonly Parser<char, tag> ParseTagContents =
            ParseTagColor.Cast<tag>().Or(ParseTagPop.Cast<tag>());

        private static readonly Parser<char, tag> ParseEnclosedTag =
            ParseTagContents.Between(Char(TagBegin), Char(TagEnd));

        private static readonly Parser<char, tag> ParseTagOrFallBack =
            Try(ParseEnclosedTag)
                // If we couldn't parse a tag then parse the [ of the start of the tag
                // so the rest is recognized as text.
                .Or(Char(TagBegin).ThenReturn<tag>(new tagText("[")));

        private static readonly Parser<char, IEnumerable<tag>> Parse =
            ParseTagText.Cast<tag>().Or(ParseEnclosedTag).Many();

        private static readonly Parser<char, IEnumerable<tag>> ParsePermissive =
            ParseTagText.Cast<tag>().Or(ParseTagOrFallBack).Many();

        public static bool ValidMarkup(string markup)
        {
            return Parse.Parse(markup).Success;
        }

        public void AddMarkup(string markup)
        {
            _tags.AddRange(Parse.ParseOrThrow(markup));
        }

        /// <summary>
        ///     Will parse invalid markup tags as text instead of ignoring them.
        /// </summary>
        public void AddMarkupPermissive(string markup)
        {
            _tags.AddRange(ParsePermissive.ParseOrThrow(markup));
        }

        private static bool ValidColorNameContents(char c)
        {
            // Match contents of valid color name.
            if (c == '#')
            {
                return true;
            }

            if (c >= 'a' && c <= 'z')
            {
                return true;
            }

            if (c >= 'A' && c <= 'Z')
            {
                return true;
            }

            if (c >= '0' && c <= '9')
            {
                return true;
            }

            return false;
        }


        public FormattedMessage Render(Section? defStyle = default) => Build(defStyle).Build();

        public FormattedMessage.Builder Build(Section? defStyle = default)
        {
            FormattedMessage.Builder b;
            if (defStyle != null)
                b = FormattedMessage.Builder.FromFormattedMessage(
                        new FormattedMessage(new[] {defStyle.Value})
                    );
            else
                b = new FormattedMessage.Builder();

            foreach (var t in _tags)
            {
                switch (t)
                {
                    case tagText txt:   b.AddText(txt.text); break;
                    case tagColor col:  b.PushColor(col.color); break;
                    case tagPop:        b.Pop(); break;
                }
            }

            return b;
        }

        public static FormattedMessage.Builder BuildMarkup(string text, Section? defStyle = default)
        {
            var nb = new Basic();
            nb.AddMarkup(text);
            return nb.Build(defStyle);
        }

        public static FormattedMessage RenderMarkup(string text, Section? defStyle = default) => BuildMarkup(text, defStyle).Build();

        /// <summary>
        ///     Escape a string of text to be able to be formatted into markup.
        /// </summary>
        public static string EscapeText(string text)
        {
            return text.Replace("\\", "\\\\").Replace("[", "\\[");
        }
    }

    public static class FormattedMessageExtensions
    {
        public static void AddMarkup(this FormattedMessage.Builder bld, string text) => bld.AddMessage(Basic.BuildMarkup(text));

        [Obsolete("Use Basic.EscapeText instead.")]
        public static void EscapeText(this FormattedMessage _, string text) => Basic.EscapeText(text);

        [Obsolete("Use Basic.EscapeText instead.")]
        public static void EscapeText(this FormattedMessage.Builder _, string text) => Basic.EscapeText(text);
    }
}
