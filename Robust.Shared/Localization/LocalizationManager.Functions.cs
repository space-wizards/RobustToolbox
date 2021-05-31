using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fluent.Net;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Utility;

namespace Robust.Shared.Localization
{
    internal sealed partial class LocalizationManager
    {
        private void AddBuiltinFunctions(MessageContext context)
        {
            // Grammatical gender / pronouns
            AddCtxFunction(context, "GENDER", FuncGender);
            AddCtxFunction(context, "SUBJECT", FuncSubject);
            AddCtxFunction(context, "OBJECT", FuncObject);
            AddCtxFunction(context, "POSS-ADJ", FuncPossAdj);
            AddCtxFunction(context, "POSS-PRONOUN", FuncPossPronoun);
            AddCtxFunction(context, "REFLEXIVE", FuncReflexive);

            // Conjugation
            AddCtxFunction(context, "CONJUGATE-BE", FuncConjugateBe);
            AddCtxFunction(context, "CONJUGATE-HAVE", FuncConjugateHave);

            // Proper nouns
            AddCtxFunction(context, "PROPER", FuncProper);
            AddCtxFunction(context, "THE", FuncThe);

            // Misc
            AddCtxFunction(context, "ATTRIB", args => FuncAttrib(context, args));
            AddCtxFunction(context, "CAPITALIZE", FuncCapitalize);
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
                return new LocValueString(input.First().ToString().ToUpper() + input.Substring(1));
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
                IEntity entity = (IEntity) entity0.Value;

                if (entity.TryGetComponent<GrammarComponent>(out var grammar) && grammar.Gender.HasValue)
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

        private ILocValue FuncAttrib(MessageContext context, LocArgs args)
        {
            if (args.Args.Count < 2) return new LocValueString("other");

            ILocValue entity0 = args.Args[0];
            if (entity0.Value != null)
            {
                IEntity entity = (IEntity) entity0.Value;
                ILocValue attrib0 = args.Args[1];
                if (TryGetEntityLocAttrib(entity, attrib0.Format(new LocContext(context)), out var attrib))
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
                IEntity entity = (IEntity) entity0.Value;

                if (entity.TryGetComponent<GrammarComponent>(out var grammar) && grammar.ProperNoun.HasValue)
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

        private void AddCtxFunction(MessageContext ctx, string name, LocFunction function)
        {
            ctx.Functions.Add(name, (args, options) => CallFunction(function, args, options));
        }

        public void AddFunction(CultureInfo culture, string name, LocFunction function)
        {
            var context = _contexts[culture];

            context.Functions.Add(name, (args, options) => CallFunction(function, args, options));
        }

        private FluentType CallFunction(
            LocFunction function,
            IList<object> fluentArgs, IDictionary<string, object> fluentOptions)
        {
            var args = new ILocValue[fluentArgs.Count];
            for (var i = 0; i < args.Length; i++)
            {
                args[i] = ValFromFluent(fluentArgs[i]);
            }

            var options = new Dictionary<string, ILocValue>(fluentOptions.Count);
            foreach (var (k, v) in fluentOptions)
            {
                options.Add(k, ValFromFluent(v));
            }

            var argStruct = new LocArgs(args, options);

            var ret = function(argStruct);

            return ValToFluent(ret);
        }
    }
}
