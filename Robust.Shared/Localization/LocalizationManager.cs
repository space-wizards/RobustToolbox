using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using Fluent.Net;
using Fluent.Net.RuntimeAst;
using JetBrains.Annotations;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.Localization
{
    internal sealed class LocalizationManager : ILocalizationManagerInternal, IPostInjectInit
    {
        [Dependency] private readonly IResourceManager _res = default!;
        [Dependency] private readonly ILogManager _log = default!;

        private ISawmill _logSawmill = default!;
        private readonly Dictionary<CultureInfo, MessageContext> _contexts = new();

        private CultureInfo? _defaultCulture;

        void IPostInjectInit.PostInject()
        {
            _logSawmill = _log.GetSawmill("loc");
        }

        public string GetString(string messageId)
        {
            if (_defaultCulture == null)
                return messageId;

            if (!TryGetString(messageId, out var msg))
            {
                _logSawmill.Warning("Unknown messageId ({culture}): {messageId}", _defaultCulture.Name, messageId);
                msg = messageId;
            }

            return msg;
        }

        public bool TryGetString(string messageId, [NotNullWhen(true)] out string? value)
        {
            if (!TryGetNode(messageId, out var context, out var node))
            {
                value = null;
                return false;
            }

            return DoFormat(messageId, out value, context, node);
        }

        public string GetString(string messageId, params (string, object)[] args0)
        {
            if (_defaultCulture == null)
                return messageId;

            if (!TryGetString(messageId, out var msg, args0))
            {
                _logSawmill.Warning("Unknown messageId ({culture}): {messageId}", _defaultCulture.Name, messageId);
                msg = messageId;
            }

            return msg;
        }

        public bool TryGetString(string messageId, [NotNullWhen(true)] out string? value,
            params (string, object)[] args0)
        {
            if (!TryGetNode(messageId, out var context, out var node))
            {
                value = null;
                return false;
            }

            var args = new Dictionary<string, object>();
            foreach (var (k, v) in args0)
            {
                var val = v switch
                {
                    IEntity entity => new LocValueEntity(entity),
                    bool or Enum => v.ToString()!.ToLowerInvariant(),
                    _ => v,
                };

                if (val is ILocValue locVal)
                    val = new FluentLocWrapperType(locVal);

                args.Add(k, val);
            }

            return DoFormat(messageId, out value, context, node, args);
        }

        private bool TryGetNode(
            string messageId,
            [NotNullWhen(true)] out MessageContext? ctx,
            [NotNullWhen(true)] out Node? node)
        {
            if (_defaultCulture == null)
            {
                ctx = null;
                node = null;
                return false;
            }

            ctx = _contexts[_defaultCulture];
            string? attribName = null;

            if (messageId.Contains('.'))
            {
                var split = messageId.Split('.');
                messageId = split[0];
                attribName = split[1];
            }

            var message = ctx.GetMessage(messageId);

            if (message == null)
            {
                node = null;
                return false;
            }

            if (attribName != null)
            {
                if (message.Attributes == null || !message.Attributes.TryGetValue(attribName, out var attrib))
                {
                    node = null;
                    return false;
                }

                node = attrib;
            }
            else
            {
                node = message;
            }

            return true;
        }

        public void ReloadLocalizations()
        {
            foreach (var (culture, context) in _contexts.ToArray())
            {
                // Fluent.Net doesn't allow us to remove messages so...
                var newContext = new MessageContext(
                    culture.Name,
                    new MessageContextOptions
                    {
                        UseIsolating = false,
                        Functions = context.Functions
                    }
                );

                _contexts[culture] = newContext;

                _loadData(_res, culture, newContext);
            }
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

        private static ILocValue ValFromFluent(object arg)
        {
            return arg switch
            {
                FluentNone none => new LocValueNone(none.Value),
                FluentNumber number => new LocValueNumber(double.Parse(number.Value)),
                FluentString str => new LocValueString(str.Value),
                FluentDateTime dateTime =>
                    new LocValueDateTime(DateTime.Parse(dateTime.Value, null, DateTimeStyles.RoundtripKind)),
                FluentLocWrapperType wrap => wrap.WrappedValue,
                _ => throw new ArgumentOutOfRangeException(nameof(arg))
            };
        }

        private static FluentType ValToFluent(ILocValue arg)
        {
            return arg switch
            {
                LocValueNone =>
                    throw new NotSupportedException("Cannot currently return LocValueNone from loc functions."),
                LocValueNumber number => new FluentNumber(number.Value.ToString("R")),
                LocValueString str => new FluentString(str.Value),
                LocValueDateTime dateTime => new FluentDateTime(dateTime.Value),
                _ => new FluentLocWrapperType(arg)
            };
        }

        private bool DoFormat(string messageId, out string? value, MessageContext context, Node node, IDictionary<string, object>? args = null)
        {
            var errs = new List<FluentError>();
            try
            {
                value = context.Format(node, args, errs);
            }
            catch (Exception e)
            {
                _logSawmill.Error("{culture}/{messageId}: {exception}", _defaultCulture!.Name, messageId, e);
                value = null;
                return false;
            }

            foreach (var err in errs)
            {
                _logSawmill.Error("{culture}/{messageId}: {error}", _defaultCulture!.Name, messageId, err);
            }

            return true;
        }

        /// <summary>
        /// Remnants of the old Localization system.
        /// It exists to prevent source errors and allow existing game text to *mostly* work
        /// </summary>
        [Obsolete]
        [StringFormatMethod("text")]
        public string GetString(string text, params object[] args)
        {
            return string.Format(text, args);
        }

        public CultureInfo? DefaultCulture
        {
            get => _defaultCulture;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (!_contexts.ContainsKey(value))
                {
                    throw new ArgumentException("That culture is not yet loaded and cannot be used.", nameof(value));
                }

                _defaultCulture = value;
                CultureInfo.CurrentCulture = value;
                CultureInfo.CurrentUICulture = value;
            }
        }

        public void LoadCulture(CultureInfo culture)
        {
            var context = new MessageContext(
                culture.Name,
                new MessageContextOptions
                {
                    UseIsolating = false,
                    // Have to pass empty dict here or else Fluent.Net will fuck up
                    // and share the same dict between multiple message contexts.
                    // Yes, you read that right.
                    Functions = new Dictionary<string, Resolver.ExternalFunction>(),
                }
            );

            _contexts.Add(culture, context);

            _loadData(_res, culture, context);
            if (DefaultCulture == null)
            {
                DefaultCulture = culture;
            }
        }

        public void AddLoadedToStringSerializer(IRobustMappedStringSerializer serializer)
        {
            /*
             * TODO: need to expose Messages on MessageContext in Fluent.NET
            serializer.AddStrings(StringIterator());

            IEnumerable<string> StringIterator()
            {
                foreach (var context in _contexts.Values)
                {
                    foreach (var (key, translations) in _context)
                    {
                        yield return key;

                        foreach (var t in translations)
                        {
                            yield return t;
                        }
                    }
                }
            }
            */
        }

        private void _loadData(IResourceManager resourceManager, CultureInfo culture, MessageContext context)
        {
            // Load data from .ftl files.
            // Data is loaded from /Locale/<language-code>/*

            var root = new ResourcePath($"/Locale/{culture.Name}/");

            var files = resourceManager.ContentFindFiles(root).ToArray();

            var resources = files.AsParallel().Select(path =>
            {
                using var fileStream = resourceManager.ContentFileRead(path);
                using var reader = new StreamReader(fileStream, EncodingHelpers.UTF8);

                var resource = FluentResource.FromReader(reader);
                return (path, resource);
            });

            foreach (var (path, resource) in resources)
            {
                var errors = context.AddResource(resource);
                foreach (var error in errors)
                {
                    _logSawmill.Warning("{path}: {exception}", path, error.Message);
                }
            }
        }

        private sealed class FluentLocWrapperType : FluentType
        {
            public readonly ILocValue WrappedValue;

            public FluentLocWrapperType(ILocValue wrappedValue)
            {
                WrappedValue = wrappedValue;
            }

            public override string Format(MessageContext ctx)
            {
                return WrappedValue.Format(new LocContext(ctx));
            }

            public override bool Match(MessageContext ctx, object obj)
            {
                return false;
                /*var strVal = obj is IFluentType ft ? ft.Value : obj.ToString() ?? "";
                return WrappedValue.Matches(new LocContext(ctx), strVal);*/
            }
        }
    }
}
