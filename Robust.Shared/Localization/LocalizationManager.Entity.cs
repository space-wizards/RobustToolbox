using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Linguini.Bundle;
using Linguini.Bundle.Errors;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Robust.Shared.Localization
{
    internal abstract partial class LocalizationManager
    {
        // Concurrent dict so that multiple threads "reading" .Name won't cause a concurrent write issue
        // when the cache gets populated.
        private readonly ConcurrentDictionary<string, EntityLocData> _entityCache = new();

        private void FlushEntityCache()
        {
            _logSawmill.Debug("Flushing entity localization cache.");
            _entityCache.Clear();
        }

        private bool TryGetEntityLocAttrib(EntityUid entity, string attribute, [NotNullWhen(true)] out string? value)
        {
            if (_entMan.TryGetComponent(entity, out GrammarComponent? grammar) &&
                grammar.Attributes.TryGetValue(attribute, out value))
            {
                return true;
            }

            if (_entMan.GetComponent<MetaDataComponent>(entity).EntityPrototype is not {} prototype)
            {
                value = null;
                return false;
            }

            var data = GetEntityData(prototype.ID);
            return data.Attributes.TryGetValue(attribute, out value);
        }

        // Flush caches conservatively on prototype/localization changes.
        private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
        {
            if (args.WasModified<EntityPrototype>())
                FlushEntityCache();
        }

        private EntityLocData CalcEntityLoc(string prototypeId)
        {
            string? name = null;
            string? desc = null;
            string? suffix = null;

            Dictionary<string, string>? attributes = null;

            // A child can have a locale entry just to override its suffix, while
            // still inheriting the description from a parent. Hold these errors
            // until we know no parent supplied a description either.
            List<(string locId, List<FluentError> errs)>? deferredDescErrors = null;

            foreach (var (id, prototype) in _prototype.EnumerateAllParents<EntityPrototype>(prototypeId, true))
            {
                var locId = prototype?.CustomLocalizationID ?? $"ent-{id}";

                if (TryGetMessage(locId, out var bundle, out var msg))
                {
                    // Localization override exists.
                    var msgAttrs = msg.Attributes;

                    if (name == null && msg.Value != null)
                    {
                        // Only set name if the value isn't empty.
                        // So that you can override *only* a desc/suffix.
                        var locName = bundle.FormatPattern(msg.Value, null, out var fmtErr);
                        WriteWarningForErrs(fmtErr, locId);

                        if (!string.IsNullOrEmpty(locName))
                            name = locName;
                    }

                    if (msgAttrs.Count != 0)
                    {
                        foreach (var (attrId, pattern) in msg.Attributes)
                        {
                            var attrib = attrId.ToString();
                            if (attrib.Equals("desc")
                                || attrib.Equals("suffix"))
                                continue;

                            attributes ??= new Dictionary<string, string>();
                            if (!attributes.ContainsKey(attrib))
                            {
                                var value = bundle.FormatPattern(pattern, null, out var errors);
                                WriteWarningForErrs(errors, locId);
                                attributes[attrib] = value;
                            }
                        }

                        if (desc == null
                            && !bundle.TryGetMessage(locId, "desc", null, out var err1, out desc))
                        {
                            desc = null;
                            // Defer: a parent prototype may still provide .desc.
                            deferredDescErrors ??= new List<(string, List<FluentError>)>();
                            deferredDescErrors.Add((locId, err1.ToList()));
                        }

                        if (suffix == null
                            && !bundle.TryGetMessage(locId, "suffix", null, out var err, out suffix))
                        {
                            suffix = null;
                        }
                    }
                }

                if (prototype == null)
                    continue;

                name ??= prototype.SetName;
                desc ??= prototype.SetDesc;
                suffix ??= prototype.SetSuffix;

                if (prototype.LocProperties.Count != 0)
                {
                    foreach (var (attrib, value) in prototype.LocProperties)
                    {
                        attributes ??= new Dictionary<string, string>();
                        if (!attributes.ContainsKey(attrib))
                        {
                            attributes[attrib] = value;
                        }
                    }
                }
            }

            // Plenty of prototypes have no description at all, such as spawners,
            // markers, and debug/admin entities. Keep the missing .desc warning
            // visible for debugging, but don't treat it as a real localization issue.
            if (desc == null && deferredDescErrors != null)
            {
                foreach (var (locId, errs) in deferredDescErrors)
                {
                    foreach (var err in errs)
                    {
                        _logSawmill.Debug("Missing .desc for `{locId}`\n{e1}", locId, err);
                    }
                }
            }

            // Attempt to infer suffix from entity categories
            if (suffix == null)
            {
                var suffixes = _prototype.Index<EntityPrototype>(prototypeId).Categories
                    .Where(x => x.Suffix != null)
                    .Select(x => GetString(x.Suffix!));
                suffix = string.Join(", ", suffixes);
            }

            return new EntityLocData(
                name ?? "",
                desc ?? "",
                suffix,
                attributes?.ToImmutableDictionary() ?? ImmutableDictionary<string, string>.Empty);
        }


        public EntityLocData GetEntityData(string prototypeId)
        {
            return _entityCache.GetOrAdd(prototypeId, (id, t) => t.CalcEntityLoc(id), this);
        }
    }
}
