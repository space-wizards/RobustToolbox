using System;
using Robust.Shared.Timing;

namespace Robust.Shared.GameObjects;

public abstract partial class EntityManager
{
    public static ulong GetModifiedAspects(IComponentDelta delta, GameTick fromTick)
    {
        if (delta.LastUnclassifiedDirty > fromTick)
        {
            // By returning max value here, we short-circuit the more expensive evaluation below while returning a value
            // with the unclassified change bit set. This will over-represent changes, technically, but in this specific
            // case components should be doing a full update anyway.
            return ulong.MaxValue;
        }

        ulong fields = 0;
        for (var i = 0; i < delta.LastModifiedFields.Length; i++)
        {
            var lastUpdate = delta.LastModifiedFields[i];

            // Field not dirty
            if (lastUpdate <= fromTick)
                continue;

            fields |= 1UL << i;
        }

        return fields;
    }

    public void DirtyField(EntityUid uid, IComponentDelta comp, string fieldName, MetaDataComponent? metadata = null)
    {
        var compReg = ComponentFactory.GetRegistration(comp);

        if (!compReg.NetworkedFieldLookup.TryGetValue(fieldName, out var idx))
        {
            _sawmill.Error($"Tried to dirty delta field {fieldName} on {ToPrettyString(uid)} that isn't implemented.");
            return;
        }

        var curTick = _gameTiming.CurTick;
        comp.LastModifiedFields[idx] = curTick;
        DirtyInternal(uid, comp, metadata, false);
    }

    public virtual void DirtyField<T>(EntityUid uid, T comp, [ValidateMember] string fieldName, MetaDataComponent? metadata = null)
        where T : IComponentDelta
    {
        var compReg = ComponentFactory.GetRegistration(CompIdx.Index<T>());

        // TODO
        // consider storing this on MetaDataComponent?
        // We alsready store other dirtying information there anyways, and avoids having to fetch the registration.
        if (!compReg.NetworkedFieldLookup.TryGetValue(fieldName, out var idx))
        {
            _sawmill.Error($"Tried to dirty delta field {fieldName} on {ToPrettyString(uid)} that isn't implemented.");
            return;
        }

        var curTick = _gameTiming.CurTick;
        comp.LastModifiedFields[idx] = curTick;
        DirtyInternal(uid, comp, metadata, false);
    }

    public virtual void DirtyFields<T>(EntityUid uid, T comp, MetaDataComponent? meta, params string[] fields)
        where T : IComponentDelta
    {
        var compReg = ComponentFactory.GetRegistration(CompIdx.Index<T>());

        var curTick = _gameTiming.CurTick;
        foreach (var field in fields)
        {
            if (!compReg.NetworkedFieldLookup.TryGetValue(field, out var idx))
                _sawmill.Error($"Tried to dirty delta field {field} on {ToPrettyString(uid)} that isn't implemented.");
            else
                comp.LastModifiedFields[idx] = curTick;
        }

        DirtyInternal(uid, comp, meta, false);
    }
}

/// <summary>
/// Indicates this component supports delta states.
/// </summary>
public partial interface IComponentDelta : IComponent
{
    /// <summary>
    /// The last unclassified modification to this component.
    /// </summary>
    public GameTick LastUnclassifiedDirty { get; set; }

    /// <summary>
    /// Stores the last modified tick for fields.
    /// </summary>
    public GameTick[] LastModifiedFields { get; set; }
}

/// <summary>
/// Component delta system aspects. These are flags returned via <see cref="EntityManager.GetModifiedAspects"/>. Fields
/// occupy the lower bits and grow upwards. System aspects occupy the upper bits and grow downwards.
/// </summary>
public static class DeltaAspect
{
    public const ulong Unclassified = 1UL << 63;
}
