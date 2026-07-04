using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameStates;

/// <summary>
/// Runtime index of chunk entities owned by a map or grid root.
/// </summary>
[RegisterComponent]
[Access(typeof(ChunkEntitySystem), typeof(ChunkEntitySystem.ChunkEntityRootEnumerator))]
public sealed partial class ChunkContainerComponent : Component
{
    [ViewVariables]
    internal int ChunkCount => Chunks.Count;

    [ViewVariables]
    internal int SavedChunkCount => ChunkEntities.Count;

    [DataField]
    internal HashSet<EntityUid> ChunkEntities = new();

    [ViewVariables(VVAccess.ReadOnly)]
    internal readonly Dictionary<Vector2i, Entity<ChunkEntityComponent>> Chunks = new();
}
