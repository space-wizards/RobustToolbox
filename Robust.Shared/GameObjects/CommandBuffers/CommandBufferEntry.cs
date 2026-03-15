using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects.CommandBuffers;

/// <summary>
/// <para>
///     A 32-byte structure used to contain an untyped command buffer command,
///     to help reduce the number of allocations necessary to buffer commands.
/// </para>
/// <para>
///     The meanings of <see cref="Field1"/>, <see cref="Field2"/>, and <see cref="Field3"/> are dependent on the
///     contents of <see cref="Command"/>. Static constructors are provided for commands themselves.
/// </para>
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal partial struct CommandBufferEntry
{
    /// <summary>
    ///     The command data. This is bitpacked and contains some extra information.
    /// </summary>
    public long Command;
    public long Field1;
    public object? Field2;
    public object? Field3;

    public CmdKind Kind => (CmdKind)(Command & 0xFF);

    static unsafe CommandBufferEntry()
    {
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        // Ensure we're the expected size, because perf sensitive.
        if (sizeof(CommandBufferEntry) != 32)
        {
            throw new Exception(
                $"{nameof(CommandBufferEntry)} was modified to no longer be half-cacheline sized. You're going to need to go adjust things!");
        }
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    }

    /// <summary>
    ///     Creates a new command buffer entry to delete a given entity.
    /// </summary>
    /// <param name="target">Target entity.</param>
    /// <param name="entry">The location to place the new entry within.</param>
    public static void DeleteEntity(EntityUid target, out CommandBufferEntry entry)
    {
        entry.Command = (long)CmdKind.DeleteEntity;
        entry.Field1 = (long)target;
        entry.Field2 = null;
        entry.Field3 = null;
    }

    /// <summary>
    ///     The kind of command in an entry.
    /// </summary>
    public enum CmdKind : byte
    {
        /// <summary>
        ///     An invalid command.
        /// </summary>
        Invalid = 0,

        /// <summary>
        ///     A command for running an Action&lt;T&gt;.
        /// <code>
        ///     unused Field1;
        ///     Action&lt;object&gt; Field2;
        ///     object Field3;
        /// </code>
        /// </summary>
        QueuedActionT,

        /// <summary>
        ///     A command for running an Action&lt;T, EntityUid&gt;.
        /// <code>
        ///     EntityUid Field1;
        ///     Action&lt;T, EntityUid&gt; Field2;
        ///     T Field3;
        /// </code>
        /// </summary>
        QueuedActionTEnt,

        /// <summary>
        ///     Handles deleting an entity.
        /// <code>
        ///     EntityUid Field1;
        ///     unused Field2;
        ///     unused Field3;
        /// </code>
        /// </summary>
        DeleteEntity,
    }
}
