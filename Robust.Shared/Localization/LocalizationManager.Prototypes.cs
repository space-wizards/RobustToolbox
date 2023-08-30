using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Linguini.Bundle.Errors;
using Robust.Shared.Prototypes;

namespace Robust.Shared.Localization;

internal sealed partial class LocalizationManager
{
    // Concurrent dict so that multiple threads "reading" .Name won't cause a concurrent write issue
    // when the cache gets populated.
    private readonly ConcurrentDictionary<string, PrototypeLocData> _prototypeCache = new();

    private void FlushPrototypeCache()
    {
        _logSawmill.Debug("Flushing entity localization cache.");
        _entityCache.Clear();
    }

    // Flush caches conservatively on prototype/localization changes.
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.ByType.ContainsKey(typeof(EntityPrototype)))
            FlushEntityCache();

        FlushPrototypeCache();
    }

    private PrototypeLocData CalcInheritingPrototypeLoc<T>(string prototypeId) where T : class, IPrototype, IInheritingPrototype, ILocalizedPrototype
    {
        return CalcInheritingPrototypeLoc(typeof(T), prototypeId);
    }

    private PrototypeLocData CalcInheritingPrototypeLoc(Type kind, string prototypeId)
    {
        string? name = null;
        string? desc = null;

        foreach (var prototype in _prototype.EnumerateParents(kind, prototypeId, true))
        {
            var lproto = (ILocalizedPrototype)prototype;
            var locId = $"{lproto.CustomLocalizationPrefix}-{prototypeId}";

            if (TryGetMessage(locId, out var bundle, out var msg))
            {
                // Localization override exists.
                var msgAttrs = msg.Attributes;

                if (name == null && msg.Value != null)
                {
                    // Only set name if the value isn't empty.
                    // So that you can override *only* a desc.
                    name = bundle.FormatPattern(msg.Value, null, out var fmtErr);
                    WriteWarningForErrs(fmtErr, locId);
                }

                if (msgAttrs.Count != 0)
                {
                    var allErrors = new List<FluentError>();
                    if (desc == null
                        && !bundle.TryGetMsg(locId, "desc", null, out var err1, out desc))
                    {
                        desc = null;
                        allErrors.AddRange(err1);
                    }

                    WriteWarningForErrs(allErrors, locId);
                }
            }

            name ??= lproto.SetName;
            desc ??= lproto.SetDesc;
        }

        return new PrototypeLocData(
            name ?? "",
            desc ?? "");
    }

    private PrototypeLocData CalcPrototypeLoc<T>(string prototypeId) where T : class, IPrototype, ILocalizedPrototype
    {
        return CalcPrototypeLoc(typeof(T), prototypeId);
    }

    private PrototypeLocData CalcPrototypeLoc(Type kind, string prototypeId)
    {
        string? name = null;
        string? desc = null;

        // Return empty strings if no prototype found
        if (!_prototype.TryIndex(kind, prototypeId, out var prototype))
            return new PrototypeLocData("", "");

        var lproto = (ILocalizedPrototype)prototype;
        var locId = $"{lproto.CustomLocalizationPrefix}-{prototypeId}";

        if (!TryGetMessage(locId, out var bundle, out var msg)
            || msg.Value == null)
            return new PrototypeLocData(lproto.SetName ?? "", lproto.SetDesc ?? "");

        name = bundle.FormatPattern(msg.Value, null, out var fmtErr);

        if (!bundle.TryGetMsg(locId, "desc", null, out var err1, out desc))
        {
            return new PrototypeLocData(name ?? "", lproto.SetDesc ?? "");
        }

        return new PrototypeLocData(name ?? "", desc);
    }

    public PrototypeLocData GetPrototypeData(Type kind, string prototypeId)
    {
        if (kind.IsAssignableTo(typeof(IInheritingPrototype)))
            return _prototypeCache.GetOrAdd(prototypeId, (id, t) => t.CalcInheritingPrototypeLoc(kind, id), this);
        return _prototypeCache.GetOrAdd(prototypeId, (id, t) => t.CalcPrototypeLoc(kind, id), this);
    }

    public PrototypeLocData GetPrototypeData<T>(string prototypeId) where T: class, IPrototype, ILocalizedPrototype
    {
        return GetPrototypeData(typeof(T), prototypeId);
    }
}
