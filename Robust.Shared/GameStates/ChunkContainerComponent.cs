using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameStates;

/// <summary>
/// Runtime index of chunk entities owned by a map or grid root.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ChunkEntitySystem), typeof(ChunkEntitySystem.ChunkEntityRootEnumerator))]
public sealed partial class ChunkContainerComponent : Component
{
    [ViewVariables]
    public int ChunkCount => Chunks.Count;

    [ViewVariables]
    public int SavedChunkCount => ChunkEntities.Count;

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> ChunkEntities = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public readonly Dictionary<Vector2i, Entity<ChunkEntityComponent>> Chunks = new();
}
