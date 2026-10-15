using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Robust.Client.GameStates;

public sealed partial class ClientChunkEntitySystem : ChunkEntitySystem
{
    public event Action<EntityUid>? ChunkEntityInitialized;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChunkEntityComponent, ComponentInit>(OnChunkEntityInit);
    }

    private void OnChunkEntityInit(EntityUid uid, ChunkEntityComponent component, ComponentInit args)
    {
        ChunkEntityInitialized?.Invoke(uid);
    }
}
