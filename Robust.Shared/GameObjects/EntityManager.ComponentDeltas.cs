using Robust.Shared.Timing;

namespace Robust.Shared.GameObjects;

public abstract partial class EntityManager
{
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

    public void DirtyField(EntityUid uid, IComponentDelta comp, string fieldName, MetaDataComponent? metadata = null)
    {
        var compReg = ComponentFactory.GetRegistration(comp);

        if (!compReg.NetworkedFieldLookup.TryGetValue(fieldName, out var idx))
        {
            _sawmill.Error($"Tried to dirty delta field {fieldName} on {ToPrettyString(uid)} that isn't implemented.");
            return;
        }

        var curTick = _gameTiming.CurTick;
        comp.LastFieldUpdate = curTick;
        comp.LastModifiedFields[idx] = curTick;
        Dirty(uid, comp, metadata);
    }

    public void DirtyField<T>(EntityUid uid, T comp, string fieldName, MetaDataComponent? metadata = null)
        where T : IComponentDelta
    {
        var compReg = ComponentFactory.GetRegistration(CompIdx.Index<T>());

        if (!compReg.NetworkedFieldLookup.TryGetValue(fieldName, out var idx))
        {
            _sawmill.Error($"Tried to dirty delta field {fieldName} on {ToPrettyString(uid)} that isn't implemented.");
            return;
        }

        var curTick = _gameTiming.CurTick;
        comp.LastFieldUpdate = curTick;
        comp.LastModifiedFields[idx] = curTick;
        Dirty(uid, comp, metadata);
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
