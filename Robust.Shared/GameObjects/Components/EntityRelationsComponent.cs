using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Tracks entities that are currently related to the Owner of this component.
/// Used for tracking <see cref="EntityRelation"/>s in other components to
/// properly clean them when the entity is terminated.
/// </summary>
[Access(typeof(EntityManager), typeof(EntityRelationsSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class EntityRelationsComponent : Component
{
    /// <summary>
    /// A list of currently active entity relations.
    /// </summary>
    /// <remarks>
    /// Not HashSet because one entity can relate by
    /// 2+ different components on the same owner, and to handle that safely
    /// the relation is listed multiple times.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public List<EntityRelation> Relations = new();

    /// <summary>
    /// A list of currently active local entity relations.
    /// Stored in a separate list so it's not interfering with networked operations.
    /// </summary>
    /// <remarks>
    /// Local relations are relations that only on one side, server or client.
    /// </remarks>
    [DataField]
    public List<EntityRelation> LocalRelations = new();

    /// <summary>
    /// If true, will remove the component when <see cref="Relations"/> list becomes empty.
    /// Setting this to false may be useful for custom performance sensitive EntityRelation interactions.
    /// This must be set to false explicitly for <see cref="LocalRelations"/> to be supported.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RemoveOnEmpty = true;
}
