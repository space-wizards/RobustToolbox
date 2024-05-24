using Robust.Shared.Timing;

namespace Robust.Shared.GameObjects;

public abstract partial class EntityManager
{
    public void DirtyField(EntityUid uid, IComponentDelta delta, string fieldName, MetaDataComponent? metadata = null)
    {
        // Much more likely for this to accidentally not be implemented than vice versa.
        if (delta is not IComponent component)
        {
            _sawmill.Error($"Tried to dirty component field for {delta.GetType()} which does not implement {nameof(IComponent)}");
            return;
        }

        var compReg = ComponentFactory.GetRegistration(component);
        InternalDirty(uid, component, delta, compReg, fieldName, metadata);
    }

    public void DirtyField<T>(EntityUid uid, T component, string fieldName, MetaDataComponent? metadata = null) where T : IComponent, IComponentDelta
    {
        var delta = (IComponentDelta)component;
        var compReg = ComponentFactory.GetRegistration(CompIdx.Index<T>());
        InternalDirty(uid, component, delta, compReg, fieldName, metadata);
    }

    private void InternalDirty(EntityUid uid, IComponent comp, IComponentDelta delta, ComponentRegistration compReg, string fieldName, MetaDataComponent? metadata = null)
    {
        if (!compReg.NetworkedFieldLookup.TryGetValue(fieldName, out var idx))
        {
            _sawmill.Error($"Tried to dirty delta field {fieldName} on {ToPrettyString(uid)} that isn't implemented.");
            return;
        }

        var curTick = _gameTiming.CurTick;
        delta.LastFieldUpdate = curTick;
        delta.LastModifiedFields[idx] = curTick;
        Dirty(uid, comp, metadata);
    }
}

/// <summary>
/// Indicates this component supports delta states.
/// </summary>
public interface IComponentDelta
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
