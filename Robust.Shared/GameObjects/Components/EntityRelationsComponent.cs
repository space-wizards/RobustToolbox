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
    /// This type is a list since the entity may be related
    /// by 2 different components on the same owner, and to handle that safely
    /// the relation is listed multiple times.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public List<EntityRelation> Relations = new();

    /// <summary>
    /// If true, will remove the component when <see cref="Relations"/> list becomes empty.
    /// Useful for conditional networking or performance sensitive EntityRelation interactions.
    /// </summary>
    /// <remarks>
    /// This field is always networked to the client.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public bool RemoveOnEmpty = true;
}
