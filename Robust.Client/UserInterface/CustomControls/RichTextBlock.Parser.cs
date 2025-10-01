using System;
using System.Collections.Generic;
using System.Linq;
using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Robust.Client.UserInterface.CustomControls;

public sealed partial class RichTextBox
{
    private abstract record Node;

    private record TextNode(string Text) : Node;

    private record Attribute(string? Name, string? Value);

    private record OpenHeadTag(string Name, IReadOnlyList<Attribute> Attributes, bool IsSelfClosing);

    private record TagNode(string Name, IReadOnlyList<Attribute> Attributes, IReadOnlyList<Node> Children) : Node;

    private record Document(IReadOnlyList<Node> Children);

    private static Document? TryParse(string text)
    {
        var result = Doc.Parse(text);
        return result.Success ? result.Value : null;
    }

    #region Parser

    private static Parser<char, Document> Doc =>
        Rec(() => NodeParser).Many().Select(doc => new Document(doc.ToList())).Before(End);

    private static Parser<char, string> Word =>
        (Letter.Or(Digit).Or(Char('_')).Or(Char('-'))).AtLeastOnceString()
        .Bind(s => Return(s.ToLowerInvariant()));

    private static Parser<char, string> TagName => Word;
    private static Parser<char, string> AttrName => Word;

    private static Parser<char, Unit> SkipSpaces =>
        OneOf(Char(' '), Char('\t'), Char('\r'), Char('\n')).Many().Then(Return(Unit.Value));

    private static Parser<char, string> AttrValue =>
        OneOf(
            Char('"').Then(AnyCharExcept('"').ManyString()).Before(Char('"')),
            Char('\'').Then(AnyCharExcept('\'').ManyString()).Before(Char('\'')),
            AnyCharExcept(' ', '\t', '\r', '\n', ']').AtLeastOnceString()
        );

    private static Parser<char, Attribute> AttributeParser =>
        SkipSpaces.Then(
            OneOf(
                Char('=').Then(AttrValue).Bind(val => Return(new Attribute(null, val))),
                AttrName.Bind(name =>
                    Char('=')
                        .Then(AttrValue)
                        .Optional()
                        .Bind(vopt => Return(new Attribute(name, vopt.HasValue ? vopt.Value : null)))
                )
            )
        );

    private static Parser<char, Node> TextNodeParser =>
        AnyCharExcept('[').AtLeastOnceString().Bind(s => Return((Node)new TextNode(s)));

    private static Parser<char, OpenHeadTag> OpenTagHead =>
        Char('[')
            .Then(TagName)
            .Bind(name =>
                AttributeParser.Many()
                    .Bind(attrs =>
                        SkipSpaces.Then(
                                OneOf(
                                    Char('/').Then(Char(']')).Then(Return(true)),
                                    Char(']').Then(Return(false))
                                )
                            )
                            .Bind(isSelf => Return(new OpenHeadTag(name, attrs.ToList(), isSelf))))
            );

    private static Parser<char, Node> TagNodeParser =>
        OpenTagHead.Bind(open =>
        {
            if (open.IsSelfClosing)
                return Return<Node>(new TagNode(open.Name, open.Attributes, []));

            return Rec(() => NodeParser)
                .Many()
                .Bind(children =>
                    Char('[')
                        .Then(Char('/'))
                        .Then(TagName)
                        .Before(Char(']'))
                        .Bind(closeName =>
                        {
                            if (!string.Equals(closeName, open.Name, StringComparison.OrdinalIgnoreCase))
                                return Fail<Node>($"Mismatched closing tag: {closeName}");

                            return Return<Node>(new TagNode(open.Name, open.Attributes, children.ToList()));
                        })
                );
        });

    private static Parser<char, Node> NodeParser =>
        Rec(() => OneOf(TextNodeParser, Try(TagNodeParser)));

    #endregion
}
