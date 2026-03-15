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
/// <para>
///     This is, essentially, a union of all the possible command buffer commands, to the best of C#'s ability.
///     The majority of command buffer commands do not need any more storage than the entry itself, reducing
///     heap fragmentation and long-lived micro-allocations significantly.
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

    /// <summary>
    ///     The kind of the command.
    /// </summary>
    /// <remarks>This is always valid.</remarks>
    public CmdKind Kind => (CmdKind)(Command & 0xFF);
    /// <summary>
    ///     The target EntityUid.
    /// </summary>
    /// <remarks>
    ///     This is only valid if the contained command uses the field for an EntityUid.
    ///     In anticipation of future refactors, we assume EntityUid is long sized and takes up the entirety
    ///     of Field1.
    /// </remarks>
    public EntityUid TargetEnt => new (unchecked((int)Field1));

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
    ///     Creates a new command buffer entry to apply another command buffer.
    /// </summary>
    /// <param name="subBuffer">The buffer to apply.</param>
    /// <param name="entry">The location to place the new entry within.</param>
    public static void SubBuffer(CommandBuffer subBuffer, out CommandBufferEntry entry)
    {
        entry.Command = (long)CmdKind.SubBuffer;
        entry.Field1 = 0;
        entry.Field2 = subBuffer;
        entry.Field3 = null;
    }

    /// <summary>
    ///     The kind of command in an entry.
    /// </summary>
    public enum CmdKind : byte
    {
        /// <summary>
        ///     An invalid command. Causes <see cref="IEntityManager.ApplyCommandBuffer"/> to throw.
        /// </summary>
        Invalid = 0,

        /// <summary>
        ///     A command for running an Action&lt;T&gt;.
        /// <code>
        ///     unused Field1;
        ///     Action&lt;T&gt; Action;
        ///     T Context;
        /// </code>
        /// </summary>
        QueuedActionT,

        /// <summary>
        ///     A command for running an Action&lt;T, EntityUid&gt;.
        /// <code>
        ///     EntityUid Target;
        ///     Action&lt;T, EntityUid&gt; Action;
        ///     T Context;
        /// </code>
        /// </summary>
        QueuedActionTEnt,

        /// <summary>
        ///     Handles executing another command buffer that was branched from this one.
        /// <code>
        ///     unused Field1;
        ///     CommandBuffer SubBuffer;
        ///     unused Field3;
        /// </code>
        /// </summary>
        SubBuffer,

        /// <summary>
        ///     Handles deleting an entity.
        /// <code>
        ///     EntityUid Target;
        ///     unused Field2;
        ///     unused Field3;
        /// </code>
        /// </summary>
        DeleteEntity,

        /// <summary>
        ///     Handles spawning a map with a builder.
        /// <code>
        ///     MapId ReservedMapId;
        ///     MapEntityBuilder EntityBuilder;
        ///     unused Field3;
        /// </code>
        /// </summary>
        SpawnMap,

        /// <summary>
        ///     Handles spawning an entity with a builder.
        /// <code>
        ///     unused Field1;
        ///     EntityBuilder EntityBuilder;
        ///     unused Field3;
        /// </code>
        /// </summary>
        SpawnEntity,

        /// <summary>
        ///     Handles adding components to an entity.
        ///     The fields for this are a bit funny, it can either contain a List of components, or up
        ///     to two direct references to components.
        /// <code>
        ///     EntityUid Target;
        ///     IComponent | List&lt;IComponent&gt; Components;
        ///     IComponent? ExtraComponent;
        /// </code>
        /// </summary>
        AddComponents,

        /// <summary>
        ///     Handles removing components from an entity.
        ///     Similar to AddComponents, this either contains a List of Type, or up to two direct references to
        ///     Type.
        /// <code>
        ///     EntityUid Target;
        ///     Type | List&lt;Type&gt; Components;
        ///     Type? ExtraComponent;
        /// </code>
        /// </summary>
        RemoveComponents,


    }
}
