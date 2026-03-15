using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Robust.Shared.Collections;
using Robust.Shared.IoC;

namespace Robust.Shared.GameObjects.CommandBuffers;

/// <summary>
///     A command buffer, which is a queue of operations to apply to the ECS. They allow you to queue entity
///     mutating operations like spawning, deletion, and component add & remove.
/// </summary>
/// <seealso cref="IEntityManager"/>
public sealed class CommandBuffer : IDisposable
{
    /// <summary>
    ///     The maximum capacity a command buffer is allowed to have when returned.
    /// </summary>
    private const int MaximumReturnCapacity = 128;

    private readonly IEntityManager _entMan;

    private ValueList<CommandBufferEntry> _entries = [];
    internal int Capacity => _entries.Capacity;

    /// <summary>
    ///     Construct a new command buffer. As these are meant to be pooled by entity manager, the constructor is
    ///     internal.
    /// </summary>
    internal CommandBuffer(IDependencyCollection collection)
    {
        _entMan = collection.Resolve<IEntityManager>();
    }

    private ref CommandBufferEntry NextEntry()
    {
        _entries.Add(default);

        return ref _entries[^1];
    }

    /// <summary>
    ///     Adds an Action&lt;T&gt; invocation to the buffer.
    /// </summary>
    /// <remarks>
    ///     It is recommended to use static lambdas and static methods with this.
    /// </remarks>
    /// <param name="action">The action to invoke.</param>
    /// <param name="context">The context object to invoke it with.</param>
    /// <typeparam name="T">The type of the context object.</typeparam>
    public void InvokeAction<T>(Action<T> action, T context)
        where T : class
    {
        ref var entry = ref NextEntry();
        CommandBufferEntry.QueuedActionT(action, context, out entry);
    }

    /// <summary>
    ///     Adds an Action&lt;T, EntityUid&gt; invocation to the buffer.
    /// </summary>
    /// <remarks>
    ///     It is recommended to use static lambdas and static methods with this.
    /// </remarks>
    /// <param name="action">The action to invoke.</param>
    /// <param name="context">The context object to invoke it with.</param>
    /// <param name="target">The entity to use as a target.</param>
    /// <typeparam name="T">The type of the context object.</typeparam>
    public void InvokeAction<T>(Action<T, EntityUid> action, T context, EntityUid target)
        where T : class
    {
        ref var entry = ref NextEntry();
        CommandBufferEntry.QueuedActionTEnt(action, context, target, out entry);
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
