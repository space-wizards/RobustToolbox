using Microsoft.Extensions.ObjectPool;
using Robust.Shared.GameObjects.CommandBuffers;
using Robust.Shared.IoC;

namespace Robust.Shared.GameObjects;

public abstract partial class EntityManager
{
    public DefaultObjectPool<CommandBuffer> CommandBufferPool { get; private set; } = default!;
    public void ApplyCommandBuffer(CommandBuffer buffer)
    {
        throw new System.NotImplementedException();
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

    public CommandBuffer GetCommandBuffer()
    {
        throw new System.NotImplementedException();
    }

    EntityUid IRemoteEntityManager.GetUnusedEntityUid()
    {
        return GenerateEntityUid();
    }
}
