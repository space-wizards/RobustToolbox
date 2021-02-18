using System.Collections.Generic;
using Pidgin;
using Robust.Shared.Maths;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Robust.Shared.Utility
{
    public partial class FormattedMessage
    {
        // wtf I love parser combinators now.
        private const char TagBegin = '[';
        private const char TagEnd = ']';

        private static readonly Parser<char, char> ParseEscapeSequence =
            Char('\\').Then(OneOf(
                Char('\\'),
                Char(TagBegin),
                Char(TagEnd)));

        private static readonly Parser<char, TagText> ParseTagText =
            ParseEscapeSequence.Or(Token(c => c != TagBegin && c != '\\'))
                .AtLeastOnceString()
                .Select(s => new TagText(s));

        private static readonly Parser<char, TagColor> ParseTagColor =
            String("color")
                .Then(Char('='))
                .Then(Token(ValidColorNameContents).AtLeastOnceString()
                    .Select(s =>
                    {
                        if (Color.TryFromName(s, out var color))
                        {
                            return new TagColor(color);
                        }
                        return new TagColor(Color.FromHex(s));
                    }));

        private static readonly Parser<char, TagPop> ParseTagPop =
            Char('/')
            .Then(String("color"))
            .ThenReturn(TagPop.Instance);

        private static readonly Parser<char, Tag> ParseTagContents =
            ParseTagColor.Cast<Tag>().Or(ParseTagPop.Cast<Tag>());

        private static readonly Parser<char, Tag> ParseEnclosedTag =
            ParseTagContents.Between(Char(TagBegin), Char(TagEnd));

        private static readonly Parser<char, Tag> ParseTagOrFallBack =
            Try(ParseEnclosedTag)
                // If we couldn't parse a tag then parse the [ of the start of the tag
                // so the rest is recognized as text.
                .Or(Char(TagBegin).ThenReturn<Tag>(new TagText("[")));

        private static readonly Parser<char, IEnumerable<Tag>> Parse =
            ParseTagText.Cast<Tag>().Or(ParseEnclosedTag).Many();

        private static readonly Parser<char, IEnumerable<Tag>> ParsePermissive =
            ParseTagText.Cast<Tag>().Or(ParseTagOrFallBack).Many();

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
    }
}
