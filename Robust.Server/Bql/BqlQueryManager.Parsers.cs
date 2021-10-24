using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pidgin;
using Robust.Shared.GameObjects;
using static Pidgin.Parser;

namespace Robust.Server.Bql
{
    public partial class BqlQueryManager
    {
        static Parser<char, T> Tok<T>(Parser<char, T> p) =>
            Try(p).Before(SkipWhitespaces);

        private static Parser<char, string> Word => Tok(
            from chars in OneOf(LetterOrDigit, Char('_')).ManyString()
            select chars
        );

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
            Try(Char('$').Then(Tok(
                from chars in OneOf(Uppercase, Char('_')).ManyString()
                select chars
            ))).MapWithInput((x, _) => new SubstitutionData(x.ToString()));

        private static Parser<char, int> Integer =>
            Try(Tok(Int(10)));

        private static Parser<char, object> SubstitutableInteger =>
            Objectify(Integer).Or(Objectify(Try(Substitution)));

        private static Parser<char, double> Float =>
            Try(Tok(Real));

        private static Parser<char, object> SubstitutableFloat =>
            Objectify(Float).Or(Objectify(Try(Substitution)));

        private static Parser<char, double> Percentage =>
            Try(Tok(Real).Before(Char('%')));

        private static Parser<char, object> SubstitutablePercentage =>
            Objectify(Percentage).Or(Objectify(Try(Substitution)));

        private static Parser<char, EntityUid> EntityId =>
            Try(Parser.Map(x => new EntityUid(x), Tok(Int(10))));

        private static Parser<char, object> SubstitutableEntityId =>
            Objectify(EntityId).Or(Objectify(Try(Substitution)));

        private static Parser<char, Type> Component =>
            Try(Parser.Map(t => _componentFactory.GetRegistration(t).Type, Word));

        private static Parser<char, object> SubstitutableComponent =>
            Objectify(Component).Or(Objectify(Try(Substitution)));

        private static Parser<char, string> String =>
            Try(Word); //TODO: Actually parse a string input.

        private static Parser<char, object> SubstitutableString =>
            Objectify(String).Or(Objectify(Try(Substitution)));

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
                Objectify(String)
            });

        private Parser<char, BqlQuerySelectorParsed> BqlQueryParser(string token)
        {
            var inst = _queriesByToken[token];

            if (inst.Arguments.Length == 0)
                return new Pidgin.Parser.String;

            List<Parser<char, object>> argsParsers = new();

            foreach (var (arg, idx) in inst.Arguments.Select((x, i) => (x, i)))
            {
                List<Parser<char, object>> choices = new();
                if ((arg & QuerySelectorArgument.String) == QuerySelectorArgument.String)
                {
                    choices.Add(SubstitutableString);
                }
                if ((arg & QuerySelectorArgument.Component) == QuerySelectorArgument.Component)
                {
                    choices.Add(SubstitutableComponent);
                }
                if ((arg & QuerySelectorArgument.EntityId) == QuerySelectorArgument.EntityId)
                {
                    choices.Add(SubstitutableEntityId);
                }
                if ((arg & QuerySelectorArgument.Integer) == QuerySelectorArgument.Integer)
                {
                    choices.Add(SubstitutableInteger);
                }
                if ((arg & QuerySelectorArgument.Percentage) == QuerySelectorArgument.Percentage)
                {
                    choices.Add(SubstitutablePercentage);
                }
                if ((arg & QuerySelectorArgument.Float) == QuerySelectorArgument.Float)
                {
                    choices.Add(SubstitutableFloat);
                }
                argsParsers.Add(OneOf(choices).Labelled("argument "+idx));
            }

            Parser<char, List<object>>? finalParser = argsParsers[0].Map(x => new List<object> { x });

            foreach (var parser in )
            {

            }
        }
    }
}
