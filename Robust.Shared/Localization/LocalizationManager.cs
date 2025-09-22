using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text;
using Linguini.Bundle;
using Linguini.Bundle.Builder;
using Linguini.Bundle.Errors;
using Linguini.Shared.Types.Bundle;
using Linguini.Syntax.Ast;
using Linguini.Syntax.Parser;
using Linguini.Syntax.Parser.Error;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.Localization
{
    internal abstract partial class LocalizationManager : ILocalizationManagerInternal
    {
        protected static readonly ResPath LocaleDirPath = new("/Locale");
        protected static readonly ResPath SupplementalDirPath = new("/Locale/supplemental");

        [Dependency] private readonly IConfigurationManager _configuration = default!;
        [Dependency] private readonly IResourceManager _res = default!;
        [Dependency] private readonly ILogManager _log = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        private ISawmill _logSawmill = default!;
        private readonly Dictionary<CultureInfo, (FluentBundle, CldrResource)> _contexts = new();

        private CldrSupplementalData _supplemental = new(null);
        private (CultureInfo, FluentBundle, CldrResource)? _defaultCulture;
        private (CultureInfo, FluentBundle, CldrResource)[] _fallbackCultures = Array.Empty<(CultureInfo, FluentBundle, CldrResource)>();

        void ILocalizationManager.Initialize() => Initialize();

        public virtual void Initialize()
        {
            _logSawmill = _log.GetSawmill("loc");
            _prototype.PrototypesReloaded += OnPrototypesReloaded;

            ReadSupplementalData(_res, ref _supplemental);
        }

        public CultureInfo SetDefaultCulture()
        {
            var code = _configuration.GetCVar(CVars.LocCultureName);

            var culture = CultureInfo.GetCultureInfo(code, predefinedOnly: false);
            SetCulture(culture);

            // Return the culture for further work with it,
            // like of adding functions
            return culture;
        }

        public string GetString(string messageId)
        {
            if (_defaultCulture == null)
                return messageId;

            if (!TryGetString(messageId, out var msg))
            {
                _logSawmill.Debug("Unknown messageId ({culture}): {messageId}", _defaultCulture.Value.Item1.Name,
                    messageId);
                msg = messageId;
            }

            return msg;
        }

        #region get string

        public string GetString(string messageId, (string, object) arg)
        {
            if (_defaultCulture == null)
                return messageId;

            if (TryGetString(messageId, out var argMsg, arg))
                return argMsg;

            _logSawmill.Debug("Unknown messageId ({culture}): {messageId}", _defaultCulture.Value.Item1.Name,
                messageId);
            return messageId;
        }

        public string GetString(string messageId, (string, object) arg1, (string, object) arg2)
        {
            if (_defaultCulture == null)
                return messageId;

            if (TryGetString(messageId, out var argMsg, arg1, arg2))
                return argMsg;

            _logSawmill.Debug("Unknown messageId ({culture}): {messageId}", _defaultCulture.Value.Item1.Name,
                messageId);
            return messageId;
        }

        public string GetString(string messageId, params (string, object)[] args)
        {
            if (_defaultCulture == null)
                return messageId;

            if (TryGetString(messageId, out var argMsg, args))
                return argMsg;

            _logSawmill.Debug("Unknown messageId ({culture}): {messageId}", _defaultCulture.Value.Item1.Name,
                messageId);
            return messageId;
        }

        #endregion

        public bool HasString(string messageId)
        {
            return HasMessage(messageId, out _);
        }

        #region TryGetString

        public bool TryGetString(string messageId, [NotNullWhen(true)] out string? value)
        {
            if (_defaultCulture == null)
            {
                value = null;
                return false;
            }

            if (TryGetString(messageId, _defaultCulture.Value, out value))
                return true;

            foreach (var fallback in _fallbackCultures)
            {
                if (TryGetString(messageId, fallback, out value))
                    return true;
            }

            value = null;
            return false;
        }

        public bool TryGetString(string messageId,
            (CultureInfo, FluentBundle, CldrResource) bundle,
            [NotNullWhen(true)] out string? value)
        {
            return TryGetString(messageId, (bundle.Item1, bundle.Item2), out value);
        }

        public bool TryGetString(string messageId,
            (CultureInfo, FluentBundle) bundle,
            [NotNullWhen(true)] out string? value)
        {
            try
            {
                if (bundle.Item2.TryGetAttrMessage(messageId, null, out var errors, out value))
                    return true;

                if (errors != null)
                {
                    foreach (var err in errors)
                    {
                        _logSawmill.Error("{culture}/{messageId}: {error}", bundle.Item1.Name, messageId, err);
                    }
                }

                return false;
            }
            catch (Exception e)
            {
                _logSawmill.Error("{culture}/{messageId}: {exception}", bundle.Item1.Name, messageId, e);
                value = null;
                return false;
            }
        }

        public bool TryGetString(string messageId,
            [NotNullWhen(true)] out string? value,
            (string, object) arg)
        {
            // TODO LINGUINI add try-get-message variant that takes in a (string, object)[]
            // I.e., have it automatically call FluentFromObject(context) with the right context if the message exists
            // This allows us to get rid of this message check.
            if (!HasMessage(messageId, out var culture))
            {
                value = null;
                return false;
            }

            var (info, bundle, _) = culture.Value;
            var context = new LocContext(bundle, this);
            var args = new Dictionary<string, IFluentType>
            {
                { arg.Item1, arg.Item2.FluentFromObject(context) }
            };

            return TryGetString(messageId, out value, args, bundle, info);
        }

        public bool TryGetString(string messageId,
            [NotNullWhen(true)] out string? value,
            (string, object) arg1,
            (string, object) arg2)
        {
            // TODO LINGUINI add try-get-message variant that takes in a (string, object)[]
            // I.e., have it automatically call FluentFromObject(context) with the right context if the message exists
            // This allows us to get rid of this message check.
            if (!HasMessage(messageId, out var culture))
            {
                value = null;
                return false;
            }

            var (info, bundle, _) = culture.Value;
            var context = new LocContext(bundle, this);
            var args = new Dictionary<string, IFluentType>
            {
                { arg1.Item1, arg1.Item2.FluentFromObject(context) },
                { arg2.Item1, arg2.Item2.FluentFromObject(context) }
            };

            return TryGetString(messageId, out value, args, bundle, info);
        }

        public bool TryGetString(string messageId,
            [NotNullWhen(true)] out string? value,
            params (string, object)[] keyArgs)
        {
            // TODO LINGUINI add try-get-message variant that takes in a (string, object)[]
            // I.e., have it automatically call FluentFromObject(context) with the right context if the message exists
            // This allows us to get rid of this message check.
            if (!HasMessage(messageId, out var culture))
            {
                value = null;
                return false;
            }

            var (info, bundle, _) = culture.Value;
            var context = new LocContext(bundle, this);
            var args = new Dictionary<string, IFluentType>(keyArgs.Length);
            foreach (var (k, v) in keyArgs)
            {
                args.Add(k, v.FluentFromObject(context));
            }

            return TryGetString(messageId, out value, args, bundle, info);
        }

        public bool TryGetString(string messageId, [NotNullWhen(true)] out string? value,
            Dictionary<string, IFluentType> args, FluentBundle bundle, CultureInfo culture)
        {
            try
            {
                var result = bundle.TryGetAttrMessage(messageId, args, out var errs, out value);
                if (errs != null)
                {
                    foreach (var err in errs)
                    {
                        _logSawmill.Error("{culture}/{messageId}: {error}", culture.Name, messageId, err);
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                _logSawmill.Error("{culture}/{messageId}: {exception}", culture.Name, messageId, e);
                value = null;
                return false;
            }
        }

        #endregion

        private bool HasMessage(
            string messageId,
            [NotNullWhen(true)] out (CultureInfo, FluentBundle, CldrResource)? culture)
        {
            if (_defaultCulture == null)
            {
                culture = null;
                return false;
            }

            var idx = messageId.IndexOf('.');
            if (idx != -1)
                messageId = messageId.Remove(idx);

            culture = _defaultCulture;
            if (culture.Value.Item2.HasMessage(messageId))
                return true;

            foreach (var fallback in _fallbackCultures)
            {
                culture = fallback;
                if (culture.Value.Item2.HasMessage(messageId))
                    return true;
            }

            culture = null;
            return false;
        }

        private bool TryGetMessage(
            string messageId,
            [NotNullWhen(true)] out FluentBundle? bundle,
            [NotNullWhen(true)] out AstMessage? message)
        {
            if (_defaultCulture == null)
            {
                bundle = null;
                message = null;
                return false;
            }

            bundle = _defaultCulture.Value.Item2;
            if (bundle.TryGetAstMessage(messageId, out message))
                return true;

            foreach (var fallback in _fallbackCultures)
            {
                bundle = fallback.Item2;
                if (bundle.TryGetAstMessage(messageId, out message))
                    return true;
            }

            bundle = null;
            return false;
        }

        public void ReloadLocalizations()
        {
            foreach (var (culture, data) in _contexts)
            {
                var cldr = data.Item2;
                _loadData(_res, culture, data.Item1, ref cldr);
                _contexts[culture] = (data.Item1, cldr);
            }
            ReadSupplementalData(_res, ref _supplemental);

            FlushEntityCache();
        }

        public CultureInfo? DefaultCulture
        {
            get => _defaultCulture?.Item1;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (!_contexts.TryGetValue(value, out var data))
                {
                    throw new ArgumentException("That culture is not yet loaded and cannot be used.", nameof(value));
                }

                _defaultCulture = (value, data.Item1, data.Item2);
                CultureInfo.CurrentCulture = value;
                CultureInfo.CurrentUICulture = value;
            }
        }

        public void SetCulture(CultureInfo culture)
        {
            if (!HasCulture(culture))
                LoadCulture(culture);

            if (DefaultCulture?.NameEquals(culture) ?? false)
                return;

            DefaultCulture = culture;
        }

        public bool HasCulture(CultureInfo culture)
        {
            return _contexts.ContainsKey(culture);
        }

        private void LoadCultureNoParents(CultureInfo culture)
        {
            if (HasCulture(culture))
                return;

            var bundle = LinguiniBuilder.Builder()
                .CultureInfo(culture)
                .SkipResources()
                .SetUseIsolating(false)
                .UseConcurrent()
                .UncheckedBuild();

            AddBuiltInFunctions(bundle);
            _initData(_res, culture, bundle, out var cldr);
            _contexts.Add(culture, (bundle, cldr));
        }

        private void LoadCultureParents(CultureInfo culture)
        {
            // Attempting to load an already loaded culture
            if (HasCulture(culture))
                throw new InvalidOperationException("Culture is already loaded");

            var parent = culture;
            while (parent != parent.Parent)
            {
                LoadCultureNoParents(parent);
                parent = parent.Parent;
            }
        }

        public void LoadCulture(CultureInfo culture)
        {
            LoadCultureParents(culture);
            DefaultCulture ??= culture;
        }

        private CldrPersonListGender ResolvePersonListGender(CultureInfo info)
        {
            if (_supplemental.Gender is not { } genderData)
                return CldrPersonListGender.Neutral;

            if (genderData.PersonList.TryGetValue(new(info), out var personList))
                return personList;

            var parent = info;
            while (parent != parent.Parent)
            {
                parent = parent.Parent;

                if (genderData.PersonList.TryGetValue(new(parent), out var parentPersonList))
                    return parentPersonList;
            }

            return CldrPersonListGender.Neutral;
        }

        public Dictionary<CldrListPatternKey, CldrListPatternParts>? ResolveListPatterns(CultureInfo info)
        {
            if (!_contexts.TryGetValue(info, out var data))
                return null;

            if (data.Item2.ListPatterns is not null)
                return data.Item2.ListPatterns;

            var parent = info;
            while (parent != parent.Parent)
            {
                parent = parent.Parent;
                if (_contexts.TryGetValue(parent, out data) && data.Item2.ListPatterns is not null)
                    return data.Item2.ListPatterns;
            }

            return null;
        }

        private Dictionary<CldrListPatternKey, CldrListPatternParts>? FindListPatterns(CultureInfo? provided)
        {
            if (provided is { } providedCulture && ResolveListPatterns(providedCulture) is { } providedPatterns)
                return providedPatterns;

            if (_defaultCulture is not { } defaultCulture)
                return null;

            if (ResolveListPatterns(defaultCulture.Item1) is { } patterns)
                return patterns;

            foreach (var fallback in _fallbackCultures)
            {
                if (ResolveListPatterns(fallback.Item1) is { } fallbackPatterns)
                    return fallbackPatterns;
            }

            return null;
        }

        private string FormatList(List<string> strings, ListType type = ListType.And, ListWidth width = ListWidth.Wide, CultureInfo? culture = null)
        {
            if (FindListPatterns(culture) is not { } patterns || !patterns.TryGetValue(new(type, width), out var parts))
                throw new InvalidOperationException($"List pattern data is missing for {_defaultCulture?.Item1} or fallbacks");

            return parts.FormatList(strings);
        }

        public string FormatList(List<string> strings, ListType type = ListType.And, ListWidth width = ListWidth.Wide)
        {
            return FormatList(strings, type, width, null);
        }

        public List<CultureInfo> GetFoundCultures()
        {
            var result = new List<CultureInfo>();
            foreach (var name in _res.ContentGetDirectoryEntries(LocaleDirPath))
            {
                // Remove last "/" symbol
                // Example "en-US/" -> "en-US"
                var cultureName = name.TrimEnd('/');
                result.Add(CultureInfo.GetCultureInfo(cultureName, predefinedOnly: false));
            }

            return result;
        }

        public void SetFallbackCluture(params CultureInfo[] cultures)
        {
            _fallbackCultures = Array.Empty<(CultureInfo, FluentBundle, CldrResource)>();
            var tuples = new (CultureInfo, FluentBundle, CldrResource)[cultures.Length];
            var i = 0;
            foreach (var culture in cultures)
            {
                if (!_contexts.TryGetValue(culture, out var data))
                    throw new ArgumentException("That culture is not loaded.", nameof(culture));

                tuples[i++] = (culture, data.Item1, data.Item2);
            }

            _fallbackCultures = tuples;
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

        private void _loadData(IResourceManager resourceManager, CultureInfo culture, FluentBundle context, ref CldrResource cldr)
        {
            var resources = ReadLocaleFolder(resourceManager, culture);

            foreach (var (path, resource, data) in resources)
            {
                var errors = resource.Errors;
                context.AddResourceOverriding(resource);
                WriteWarningForErrs(path, errors, data);
            }

            ReadCldrData(resourceManager, culture, ref cldr);
        }

        private void _initData(IResourceManager resourceManager, CultureInfo culture, FluentBundle context, out CldrResource cldr)
        {
            var resources = ReadLocaleFolder(resourceManager, culture);

            var resErrors = new List<LocError>();
            foreach (var (path, resource, data) in resources)
            {
                var errors = resource.Errors;
                WriteWarningForErrs(path, errors, data);
                if (!context.InsertResourcesAndReport(resource, path, out var errs))
                {
                    resErrors.AddRange(errs);
                }

            }

            if (resErrors.Count > 0)
            {
                WriteLocErrors(resErrors);
            }

            cldr = new CldrResource(CldrIdentity.FromCultureInfo(culture), null);
            ReadCldrData(resourceManager, culture, ref cldr);
        }

        private void ReadCldrData(IResourceManager resourceManager, CultureInfo culture, ref CldrResource cldr)
        {
            var cldrResources = ReadCldrLocaleFolder(resourceManager, culture);

            foreach (var (path, resource) in cldrResources)
            {
                try
                {
                    cldr = CldrResource.Merge(cldr, resource);
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to merge into CLDR resource at {path}", e);
                }
            }
        }

        private void ReadSupplementalData(IResourceManager resourceManager, ref CldrSupplementalData cldr)
        {
            var cldrSupplemental = ReadCldrSupplementalFolder(resourceManager);

            foreach (var supplemental in cldrSupplemental)
            {
                cldr = CldrSupplementalData.Merge(cldr, supplemental);
            }
        }

        private static ParallelQuery<(ResPath path, Resource resource, string contents)> ReadLocaleFolder(
            IResourceManager resourceManager, CultureInfo culture)
        {
            // Load data from .ftl files.
            // Data is loaded from /Locale/<language-code>/*

            var root = LocaleDirPath / culture.Name;

            var files = resourceManager.ContentFindFiles(root)
                .Where(c => c.Filename.EndsWith(".ftl", StringComparison.InvariantCultureIgnoreCase))
                .ToArray();

            var resources = files.AsParallel().Select(path =>
            {
                string contents;

                using (var fileStream = resourceManager.ContentFileRead(path))
                using (var reader = new StreamReader(fileStream, EncodingHelpers.UTF8))
                {
                    contents = reader.ReadToEnd();
                }

                var parser = new LinguiniParser(contents);
                var resource = parser.Parse();
                return (path, resource, contents);
            });
            return resources;
        }

        private static ParallelQuery<(ResPath, CldrResource)> ReadCldrLocaleFolder(
            IResourceManager resourceManager, CultureInfo culture)
        {
            var root = LocaleDirPath / culture.Name;

            var files = resourceManager.ContentFindFiles(root)
                .Where(c => c.Filename.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
                .ToArray();

            return files.AsParallel().SelectMany(path => ReadCldrResources(resourceManager, path));
        }

        private static ParallelQuery<CldrSupplementalData> ReadCldrSupplementalFolder(
            IResourceManager resourceManager)
        {
            var files = resourceManager.ContentFindFiles(SupplementalDirPath)
                .Where(c => c.Filename.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
                .ToArray();

            return files.AsParallel().Select(path =>
            {
                using var fileStream = resourceManager.ContentFileRead(path);

                return JsonSerializer.Deserialize<CldrBundle>(fileStream)!.Supplemental;
            }).OfType<CldrSupplementalData>();
        }

        private static IEnumerable<(ResPath, CldrResource)> ReadCldrResources(IResourceManager resourceManager, ResPath path)
        {
            using var fileStream = resourceManager.ContentFileRead(path);

            var bundle = JsonSerializer.Deserialize<CldrBundle>(fileStream)!;
            foreach (var resource in bundle.Resources.Values)
            {
                yield return (path, resource);
            }
        }

        private void WriteWarningForErrs(ResPath path, List<ParseError> errs, string resource)
        {
            foreach (var err in errs)
            {
                _logSawmill.Error($"{path}:\n{err.FormatCompileErrors(resource.AsMemory())}");
            }
        }

        private void WriteWarningForErrs(IList<FluentError>? errs, string locId)
        {
            if (errs == null) return;
            foreach (var err in errs)
            {
                _logSawmill.Error("Error extracting `{locId}`\n{e1}", locId, err);
            }
        }

        private void WriteLocErrors(IList<LocError>? errs)
        {
            if (errs == null) return;
            var sbErr = new StringBuilder();
            foreach (var err in errs)
            {
                sbErr.Append(err).AppendLine();
            }
            _logSawmill.Error(sbErr.ToString());
        }
    }
}
