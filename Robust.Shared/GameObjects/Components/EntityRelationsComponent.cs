using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.GameObjects;

[RegisterComponent, NetworkedComponent, Access(typeof(EntityManager), typeof(EntityRelationsSystem))]
[AutoGenerateComponentState]
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
}
