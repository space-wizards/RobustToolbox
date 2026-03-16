using System;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.GameObjects.CommandBuffers;
using Robust.Shared.GameObjects.EntityBuilders;
using Robust.Shared.IoC;

namespace Robust.Shared.GameObjects;

public abstract partial class EntityManager
{
    private DefaultObjectPool<CommandBuffer> CommandBufferPool { get; set; } = default!;

    DefaultObjectPool<CommandBuffer> IEntityManager.CommandBufferPool => CommandBufferPool;

    public void ApplyCommandBuffer(CommandBuffer buffer)
    {
        throw new System.NotImplementedException();
    }

    public EntityUid ApplyEntityBuilder(EntityBuilder builder)
    {
        throw new NotImplementedException();
    }

    public void BulkApplyEntityBuilders(ReadOnlySpan<EntityBuilder> builders)
    {
        throw new NotImplementedException();
    }

    public CommandBuffer GetCommandBuffer()
    {
        return CommandBufferPool.Get();
    }

    public EntityBuilder BlankEntityBuilder()
    {
        return EntityBuilder.BlankEntity(_dependencyCollection, GenerateEntityUid(), null);
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
