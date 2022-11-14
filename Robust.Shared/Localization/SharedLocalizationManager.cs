using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using Linguini.Bundle;
using Linguini.Bundle.Builder;
using Linguini.Bundle.Errors;
using Linguini.Bundle.Types;
using Linguini.Shared.Types.Bundle;
using Linguini.Syntax.Parser;
using Linguini.Syntax.Parser.Error;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Robust.Shared.Localization;

public sealed class SharedLocalizationManager : IPostInjectInit
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly Dictionary<string, FluentBundle> _contexts = new();
    private readonly ConcurrentDictionary<string, EntityLocData> _entityCache = new();
    private ISawmill _logSawmill = default!;
    private List<CultureInfo>? _availableCulture;
    public string SelectedLang => _configurationManager.GetCVar(CVars.UILang);

    public IEnumerable<CultureInfo> GetAvailableLocalizations()
    {
        if (_availableCulture == null)
        {
            // Lazy evaluation because getting content directory entries is expensive
            _availableCulture = new List<CultureInfo>();
            foreach (var localeDir in _res.ContentGetDirectoryEntries(new ResourcePath("/Locale")))
            {
                // Directories end with / and we add them as such
                if (localeDir.EndsWith("/"))
                {
                    _availableCulture.Add(new CultureInfo(localeDir[..Index.FromEnd(1)]));
                }
            }
        }

        foreach (var cultureInfo in _availableCulture)
        {
            yield return cultureInfo;
        }
    }

    public bool HasMessage(FText message, [NotNullWhen(true)] out FluentBundle? bundle)
    {
        if (!_contexts.TryGetValue(SelectedLang, out bundle))
        {
            bundle = null;
            return false;
        }

        if (message.Name.Contains('.'))
        {
            var split = message.Name.Split('.');
            return bundle.HasMessage(split[0]);
        }

        return bundle.HasMessage(message.Name);
    }

    public FText GetString(string messageId, params (string, object)[] keyArgs)
    {
        return new FText(messageId, keyArgs);
    }

    public FText GetString(string messageId)
    {
        return new FText(messageId);
    }

    public bool TryGetString(FText message, [NotNullWhen(true)] out string? value)
    {
        if (!HasMessage(message, out var bundle))
        {
            value = null;
            return false;
        }

        var context = new LocContext(bundle);
        var args = new Dictionary<string, IFluentType>();
        if (!message.NoArguments)
        {
            foreach (var (k, v) in message.Arguments)
            {
                args.Add(k, v.FluentFromObject(context));
            }
        }

        try
        {
            var result = bundle.TryGetAttrMsg(message.Name, args, out var errs, out value);
            foreach (var err in errs)
            {
                _logSawmill.Error("{culture}/{messageId}: {error}", SelectedLang, message.Name, err);
            }

            return result;
        }
        catch (Exception e)
        {
            _logSawmill.Error("{culture}/{messageId}: {exception}", SelectedLang, message.Name, e);
            value = null;
            return false;
        }
    }


    void IPostInjectInit.PostInject()
    {
        _logSawmill = _log.GetSawmill("loc");
        _prototype.PrototypesReloaded += OnPrototypesReloaded;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.ByType.ContainsKey(typeof(EntityPrototype)))
            return;

        FlushEntityCache();
    }

    public void ReloadLocalizations()
    {
        foreach (var (culture, context) in _contexts)
        {
            CultureInfo info = new CultureInfo(culture);
            _loadData(info, context);
        }

        FlushEntityCache();
    }

    private void _loadData(CultureInfo culture, FluentBundle context)
    {
        // Load data from .ftl files.
        // Data is loaded from /Locale/<language-code>/*

        var root = new ResourcePath($"/Locale/{culture.Name}/");

        var files = _res.ContentFindFiles(root)
            .Where(c => c.Filename.EndsWith(".ftl", StringComparison.InvariantCultureIgnoreCase))
            .ToArray();

        var resources = files.AsParallel().Select(path =>
        {
            using var fileStream = _res.ContentFileRead(path);
            using var reader = new StreamReader(fileStream, EncodingHelpers.UTF8);

            var parser = new LinguiniParser(reader);
            var resource = parser.Parse();
            return (path, resource, parser.GetReadonlyData);
        });

        foreach (var (path, resource, data) in resources)
        {
            var errors = resource.Errors;
            context.AddResourceOverriding(resource);
            WriteWarningForErrs(path, errors, data);
        }
    }

    private void FlushEntityCache()
    {
        _logSawmill.Debug("Flushing entity localization cache.");
        _entityCache.Clear();
    }


    private void WriteWarningForErrs(ResourcePath path, List<ParseError> errs, ReadOnlyMemory<char> resource)
    {
        foreach (var err in errs)
        {
            _logSawmill.Warning("{path}:\n{exception}", path, err.FormatCompileErrors(resource));
        }
    }

    private void WriteWarningForErrs(IList<FluentError> errs, string locId)
    {
        foreach (var err in errs)
        {
            _logSawmill.Warning("Error extracting `{locId}`\n{e1}", locId, err);
        }
    }

    public void LoadCulture(CultureInfo culture)
    {
        var bundle = LinguiniBuilder.Builder()
            .CultureInfo(culture)
            .SkipResources()
            .SetUseIsolating(false)
            .UseConcurrent()
            .UncheckedBuild();

        _contexts.Add(culture.Name, bundle);
        AddBuiltInFunctions(bundle);

        _loadData(culture, bundle);
    }

    #region Functions

    private void AddBuiltInFunctions(FluentBundle bundle)
    {
        // Grammatical gender / pronouns
        // LocalizationManager.AddCtxFunction(bundle, "GENDER", FuncGender);
        // LocalizationManager.AddCtxFunction(bundle, "SUBJECT", FuncSubject);
        // LocalizationManager.AddCtxFunction(bundle, "OBJECT", FuncObject);
        // LocalizationManager.AddCtxFunction(bundle, "POSS-ADJ", FuncPossAdj);
        // LocalizationManager.AddCtxFunction(bundle, "POSS-PRONOUN", FuncPossPronoun);
        // LocalizationManager.AddCtxFunction(bundle, "REFLEXIVE", FuncReflexive);
        //
        // // Conjugation
        // LocalizationManager.AddCtxFunction(bundle, "CONJUGATE-BE", FuncConjugateBe);
        // LocalizationManager.AddCtxFunction(bundle, "CONJUGATE-HAVE", FuncConjugateHave);
        // LocalizationManager.AddCtxFunction(bundle, "CONJUGATE-BASIC", FuncConjugateBasic);
        //
        // // Proper nouns
        // LocalizationManager.AddCtxFunction(bundle, "PROPER", FuncProper);
        // LocalizationManager.AddCtxFunction(bundle, "THE", FuncThe);
        //
        // // Misc
        // LocalizationManager.AddCtxFunction(bundle, "ATTRIB", args => FuncAttrib(bundle, args));
        // LocalizationManager.AddCtxFunction(bundle, "CAPITALIZE", FuncCapitalize);
        // LocalizationManager.AddCtxFunction(bundle, "INDEFINITE", FuncIndefinite);
    }

    #endregion

    public void AddFunction(string selectedLang, string name, LocFunction function)
    {
        var ctx = _contexts[SelectedLang];

        ctx.AddFunction(name, (args, options)
            => CallFunction(function, ctx, args, options), out _, InsertBehavior.Overriding);
    }

    private static IFluentType CallFunction(
        LocFunction function,
        FluentBundle bundle,
        IList<IFluentType> positionalArgs,
        IDictionary<string, IFluentType> namedArgs)
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
        return function.Invoke(argStruct).FluentFromVal(new LocContext(bundle));
    }
}
