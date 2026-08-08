using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.GameStates;

/// <summary>
/// Marks an entity as server-managed chunk data for a map or grid PVS chunk.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class ChunkEntityComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Root;

    [DataField, AutoNetworkedField]
    public Vector2i Chunk;
}
