using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Linguini.Bundle;
using Linguini.Bundle.Builder;
using Linguini.Bundle.Errors;
using Linguini.Shared.Types.Bundle;
using Linguini.Syntax.Ast;
using Linguini.Syntax.Parser;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.Localization
{
    internal sealed partial class LocalizationManager : ILocalizationManagerInternal, IPostInjectInit
    {
        [Dependency] private readonly IResourceManager _res = default!;
        [Dependency] private readonly ILogManager _log = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;

        private ISawmill _logSawmill = default!;
        private readonly Dictionary<CultureInfo, FluentBundle> _contexts = new();

        private CultureInfo? _defaultCulture;

        void IPostInjectInit.PostInject()
        {
            _logSawmill = _log.GetSawmill("loc");
            _prototype.PrototypesReloaded += OnPrototypesReloaded;
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

        public bool TryGetString(string messageId, [NotNullWhen(true)] out string? value)
        {
            return TryGetString(messageId, out value, null);
        }

        public bool TryGetString(string messageId, [NotNullWhen(true)] out string? value,
            params (string, object)[]? keyArgs)
        {
            var args = new Dictionary<string, IFluentType>();
            if (keyArgs != null)
            {
                foreach (var (k, v) in keyArgs)
                {
                    IFluentType val = v switch
                    {
                        IEntity entity => new LocValueEntity(entity),
                        DateTime dateTime => new LocValueDateTime(dateTime),
                        bool or Enum => (FluentString) v.ToString()!.ToLowerInvariant(),
                        _ => (FluentString) v.ToString()!,
                    };


                    args.Add(k, val);
                }
            }

            if (!HasMessage(messageId, out var bundle))
            {
                value = null;
                return false;
            }

            try
            {
                bundle.TryGetAttrMsg(messageId, args, out var errs, out value);
                foreach (var err in errs)
                {
                    _logSawmill.Error("{culture}/{messageId}: {error}", _defaultCulture!.Name, messageId, err);
                }

                return errs.Count == 0;
            }
            catch (Exception e)
            {
                _logSawmill.Error("{culture}/{messageId}: {exception}", _defaultCulture!.Name, messageId, e);
                value = null;
                return false;
            }
        }

        private bool HasMessage(
            string messageId,
            [NotNullWhen(true)] out FluentBundle? bundle)
        {
            if (_defaultCulture == null)
            {
                bundle = null;
                return false;
            }

            bundle = _contexts[_defaultCulture];
            if (messageId.Contains('.'))
            {
                var split = messageId.Split('.');
                return bundle.HasMessage(split[0]);
            }

            return bundle.HasMessage(messageId);
        }

        private bool TryGetMessage(
            string messageId,
            [NotNullWhen(true)] out FluentBundle? ctx,
            [NotNullWhen(true)] out AstMessage? message)
        {
            if (_defaultCulture == null)
            {
                ctx = null;
                message = null;
                return false;
            }

            ctx = _contexts[_defaultCulture];
            return ctx.TryGetAstMessage(messageId, out message);
        }

        public void ReloadLocalizations()
        {
            foreach (var (culture, context) in _contexts.ToArray())
            {
                _loadData(_res, culture, context);
            }

            FlushEntityCache();
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
            var bundle = LinguiniBuilder.Builder()
                .CultureInfo(culture)
                .SkipResources()
                .SetUseIsolating(false)
                .UncheckedBuild();

            _contexts.Add(culture, bundle);
            AddBuiltInFunctions(bundle);

            _loadData(_res, culture, bundle);
            DefaultCulture ??= culture;
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

        private void _loadData(IResourceManager resourceManager, CultureInfo culture, FluentBundle context)
        {
            // Load data from .ftl files.
            // Data is loaded from /Locale/<language-code>/*

            var root = new ResourcePath($"/Locale/{culture.Name}/");

            var files = resourceManager.ContentFindFiles(root)
                .Where(c => c.Filename.EndsWith(".ftl", StringComparison.InvariantCultureIgnoreCase))
                .ToArray();

            var resources = files.AsParallel().Select(path =>
            {
                using var fileStream = resourceManager.ContentFileRead(path);
                using var reader = new StreamReader(fileStream, EncodingHelpers.UTF8);

                var resource = new LinguiniParser(reader).Parse();
                return (path, resource);
            });

            foreach (var (path, resource) in resources)
            {
                var errors = resource.Errors.Select(e => (FluentError) ParserFluentError.ParseError(e)).ToList();
                context.AddResourceOverriding(resource);
                foreach (var error in errors)
                {
                    _logSawmill.Error("{path}: {exception}", path, error.ToString());
                }
            }
        }
    }
}
