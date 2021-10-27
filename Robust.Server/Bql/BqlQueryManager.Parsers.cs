using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pidgin;
using Robust.Shared.GameObjects;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Robust.Server.Bql
{
    public partial class BqlQueryManager
    {
        private readonly Dictionary<Type, Parser<char, BqlQuerySelectorParsed>> _parsers = new();
        private Parser<char, BqlQuerySelectorParsed> _allQuerySelectors = default!;
        private Parser<char, (IEnumerable<BqlQuerySelectorParsed>, string)> _simpleQuery => Parser.Map((en, _, rest) => (en, rest), SkipWhitespaces.Then(_allQuerySelectors).Many(), String("do").Then(SkipWhitespaces), Any.ManyString());

        private static Parser<char, string> Word =>
            from chars in OneOf(LetterOrDigit, Char('_')).ManyString()
            select chars;

        private static Parser<char, object> Objectify<T>(Parser<char, T> inp)
        {
            return Parser.Map(x => (object) x!, inp);
        }

        private struct SubstitutionData
        {
            public string Name;

            public SubstitutionData(string name)
            {
                Name = name;
            }
        }

        private static Parser<char, SubstitutionData> Substitution =>
            Try(Char('$').Then(
                from chars in OneOf(Uppercase, Char('_')).ManyString()
                select chars
            )).MapWithInput((x, _) => new SubstitutionData(x.ToString()));

        private static Parser<char, int> Integer =>
            Try(Int(10));

        private static Parser<char, object> SubstitutableInteger =>
            Objectify(Integer).Or(Objectify(Try(Substitution)));

        private static Parser<char, double> Float =>
            Try(Real);

        private static Parser<char, object> SubstitutableFloat =>
            Objectify(Float).Or(Objectify(Try(Substitution)));

        private static Parser<char, double> Percentage =>
            Try(Real).Before(Char('%'));

        private static Parser<char, object> SubstitutablePercentage =>
            Objectify(Percentage).Or(Objectify(Try(Substitution)));

        private static Parser<char, EntityUid> EntityId =>
            Try(Parser.Map(x => new EntityUid(x), Int(10)));

        private static Parser<char, object> SubstitutableEntityId =>
            Objectify(EntityId).Or(Objectify(Try(Substitution)));

        private static Parser<char, Type> Component =>
            Try(Parser.Map(t => _componentFactory.GetRegistration(t).Type, Word));

        private static Parser<char, object> SubstitutableComponent =>
            Objectify(Component).Or(Objectify(Try(Substitution)));

        private static Parser<char, string> QuotedString =>
            OneOf(Try(Char('"').Then(OneOf(new []
            {
                AnyCharExcept("\"")
            }).ManyString().Before(Char('"')))), Try(Word));

        private static Parser<char, object> SubstitutableString =>
            Objectify(QuotedString).Or(Objectify(Try(Substitution)));

        // thing to make sure it all compiles.
        [UsedImplicitly]
        private static Parser<char, object> TypeSystemCheck =>
            OneOf(new[]
            {
                Objectify(Integer),
                Objectify(Percentage),
                Objectify(EntityId),
                Objectify(Component),
                Objectify(Float),
                Objectify(QuotedString)
            });

        private Parser<char, BqlQuerySelectorParsed> BuildBqlQueryParser(BqlQuerySelector inst)
        {
            var leadToken = String(inst.Token);

            if (inst.Arguments.Length == 0)
            {
                return Parser.Map(_ => new BqlQuerySelectorParsed(new List<object>(), inst.Token, false), SkipWhitespaces);
            }

            List<Parser<char, object>> argsParsers = new();

            foreach (var (arg, idx) in inst.Arguments.Select((x, i) => (x, i)))
            {
                List<Parser<char, object>> choices = new();
                if ((arg & QuerySelectorArgument.String) == QuerySelectorArgument.String)
                {
                    choices.Add(Try(SubstitutableString.Before(SkipWhitespaces).Labelled("string argument")));
                }
                if ((arg & QuerySelectorArgument.Component) == QuerySelectorArgument.Component)
                {
                    choices.Add(Try(SubstitutableComponent.Before(SkipWhitespaces).Labelled("component argument")));
                }
                if ((arg & QuerySelectorArgument.EntityId) == QuerySelectorArgument.EntityId)
                {
                    choices.Add(Try(SubstitutableEntityId.Before(SkipWhitespaces).Labelled("entity ID argument")));
                }
                if ((arg & QuerySelectorArgument.Integer) == QuerySelectorArgument.Integer)
                {
                    choices.Add(Try(SubstitutableInteger.Before(SkipWhitespaces).Labelled("integer argument")));
                }
                if ((arg & QuerySelectorArgument.Percentage) == QuerySelectorArgument.Percentage)
                {
                    choices.Add(Try(SubstitutablePercentage.Before(SkipWhitespaces).Labelled("percentage argument")));
                }
                if ((arg & QuerySelectorArgument.Float) == QuerySelectorArgument.Float)
                {
                    choices.Add(Try(SubstitutableFloat.Before(SkipWhitespaces).Labelled("float argument")));
                }

                argsParsers.Add(OneOf(choices));
            }

            Parser<char, List<object>> finalParser = argsParsers[0].Map(x => new List<object> { x });

            for (var i = 1; i < argsParsers.Count; i++)
            {
                finalParser = finalParser.Then(argsParsers[i], (list, o) =>
                {
                    list.Add(o);
                    return list;
                }).Labelled("arguments");
            }

            return Parser.Map(args => new BqlQuerySelectorParsed(args, inst.Token, false), finalParser);
        }

        private void DoParserSetup()
        {
            foreach (var inst in _instances)
            {
                _parsers.Add(inst.GetType(), BuildBqlQueryParser(inst));
            }

            _allQuerySelectors = OneOf(_instances.Select(x => Parser.Map((a, b) => (a, b),
                Try(String("not").Before(Char(' '))).Optional(),
                Try(String(x.Token).Before(Char(' ')))))).Then(tok =>
                _parsers[_queriesByToken[tok.b].GetType()].Map(a =>
                {
                    a.Inverted = tok.a.HasValue;
                    return a;
                })
            );


        }
    }
}
