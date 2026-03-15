using System;
using System.Diagnostics;
using Robust.Shared.Collections;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Robust.Shared.GameObjects.CommandBuffers;

/// <summary>
/// <para>
///     A command buffer, which is a queue of operations to apply to the ECS. They allow you to queue entity
///     mutating operations like spawning, deletion, and component add & remove.
/// </para>
/// <para>
///     Actions added to the command buffer are executed in order of addition when <see cref="IEntityManager.ApplyCommandBuffer"/>
///     is called, which then also clears and returns the CommandBuffer.
/// </para>
/// </summary>
/// <seealso cref="IEntityManager"/>
public sealed partial class CommandBuffer : IDisposable
{
    private readonly IEntityManager _entMan;
    private readonly IPrototypeManager _protoMan;

    /// <summary>
    ///     The underlying list of entries in the buffer.
    /// </summary>
    private ValueList<CommandBufferEntry> _entries = [];
    /// <summary>
    ///     The capacity of the underlying entry collection, for object pooling usage.
    /// </summary>
    internal int Capacity => _entries.Capacity;

    /// <summary>
    ///     Construct a new command buffer. As these are meant to be pooled by entity manager, the constructor is
    ///     internal.
    /// </summary>
    internal CommandBuffer(IDependencyCollection collection)
    {
        _entMan = collection.Resolve<IEntityManager>();
        _protoMan = collection.Resolve<IPrototypeManager>();
    }

    private ref CommandBufferEntry NextEntry()
    {
        _entries.Add(default);

        return ref _entries[^1];
    }

    /// <summary>
    ///     Creates a new CommandBuffer that will execute at precisely this point.
    ///     This buffer is independent of its parent and can be safely given to another thread.
    /// </summary>
    /// <returns>The created sub-buffer.</returns>
    public CommandBuffer CreateSubBuffer()
    {
        var subBuffer = _entMan.GetCommandBuffer();

        CommandBufferEntry.SubBuffer(subBuffer, out NextEntry());

        return subBuffer;
    }

    /// <summary>
    ///     Adds an entity deletion to the buffer.
    /// </summary>
    /// <param name="target">The entity to delete immediately.</param>
    public void DeleteEntity(EntityUid target)
    {
        CommandBufferEntry.DeleteEntity(target, out NextEntry());
    }

    /// <summary>
    ///     Returns the CommandBuffer without applying it.
    /// </summary>
    public void Dispose()
    {
        _entMan.CommandBufferPool.Return(this);
    }

    /// <summary>
    ///     Internal method used for cleaning up a CommandBuffer on return.
    /// </summary>
    internal void Clear()
    {
        _entries.Clear();
    }
}
