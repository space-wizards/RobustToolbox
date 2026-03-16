using System;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.GameObjects.CommandBuffers;
using Robust.Shared.GameObjects.EntityBuilders;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Robust.Shared.GameObjects;

public abstract partial class EntityManager
{
    private DefaultObjectPool<CommandBuffer> CommandBufferPool { get; set; } = default!;

    DefaultObjectPool<CommandBuffer> IEntityManager.CommandBufferPool => CommandBufferPool;

    public CommandBuffer GetCommandBuffer()
    {
        return CommandBufferPool.Get();
    }

    public EntityBuilder EntityBuilder(EntProtoId? protoId = null, ISerializationContext? context = null)
    {
        if (protoId is null)
            return EntityBuilders.EntityBuilder.BlankEntity(_dependencyCollection, GenerateEntityUid(), context);

        return EntityBuilders.EntityBuilder.PrototypedEntity(_dependencyCollection, GenerateEntityUid(), protoId.Value, context);
    }

    EntityUid IRemoteEntityManager.GetUnusedEntityUid()
    {
        return GenerateEntityUid();
    }

    private sealed class CommandBufferPolicy(IDependencyCollection collection) : IPooledObjectPolicy<CommandBuffer>
    {
        public CommandBuffer Create()
        {
            return new CommandBuffer(collection);
        }

        public bool Return(CommandBuffer obj)
        {
            if (obj.Capacity > 128)
                return false; // Get rid of it, too chonky.

            obj.Clear();
            return true;
        }
    }
}
