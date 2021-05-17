using System.Collections.Generic;
using System.Globalization;
using Fluent.Net;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Localization;

namespace Robust.Shared.Localization
{
    internal sealed partial class LocalizationManager
    {
        private void AddBuiltinFunctions(MessageContext context)
        {
            // Grammatical gender
            AddCtxFunction(context, "GENDER", FuncGender);

            // Proper nouns
            AddCtxFunction(context, "PROPER", FuncProper);

            // Misc Attribs
            AddCtxFunction(context, "ATTRIB", args => FuncAttrib(context, args));

            AddCtxFunction(context, "THE", FuncThe);
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
