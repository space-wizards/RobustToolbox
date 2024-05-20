using System.Collections.Generic;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public abstract partial class EntityManager
{
    public void DirtyField<T>(EntityUid uid, T component, string fieldName, MetaDataComponent? metadata = null) where T : IComponent, IComponentDelta
    {
        var delta = (IComponentDelta)component;

#if DEBUG
        var field = typeof(T).GetField(fieldName);
        DebugTools.Assert(field!.HasCustomAttribute<AutoNetworkedFieldAttribute>());
#endif

        if (!delta.LastModifiedFields.ContainsKey(fieldName))
        {
            _sawmill.Error($"Tried to dirty delta field {fieldName} on {ToPrettyString(uid)} that isn't implemented.");
            return;
        }

        var curTick = _gameTiming.CurTick;
        delta.LastFieldUpdate = curTick;
        delta.LastModifiedFields[fieldName] = curTick;
        Dirty(uid, component, metadata);
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
    public Dictionary<string, GameTick> LastModifiedFields
    {
        get;
        set;
    }
}
