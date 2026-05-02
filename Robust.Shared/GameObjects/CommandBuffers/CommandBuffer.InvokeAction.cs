using System;

namespace Robust.Shared.GameObjects.CommandBuffers;

public sealed partial class CommandBuffer
{
    /// <summary>
    ///     Adds an Action&lt;T&gt; invocation to the buffer.
    /// </summary>
    /// <remarks>
    ///     It is recommended to use static lambdas and static methods with this.
    /// </remarks>
    /// <param name="action">The action to invoke.</param>
    /// <param name="context">The context object to invoke it with.</param>
    /// <typeparam name="T">The type of the context object.</typeparam>
    /// <returns>The command buffer, for chaining.</returns>
    public CommandBuffer InvokeAction<T>(Action<T> action, T context)
        where T : class
    {
        CommandBufferEntry.QueuedActionT(action, context, out NextEntry());
        return this;
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
    public CommandBuffer InvokeAction<T>(Action<T, EntityUid> action, T context, EntityUid target)
        where T : class
    {
        CommandBufferEntry.QueuedActionTEnt(action, context, target, out NextEntry());
        return this;
    }
}
