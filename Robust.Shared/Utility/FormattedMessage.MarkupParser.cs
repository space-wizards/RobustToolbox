using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Pidgin;
using Robust.Shared.Maths;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Robust.Shared.Utility;

public sealed partial class FormattedMessage
{
    /// <summary>
    /// Runs the given markup through the parser and returns if the markup is valid or not
    /// </summary>
    /// <param name="markup">The markup to check for validity</param>
    /// <returns>true if the markup is valid</returns>
    public static bool ValidMarkup(string markup)
    {
        return TryParse(markup, out _, out _);
    }

    /// <summary>
    /// Attempts to add markup. If an error occurs, it will do nothing and return an error message.
    /// This method does NOT fall back to using the permissive parser (which parses invalid markup as text).
    /// </summary>
    public bool TryAddMarkup(string markup, [NotNullWhen(false)] out string? error)
    {
        if (!TryParse(markup, out var nodes, out error))
            return false;

        _nodes.AddRange(nodes);
        return true;
    }

    [Obsolete("Use AddMarkupOrThrow or TryAddMarkup")]
    public void AddMarkup(string markup) => AddMarkupOrThrow(markup);

    /// <summary>
    /// Parses the given markup and adds the resulting nodes to this formatted message
    /// </summary>
    /// <param name="markup">The markup to parse</param>
    /// <exception cref="ParseException">Thrown when an error occurs while trying to parse the markup.</exception>
    public void AddMarkupOrThrow(string markup)
    {
        _nodes.AddRange(ParseOrThrow(markup));
    }

    /// <summary>
    ///     Same as <see cref="AddMarkup"/> but will attempt to parse invalid markup tags as text.
    /// </summary>
    /// <exception cref="ParseException">Thrown when an error occurs even when using the permissive parser.</exception>
    public void AddMarkupPermissive(string markup, out string? error)
    {
        _nodes.AddRange(ParsePermissive(markup, out error));
    }

    /// <inheritdoc cref="AddMarkupPermissive(string,out string?)"/>
    public void AddMarkupPermissive(string markup) => AddMarkupPermissive(markup, out _);

    /// <summary>
    /// Same as <see cref="AddMarkup"/> but adds a newline too.
    /// </summary>
    [Obsolete]
    public void PushMarkup(string markup)
    {
        AddMarkup(markup);
        PushNewline();
    }

    // > wtf I love parser combinators now
    //   - PJB 13 Oct 2019
    // this tbh - Julian 26 Jan 2023

    /// <summary>
    /// This parser doesn't use backtracking by chaining pidgins parsers in such a way that branches that don't apply
    /// always fail on the first character
    /// </summary>
    private static List<MarkupNode> ParseOrThrow(string input) => ParseNodes.ParseOrThrow(input);

    /// <summary>
    /// Attempt to parse the given input. Returns an error message if it fails. Does not fall back to the permissive parser
    /// </summary>
    public static bool TryParse(string input, [NotNullWhen(true)] out List<MarkupNode>? nodes, [NotNullWhen(false)] out string? error)
    {
        var result = ParseNodes.Parse(input);
        if (result.Success)
        {
            nodes = result.Value;
            error = null;
            return true;
        }

        error = result.Error!.RenderErrorMessage();
        nodes = null;
        return false;
    }

    /// <summary>
    /// Variant of <see cref="TryParse"/> that falls back to using the permissive parser if an error occurs.
    /// </summary>
    /// <exception cref="ParseException">Thrown when an error occurs even when using the permissive parser.</exception>
    public static List<MarkupNode> ParsePermissive(string input, out string? error)
    {
        if (TryParse(input, out var nodes, out error))
            return nodes;

        return ParseNodesSafe.ParseOrThrow(input);
    }

    //TODO: Make Begin and End a cvar
    // Parser definitions for reserved characters
    private static readonly Parser<char, char> Escape = Char('\\');
    private static readonly Parser<char, char> Begin = Char('[');
    private static readonly Parser<char, char> End = Char(']');
    private static readonly Parser<char, char> Quote = Char('"');
    private static readonly Parser<char, char> Equal = Char('=');
    private static readonly Parser<char, char> Slash = Char('/');

    //This just checks for the following characters: /]
    private static readonly Parser<char, Unit> SlashEnd =
        Slash.Then(Whitespaces).Then(End).Then(Return(Unit.Value));

    //This checks for a backslash and one reserved character
    private static readonly Parser<char, char> EscapeSequence =
        Try(Escape.Then(OneOf(Escape, Begin, End, Slash)));

    //Parses text by repeatedly parsing escape sequences or any character except [ and \
    //The result is put into a new markup node representing text (it has no name)
    private static readonly Parser<char, List<MarkupNode>> Text =
         EscapeSequence.Or(Token(c => c != '[' && c != '\\'))
                .AtLeastOnceString()
                .Select(s => new List<MarkupNode>{new(s)});

    //Parses a string of letters or digits beginning with a letter
	private static readonly Parser<char, string> Identifier =
        Parser.Map(
            (first, rest) => first + rest,
            Token(char.IsLetter),
            Token(char.IsLetterOrDigit).ManyString()
        );

    //Parses any character except ". Used for parsing a string parameter wrapped in ""
    private static readonly Parser<char, string> ParameterString =
        Token(c => c != '"').ManyString();

    //Parses eiter a string not wrapped by "" or a hexadecimal value beginning with #
    //The distinction is made by checking if the first character is a pound sign
    //Each character is checked to see if it would be valid in a color name or hex
    private static readonly Parser<char, Color> ParameterColor =
        Parser.Map(
            (first, rest) => CreateColor(first + rest),
            Char('#').Or(Token(char.IsLetter)),
            Token(ValidColorNameContents).ManyString()
        );

    //Parses a parameter by trying to parse each parameter type in order.
    //The order is important because each type parser must fail on the first character if the parameter isn't the correct type
    private static readonly Parser<char, MarkupParameter> Parameter =
        Equal.Before(SkipWhitespaces).Then(ParameterString.Between(Quote).Select(value => new MarkupParameter(value))
            .Or(ParameterColor.Select(color => new MarkupParameter(color)))
            .Or(LongNum.Select(num => new MarkupParameter(num))));

    //This parses an identifier and a parameter value
    private static readonly Parser<char, TagInfo> KeyValuePair =
        Parser.Map(
            (name, parameter) => new TagInfo(name, parameter.GetValueOrDefault()),
            Identifier.Before(SkipWhitespaces),
            Parameter.Optional()
        )
        .Between(SkipWhitespaces);

    //Starts with parsing an opening tag with an parameter and attributes.
    //It then checks if the tag is self closing by parsing either a /] or a ] mapping a boolean for passing into 'CreateTag'
    //This doesn't parse an [ because every non text node starts with a [
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

    //This parses a closing tag by parsing a / first
    //It also doesn't parse a [ for the same reason as 'OpeningTag'
    private static Parser<char, List<MarkupNode>> ClosingTag =>
        Identifier
        .Between(SkipWhitespaces)
        .Between(Slash, End)
        .Select(name => new MarkupNode(name, null, null, true))
        .Select(tag => new List<MarkupNode>{tag});


    //This parses a non text node by first parsing a [ and then either an 'OpeningTag' or 'ClosingTag'
    private static readonly Parser<char, List<MarkupNode>> Tag =
        Begin.Then(OneOf(
            ClosingTag,
            OpeningTag
        ));

    //Chains the text and tag parsers together to create a the list of nodes used by 'FormattedText'
    private static readonly Parser<char, List<MarkupNode>> ParseNodes = Text.Or(Tag).Many().Select(FlattenTagLists);

    //Same as 'ParseNodes' but uses a 'Try' (backtracking) to catch invalid tags
    private static readonly Parser<char, List<MarkupNode>> ParseNodesSafe =
        Text
        .Or(Try(Tag)
        .Or(Any.Select(char.ToString).Select(c => new List<MarkupNode>{new(c)}))).Many().Select(FlattenTagLists);


    /// <summary>
    /// Creates a tag node-
    /// </summary>
    /// <param name="name">The tag name</param>
    /// <param name="parameter">An optional parameter</param>
    /// <param name="attributesEnumerator">A list of attributes</param>
    /// <param name="selfClosing">Whether the node is self closing or node. Self closing nodes immediately </param>
    /// <returns></returns>
    private static List<MarkupNode> CreateTag(string name, MarkupParameter parameter, IEnumerable<TagInfo> attributesEnumerator,  bool selfClosing)
    {
        var attributes = new Dictionary<string, MarkupParameter>();

        foreach (var attribute in attributesEnumerator)
        {
            attributes.TryAdd(attribute.Name, attribute.Parameter);
        }

        var result = new List<MarkupNode>
        {
            new(name,  parameter, attributes)
        };

        if (selfClosing)
            result.Add(new MarkupNode(name, null, null, true));

        return result;
    }

    /// <summary>
    /// Both node parsers return a list containing short lists of parsed tags.
    /// This method flattens that list.
    /// </summary>
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
