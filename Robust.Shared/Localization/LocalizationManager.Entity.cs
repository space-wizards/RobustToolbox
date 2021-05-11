using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;

namespace Robust.Shared.Localization
{
    internal sealed partial class LocalizationManager
    {
        private readonly Dictionary<string, EntityLocData> _entityCache = new();

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
            if (!args.ByType.TryGetValue(typeof(EntityPrototype), out var changeSet))
                return;

            FlushEntityCache();
        }

        private EntityLocData CalcEntityLoc(string prototypeId)
        {
            string? name = null;
            string? desc = null;
            string? suffix = null;
            Dictionary<string, string>? attribs = null;

            while (true)
            {
                var prototype = _prototype.Index<EntityPrototype>(prototypeId);
                var locId = prototype.CustomLocalizationID ?? $"ent-{prototypeId}";

                if (TryGetMessage(locId, out var ctx, out var msg))
                {
                    // Localization override exists.
                    var mAttribs = msg.Attributes;

                    if (name == null && msg.Value != null)
                    {
                        // Only set name if the value isn't empty.
                        // So that you can override *only* a desc/suffix.
                        name = ctx.Format(msg.Value);
                    }

                    if (mAttribs != null && mAttribs.Count != 0)
                    {
                        if (desc == null && mAttribs.TryGetValue("desc", out var mDesc))
                        {
                            desc = ctx.Format(mDesc);
                        }

                        if (suffix == null && mAttribs.TryGetValue("suffix", out var mSuffix))
                        {
                            suffix = ctx.Format(mSuffix);
                        }

                        foreach (var (attrib, value) in msg.Attributes)
                        {
                            if (attrib is "desc" or "suffix")
                                continue;

                            attribs ??= new Dictionary<string, string>();
                            if (!attribs.ContainsKey(attrib))
                            {
                                attribs[attrib] = ctx.Format(value);
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
                        attribs ??= new Dictionary<string, string>();
                        if (!attribs.ContainsKey(attrib))
                        {
                            attribs[attrib] = value;
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
                attribs?.ToImmutableDictionary() ?? ImmutableDictionary<string, string>.Empty);
        }

        public EntityLocData GetEntityData(string prototypeId)
        {
            if (!_entityCache.TryGetValue(prototypeId, out var data))
            {
                data = CalcEntityLoc(prototypeId);
                _entityCache.Add(prototypeId, data);
            }

            return data;
        }
    }
}
