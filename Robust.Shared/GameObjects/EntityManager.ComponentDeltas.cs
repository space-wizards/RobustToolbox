using System;
using System.Diagnostics.Contracts;
using Robust.Shared.Timing;

namespace Robust.Shared.GameObjects;

public abstract partial class EntityManager
{
    [Pure]
    public uint GetModifiedFields(IComponentDelta delta, GameTick fromTick)
    {
        uint fields = 0;

        for (var i = 0; i < delta.LastModifiedFields.Length; i++)
        {
            var lastUpdate = delta.LastModifiedFields[i];

            // Field not dirty
            if (lastUpdate < fromTick)
                continue;

            fields |= (uint) (1 << i);
        }

        return fields;
    }

    [Pure]
    public uint GetModifiedFields<T>(T delta, GameTick fromTick) where T : IComponentDelta
    {
        uint fields = 0;

        for (var i = 0; i < delta.LastModifiedFields.Length; i++)
        {
            var lastUpdate = delta.LastModifiedFields[i];

            // Field not dirty
            if (lastUpdate < fromTick)
                continue;

            fields |= (uint) (1 << i);
        }

        return fields;
    }

    private bool TryDirtyField<T>(EntityUid uid, T comp, string fieldName, MetaDataComponent? metadata)
        where T : IComponentDelta
    {
        var compReg = ComponentFactory.GetRegistration(CompIdx.Index<T>());

        // TODO
        // consider storing this on MetaDataComponent?
        // We already store other dirtying information there anyways, and avoids having to fetch the registration.
        if (!compReg.NetworkedFieldLookup.TryGetValue(fieldName, out var idx))
        {
            _sawmill.Error($"Tried to dirty delta field {fieldName} on {ToPrettyString(uid)} that isn't implemented.");
            return false;
        }

        var curTick = _gameTiming.CurTick;
        comp.LastFieldUpdate = curTick;
        comp.LastModifiedFields[idx] = curTick;
        Dirty(uid, comp, metadata);
        return true;
    }

    /// <inheritdoc />
    public bool TryDirtyField<T, U>(EntityUid uid, T component, string fieldName, ref U? field, U? value, MetaDataComponent? metadata = null)
        where T : IComponentDelta
        where U : IEquatable<U>?
    {
        if (field?.Equals(value) == true)
            return false;

        if (!TryDirtyField(uid, component, fieldName, metadata))
            return false;

        field = value;
        return true;
    }

    public bool TryDirtyField<T, U>(Entity<T?> ent, string fieldName, ref U? field, U? value, MetaDataComponent? metadata = null)
        where T : IComponentDelta
        where U : IEquatable<U>?
    {
        var comp = ent.Comp;

        if (comp == null || !TryGetComponent(ent.Owner, out comp))
            return false;

        return TryDirtyField(ent.Owner, comp, fieldName, ref field, value, metadata);
    }

    /// <inheritdoc />
    public void DirtyField(EntityUid uid, IComponentDelta comp, string fieldName, MetaDataComponent? metadata = null)
    {
        TryDirtyField(uid, comp, fieldName, metadata);
    }

    /// <inheritdoc />
    public virtual void DirtyField<T>(EntityUid uid, T comp, string fieldName, MetaDataComponent? metadata = null)
        where T : IComponentDelta
    {
        TryDirtyField(uid, comp, fieldName, metadata);
    }

    /// <inheritdoc />
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

        comp.LastFieldUpdate = curTick;
        Dirty(uid, comp, meta);
    }
}

/// <summary>
/// Indicates this component supports delta states.
/// </summary>
public partial interface IComponentDelta : IComponent
{
    // TODO: This isn't entirely robust but not sure how else to handle this?
    /// <summary>
    /// Track last time a field was dirtied. if the full component dirty exceeds this then we send a full state update.
    /// </summary>
    public GameTick LastFieldUpdate { get; set; }

    /// <summary>
    /// Stores the last modified tick for fields.
    /// </summary>
    public GameTick[] LastModifiedFields
    {
        get;
        set;
    }
}
