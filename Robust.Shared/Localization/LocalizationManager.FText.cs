using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Linguini.Shared.Types.Bundle;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Robust.Shared.Localization;

internal partial class LocalizationManager
{
    private List<CultureInfo>? _availableCulture;
    private CultureInfo? _selectedLang;

    public IEnumerable<CultureInfo> GetAvailableLocalizations(bool forceUpdate = false)
    {
        if (_availableCulture == null || forceUpdate)
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

    public CultureInfo GetSelectedLang()
    {
        if (_selectedLang == null)
        {
            SetSelectedLang();
        }

        return _selectedLang!;
    }

    public void SetSelectedLang(CVarDef<string>? uiLang = null)
    {
        var cultureName = _configurationManager.GetCVar(uiLang ?? CVars.UILang);
        var cultureInfo = CultureInfo.GetCultureInfo(cultureName);
        if (Equals(_selectedLang, cultureInfo))
            return;

        if (!_contexts.ContainsKey(cultureInfo))
        {
            _logSawmill.Warning($"Culture {cultureInfo.DisplayName} is not yet loaded and cannot be used.");
            return;
        }

        _selectedLang = cultureInfo;
        CultureInfo.CurrentUICulture = _selectedLang;
        CultureInfo.DefaultThreadCurrentUICulture = _selectedLang;
    }

    public string GetString(FText messageId)
    {
        if (!TryGetString(messageId, out var msg))
        {
            _logSawmill.Debug("Unknown messageId ({culture}): {messageId}", GetSelectedLang().Name, messageId);
            msg = messageId.Name;
        }

        return msg;
    }

    public bool TryGetString(FText message, [NotNullWhen(true)] out string? value)
    {
        if (!HasMessage(message.Name, out var bundle))
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
                _logSawmill.Error("{culture}/{messageId}: {error}", GetSelectedLang().Name, message.Name, err);
            }

            return result;
        }
        catch (Exception e)
        {
            _logSawmill.Error("{culture}/{messageId}: {exception}", GetSelectedLang().Name, message.Name, e);
            value = null;
            return false;
        }
    }
}
