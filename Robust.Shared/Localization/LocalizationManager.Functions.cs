#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using Linguini.Bundle;
using Linguini.Bundle.Types;
using Linguini.Shared.Types.Bundle;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.IoC;

namespace Robust.Shared.Localization
{
    internal sealed partial class LocalizationManager
    {
        private void AddBuiltInFunctions(FluentBundle bundle)
        {
            // Grammatical gender / pronouns
            AddCtxFunction(bundle, "GENDER", FuncGender);
            AddCtxFunction(bundle, "SUBJECT", FuncSubject);
            AddCtxFunction(bundle, "OBJECT", FuncObject);
            AddCtxFunction(bundle, "POSS-ADJ", FuncPossAdj);
            AddCtxFunction(bundle, "POSS-PRONOUN", FuncPossPronoun);
            AddCtxFunction(bundle, "REFLEXIVE", FuncReflexive);

            // Conjugation
            AddCtxFunction(bundle, "CONJUGATE-BE", FuncConjugateBe);
            AddCtxFunction(bundle, "CONJUGATE-HAVE", FuncConjugateHave);

            // Proper nouns
            AddCtxFunction(bundle, "PROPER", FuncProper);
            AddCtxFunction(bundle, "THE", FuncThe);

            // Misc
            AddCtxFunction(bundle, "ATTRIB", args => FuncAttrib(bundle, args));
            AddCtxFunction(bundle, "CAPITALIZE", FuncCapitalize);
        }

        /// <summary>
        /// Returns the name of the entity passed in, prepended with "the" if it is not a proper noun.
        /// </summary>
        private ILocValue FuncThe(LocArgs args)
        {
            return new LocValueString(GetString("zzzz-the", ("ent", args.Args[0])));
        }

        /// <summary>
        /// Returns the string passed in, with the first letter capitalized.
        /// </summary>
        private ILocValue FuncCapitalize(LocArgs args)
        {
            var input = args.Args[0].Format(new LocContext());
            if (!String.IsNullOrEmpty(input))
                return new LocValueString(input[0].ToString().ToUpper() + input.Substring(1));
            else return new LocValueString("");
        }

        /// <summary>
        /// Returns the gender of the entity passed in; either Male, Female, Neuter or Epicene.
        /// </summary>
        private ILocValue FuncGender(LocArgs args)
        {
            if (args.Args.Count < 1) return new LocValueString(nameof(Gender.Neuter));

            ILocValue entity0 = args.Args[0];
            if (entity0.Value != null)
            {
                EntityUid entity = (EntityUid)entity0.Value;

                if (_entMan.TryGetComponent<GrammarComponent?>(entity, out var grammar) && grammar.Gender.HasValue)
                {
                    return new LocValueString(grammar.Gender.Value.ToString().ToLowerInvariant());
                }

                if (TryGetEntityLocAttrib(entity, "gender", out var gender))
                {
                    return new LocValueString(gender);
                }
            }

            return new LocValueString(nameof(Gender.Neuter));
        }

        /// <summary>
        /// Returns the respective subject pronoun (he, she, they, it) for the entity's gender.
        /// </summary>
        private ILocValue FuncSubject(LocArgs args)
        {
            return new LocValueString(GetString("zzzz-subject-pronoun", ("ent", args.Args[0])));
        }

        /// <summary>
        /// Returns the respective object pronoun (him, her, them, it) for the entity's gender.
        /// </summary>
        private ILocValue FuncObject(LocArgs args)
        {
            return new LocValueString(GetString("zzzz-object-pronoun", ("ent", args.Args[0])));
        }

        /// <summary>
        /// Returns the respective possessive adjective (his, her, their, its) for the entity's gender.
        /// </summary>
        private ILocValue FuncPossAdj(LocArgs args)
        {
            return new LocValueString(GetString("zzzz-possessive-adjective", ("ent", args.Args[0])));
        }

        /// <summary>
        /// Returns the respective possessive pronoun (his, hers, theirs, its) for the entity's gender.
        /// </summary>
        private ILocValue FuncPossPronoun(LocArgs args)
        {
            return new LocValueString(GetString("zzzz-possessive-pronoun", ("ent", args.Args[0])));
        }

        /// <summary>
        /// Returns the respective reflexive pronoun (himself, herself, themselves, itself) for the entity's gender.
        /// </summary>
        private ILocValue FuncReflexive(LocArgs args)
        {
            return new LocValueString(GetString("zzzz-reflexive-pronoun", ("ent", args.Args[0])));
        }

        /// <summary>
        /// Returns the respective conjugated form of "to be" (is for male/female/neuter, are for epicene) for the entity's gender.
        /// </summary>
        private ILocValue FuncConjugateBe(LocArgs args)
        {
            return new LocValueString(GetString("zzzz-conjugate-be", ("ent", args.Args[0])));
        }

        /// <summary>
        /// Returns the respective conjugated form of "to have" (has for male/female/neuter, have for epicene) for the entity's gender.
        /// </summary>
        private ILocValue FuncConjugateHave(LocArgs args)
        {
            return new LocValueString(GetString("zzzz-conjugate-have", ("ent", args.Args[0])));
        }

        private ILocValue FuncAttrib(FluentBundle bundle, LocArgs args)
        {
            if (args.Args.Count < 2) return new LocValueString("other");

            ILocValue entity0 = args.Args[0];
            if (entity0.Value != null)
            {
                EntityUid entity = (EntityUid)entity0.Value;
                ILocValue attrib0 = args.Args[1];
                if (TryGetEntityLocAttrib(entity, attrib0.Format(new LocContext(bundle)), out var attrib))
                {
                    return new LocValueString(attrib);
                }
            }

            return new LocValueString("other");
        }

        /// <summary>
        /// Returns whether the passed in entity's name is proper or not.
        /// </summary>
        private ILocValue FuncProper(LocArgs args)
        {
            if (args.Args.Count < 1) return new LocValueString("false");

            ILocValue entity0 = args.Args[0];
            if (entity0.Value != null)
            {
                EntityUid entity = (EntityUid)entity0.Value;

                if (_entMan.TryGetComponent<GrammarComponent?>(entity, out var grammar) && grammar.ProperNoun.HasValue)
                {
                    return new LocValueString(grammar.ProperNoun.Value.ToString().ToLowerInvariant());
                }

                if (TryGetEntityLocAttrib(entity, "proper", out var proper))
                {
                    return new LocValueString(proper);
                }
            }

            return new LocValueString("false");
        }


        private void AddCtxFunction(FluentBundle ctx, string name, LocFunction function)
        {
            ctx.AddFunction(name, (args, options)
                => CallFunction(function, args, options), out _, InsertBehavior.Overriding);
        }

        private IFluentType CallFunction(LocFunction function,
            IList<IFluentType> positionalArgs, IDictionary<string, IFluentType> namedArgs)
        {
            var args = new ILocValue[positionalArgs.Count];
            for (var i = 0; i < args.Length; i++)
            {
                args[i] = positionalArgs[i].ToLocValue();
            }

            var options = new Dictionary<string, ILocValue>(namedArgs.Count);
            foreach (var (k, v) in namedArgs)
            {
                options.Add(k, v.ToLocValue());
            }

            var argStruct = new LocArgs(args, options);
            return function.Invoke(argStruct).FluentFromVal();
        }

        public void AddFunction(CultureInfo culture, string name, LocFunction function)
        {
            var bundle = _contexts[culture];

            bundle.AddFunction(name, (args, options)
                => CallFunction(function, args, options), out _, InsertBehavior.Overriding);
        }
    }

    internal sealed class FluentLocWrapperType : IFluentType
    {
        public readonly ILocValue WrappedValue;

        public FluentLocWrapperType(ILocValue wrappedValue)
        {
            WrappedValue = wrappedValue;
        }

        public string AsString()
        {
            return WrappedValue.Format(new LocContext());
        }

        public IFluentType Copy()
        {
            return this;
        }
    }

    static class LinguiniAdapter
    {
        internal static ILocValue ToLocValue(this IFluentType arg)
        {
            return arg switch
            {
                FluentNone => new LocValueNone(""),
                FluentNumber number => new LocValueNumber(number),
                FluentString str => new LocValueString(str),
                FluentLocWrapperType value => value.WrappedValue,
                _ => throw new ArgumentOutOfRangeException(nameof(arg)),
            };
        }

        public static IFluentType FluentFromObject(this object obj)
        {
            return obj switch
            {
                ILocValue wrap => new FluentLocWrapperType(wrap),
                EntityUid entity => new FluentLocWrapperType(new LocValueEntity(entity)),
                DateTime dateTime => new FluentLocWrapperType(new LocValueDateTime(dateTime)),
                TimeSpan timeSpan => new FluentLocWrapperType(new LocValueTimeSpan(timeSpan)),
                bool or Enum => (FluentString)obj.ToString()!.ToLowerInvariant(),
                string str => (FluentString)str,
                byte num => (FluentNumber)num,
                sbyte num => (FluentNumber)num,
                short num => (FluentNumber)num,
                ushort num => (FluentNumber)num,
                int num => (FluentNumber)num,
                uint num => (FluentNumber)num,
                long num => (FluentNumber)num,
                ulong num => (FluentNumber)num,
                double dbl => (FluentNumber)dbl,
                float dbl => (FluentNumber)dbl,
                _ => (FluentString)obj.ToString()!,
            };
        }

        public static IFluentType FluentFromVal(this ILocValue locValue)
        {
            return locValue switch
            {
                LocValueNone => FluentNone.None,
                LocValueNumber number => (FluentNumber)number.Value,
                LocValueString str => (FluentString)str.Value,
                _ => new FluentLocWrapperType(locValue),
            };
        }
    }
}
