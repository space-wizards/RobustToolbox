using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.GameObjects;

[RegisterComponent, NetworkedComponent, Access(typeof(EntityManager), typeof(EntityRelationsSystem))]
[AutoGenerateComponentState, AutoGenerateEntityRelations]
public sealed partial class EntityRelationsComponent : Component
{
    [DataField, AutoNetworkedField, AutoRelationField]
    public List<EntityRelation> Relations = new();
}
