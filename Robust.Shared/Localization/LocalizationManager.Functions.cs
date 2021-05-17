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

namespace Robust.Shared.Localization
{
    internal sealed partial class LocalizationManager
    {
        private void AddBuiltInFunctions(FluentBundle bundle)
        {
            // Grammatical gender
            AddCtxFunction(bundle, "GENDER", FuncGender);

            // Proper nouns
            AddCtxFunction(bundle, "PROPER", FuncProper);

            // Misc Attribs
            AddCtxFunction(bundle, "ATTRIB", args => FuncAttrib(bundle, args));

            AddCtxFunction(bundle, "THE", FuncThe);
        }

        private ILocValue FuncThe(LocArgs args)
        {
            return new LocValueString(GetString("zzzz-the", ("ent", args.Args[0])));
        }

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

        private ILocValue FuncAttrib(FluentBundle context, LocArgs args)
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
            return function.Invoke(argStruct);
        }

        public void AddFunction(CultureInfo culture, string name, LocFunction function)
        {
            var context = _contexts[culture];

            context.AddFunction(name, (args, options)
                => CallFunction(function, args, options), out _, InsertBehavior.Overriding);
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
                ILocValue value => value,
                _ => throw new ArgumentOutOfRangeException(nameof(arg)),
            };
        }
    }
}
