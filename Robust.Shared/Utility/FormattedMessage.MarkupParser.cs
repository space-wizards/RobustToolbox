using System.Collections.Generic;
using Pidgin;
using Robust.Shared.Maths;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;
namespace Robust.Shared.Utility;

public sealed partial class FormattedMessage
{
    public static bool ValidMarkup(string markup)
    {
        return ParseResult(markup).Success;
    }

    public void AddMarkup(string markup)
    {
        _nodes.AddRange(Parse(markup));
    }

    /// <summary>
    ///     Will parse invalid markup tags as text.
    /// </summary>
    public void AddMarkupPermissive(string markup)
    {
        _nodes.AddRange(ParseSafe(markup));
    }

    // > wtf I love parser combinators now
    //   - PJB 13 Oct 2019
    // this tbh - Julian 26 Jan 2023
    private static IEnumerable<MarkupNode> Parse(string input) => ParseNodes.ParseOrThrow(input);

    private static IEnumerable<MarkupNode> ParseSafe(string input) => ParseNodesSafe.ParseOrThrow(input);
    private static Result<char, List<MarkupNode>> ParseResult(string input) => ParseNodes.Parse(input);

    private static readonly Parser<char, char> Escape = Char('\\');
    private static readonly Parser<char, char> Begin = Char('[');
    private static readonly Parser<char, char> End = Char(']');
    private static readonly Parser<char, char> Quote = Char('"');
    private static readonly Parser<char, char> Equal = Char('=');
    private static readonly Parser<char, char> Slash = Char('/');

    private static readonly Parser<char, Unit> SlashEnd =
        Slash.Then(Whitespaces).Then(End).Then(Return(Unit.Value));

    private static readonly Parser<char, char> EscapeSequence =
        Escape.Then(OneOf(Escape, Begin, End, Slash));

    private static readonly Parser<char, List<MarkupNode>> Text =
         EscapeSequence.Or(Token(c => c != '[' && c != '\\'))
                .AtLeastOnceString()
                .Select(s => new MarkupNode(s))
                .Select(tag => new List<MarkupNode>{tag});

	private static readonly Parser<char, string> Identifier =
        Parser.Map(
            (first, rest) => first + rest,
            Token(char.IsLetter),
            Token(char.IsLetterOrDigit).ManyString()
        );

    private static readonly Parser<char, string> ParameterString =
        Token(c => c != '"').ManyString();

    private static readonly Parser<char, Color> ParameterColor =
        Parser.Map(
            (first, rest) => CreateColor(first + rest),
            Char('#').Or(Token(char.IsLetter)),
            Token(ValidColorNameContents).ManyString()
        );

    private static readonly Parser<char, MarkupParameter> Parameter =
        Equal.Before(SkipWhitespaces).Then(ParameterString.Between(Quote).Select(value => new MarkupParameter(value))
            .Or(ParameterColor.Select(color => new MarkupParameter(color)))
            .Or(LongNum.Select(num => new MarkupParameter(num))));

    private static readonly Parser<char, TagInfo> KeyValuePair =
        Parser.Map(
            (name, parameter) => new TagInfo(name, parameter.GetValueOrDefault()),
            Identifier.Before(SkipWhitespaces),
            Parameter.Optional()
        )
        .Between(SkipWhitespaces);

    private static readonly Parser<char, List<MarkupNode>> OpeningTag =
        Parser.Map(
            (body, attributes, isSelfClosing) => CreateTag(body.Name, body.Parameter, attributes, isSelfClosing),
            KeyValuePair,
            KeyValuePair.Many(),
            OneOf(
                SlashEnd.Select(_ =>  true),
                End.Select(_ => false)
            )
        );

    private static Parser<char, List<MarkupNode>> ClosingTag =>
        Identifier
        .Between(SkipWhitespaces)
        .Between(Slash, End)
        .Select(name => new MarkupNode(name, null, null, true))
        .Select(tag => new List<MarkupNode>{tag});


    private static readonly Parser<char, List<MarkupNode>> Tag =
        Begin.Then(OneOf(
            ClosingTag,
            OpeningTag
        ));

    private static readonly Parser<char, List<MarkupNode>> ParseNodes = Text.Or(Tag).Many().Select(FlattenTagLists);

    private static readonly Parser<char, List<MarkupNode>> ParseNodesSafe =
        Text
        .Or(Try(Tag)
        .Or(Any.Select(char.ToString).Select(c => new List<MarkupNode>{new(c)}))).Many().Select(FlattenTagLists);


    private static List<MarkupNode> CreateTag(string name, MarkupParameter parameter, IEnumerable<TagInfo> attributesEnumerator,  bool selfClosing)
    {
        var attributes = new Dictionary<string, MarkupParameter>();

        foreach (var attribute in attributesEnumerator)
        {
            attributes.TryAdd(attribute.Name, attribute.Parameter);
        }

        var result = new List<MarkupNode>
        {
            //Just pass null for attributes when the node doesn'T have any
            new(name,  parameter, attributes.Count > 0 ? attributes : null)
        };

        if (selfClosing)
            result.Add(new MarkupNode(name, null, null, true));

        return result;
    }

    private static List<MarkupNode> FlattenTagLists(IEnumerable<List<MarkupNode>> tagLists)
    {
        var result = new List<MarkupNode>();

        foreach (var tagList in tagLists)
        {
            result.AddRange(tagList);
        }

        return result;
    }

    private record struct TagInfo(string Name, MarkupParameter Parameter);

    private static bool ValidColorNameContents(char c)
    {
        // Match contents of valid color name.
        return c is '#' or >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9';
    }

    private static Color CreateColor(string nameOrHex)
    {
        if (Color.TryFromName(nameOrHex, out var nameColor))
            return nameColor;

        return Color.TryFromHex(nameOrHex) ?? Color.Black;
    }
}
