using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Linguini.Bundle.Errors;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Robust.Shared.Localization
{
    internal sealed partial class LocalizationManager
    {
        // Concurrent dict so that multiple threads "reading" .Name won't cause a concurrent write issue
        // when the cache gets populated.
        private readonly ConcurrentDictionary<string, EntityLocData> _entityCache = new();

        private void FlushEntityCache()
        {
            _logSawmill.Debug("Flushing entity localization cache.");
            _entityCache.Clear();
        }

        private bool TryGetEntityLocAttrib(IEntity entity, string attribute, [NotNullWhen(true)] out string? value)
        {
            if (entity.TryGetComponent<GrammarComponent>(out var grammar) &&
                grammar.Attributes.TryGetValue(attribute, out value))
            {
                return true;
            }

            if (entity.Prototype == null)
            {
                value = null;
                return false;
            }

            var data = GetEntityData(entity.Prototype.ID);
            return data.Attributes.TryGetValue(attribute, out value);
        }

        // Flush caches conservatively on prototype/localization changes.
        private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
        {
            if (!args.ByType.TryGetValue(typeof(EntityPrototype), out _))
                return;

            FlushEntityCache();
        }

        private EntityLocData CalcEntityLoc(string prototypeId)
        {
            string? name = null;
            string? desc = null;
            string? suffix = null;
            Dictionary<string, string>? attributes = null;

            while (true)
            {
                var prototype = _prototype.Index<EntityPrototype>(prototypeId);
                var locId = prototype.CustomLocalizationID ?? $"ent-{prototypeId}";

                if (TryGetMessage(locId, out var ctx, out var msg))
                {
                    // Localization override exists.
                    var msgAttrs = msg.Attributes;

                    if (name == null && msg.Value != null)
                    {
                        // Only set name if the value isn't empty.
                        // So that you can override *only* a desc/suffix.
                        name = ctx.FormatPattern(msg.Value, null, out var fmtErr);
                        WriteWarningForErrs(fmtErr, locId);
                    }

                    IList<FluentError> errs = new List<FluentError>();
                    if (msgAttrs.Count != 0)
                    {
                        if (desc == null
                            && ctx.TryGetMsg(locId, "desc", null, out errs, out desc))
                        {
                        }

                        WriteWarningForErrs(errs, locId);

                        if (suffix == null
                            && ctx.TryGetMsg(locId, "suffix", null, out errs, out suffix))
                        {
                        }

                        WriteWarningForErrs(errs, locId);


                        foreach (var (attrId, pattern) in msg.Attributes)
                        {
                            var attrib = attrId.ToString();
                            if (attrib.Equals("desc")
                                || attrib.Equals("suffix"))
                                continue;

                            attributes ??= new Dictionary<string, string>();
                            if (!attributes.ContainsKey(attrib))
                            {
                                var value = ctx.FormatPattern(pattern, null, out var errors);
                                WriteWarningForErrs(errors, locId);
                                attributes[attrib] = value;
                            }
                        }
                    }
                }

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

                if (prototype.Parent == null)
                    break;

                prototypeId = prototype.Parent;
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
