using System;
using System.Reflection;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects.CommandBuffers;

internal partial struct CommandBufferEntry
{
    /// <summary>
    ///     Creates a new queued <see cref="Action{T}"/> invocation to add to a command buffer.
    /// </summary>
    /// <param name="action">The action to invoke.</param>
    /// <param name="context">Context to provide to the action.</param>
    /// <param name="entry">The location to place the new entry within.</param>
    /// <typeparam name="T">The type of the context object to pass to the action.</typeparam>
    public static void QueuedActionT<T>(Action<T> action, T context, out CommandBufferEntry entry)
        where T : class
    {
        entry.Command = (long)CmdKind.QueuedActionT;

        entry.Field1 = 0;
        entry.Field2 = action;
        entry.Field3 = context;
    }

    /// <summary>
    ///     Creates a new queued <see cref="Action{T}"/> invocation to add to a command buffer.
    /// </summary>
    /// <param name="action">The action to invoke.</param>
    /// <param name="context">Context to provide to the action.</param>
    /// <param name="ent">The entity to pass along.</param>
    /// <param name="entry">The location to place the new entry within.</param>
    /// <typeparam name="T"></typeparam>
    public static void QueuedActionTEnt<T>(
        Action<T, EntityUid> action,
        T context,
        EntityUid ent,
        out CommandBufferEntry entry)
        where T : class
    {
        entry.Command = (long)CmdKind.QueuedActionTEnt;

        entry.Field1 = (int)ent;
        entry.Field2 = action;
        entry.Field3 = context;
    }

    /// <summary>
    ///     Handles invoking a queued action with context argument..
    /// </summary>
    public void InvokeQueuedActionT()
    {
        DebugTools.AssertEqual(Kind, CmdKind.QueuedActionT);

        // Yes, we potentially allocate at invocation time.
        // A shame, but we can't invoke Action<T> otherwise.
        // The actual allocation is a single entry object array, which should never leave Gen0 and thus should be quite
        // cheap.
        //
        // So it's still better we allocate here at the call site rather than in a potentially long-lived command buffer.
        var d = (Delegate)Field2!;
        // We don't check if the Delegate has anything like multi-invocation because the only constructor we have
        // makes sure it's an Action<T>.
        d.Method.Invoke(d.Target, BindingFlags.DoNotWrapExceptions, null, [Field3!], null);
    }

    /// <summary>
    ///     Handles invoking a queued action with context argument and entity.
    /// </summary>
    public void InvokeQueuedActionTEnt()
    {
        DebugTools.AssertEqual(Kind, CmdKind.QueuedActionTEnt);

        // Yes, we potentially allocate at invocation time.
        // A shame, but we can't invoke Action<T> otherwise.
        // The actual allocation is a single entry object array, which should never leave Gen0 and thus should be quite
        // cheap.
        //
        // So it's still better we allocate here at the call site rather than in a potentially long-lived command buffer.
        var d = (Delegate)Field2!;
        // We don't check if the Delegate has anything like multi-invocation because the only constructor we have
        // makes sure it's an Action<T>.
        d.Method.Invoke(d.Target, BindingFlags.DoNotWrapExceptions, null, [Field3!, new EntityUid((int)Field1)], null);
    }
}
