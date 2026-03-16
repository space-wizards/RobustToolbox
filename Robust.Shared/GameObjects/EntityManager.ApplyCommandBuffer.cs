using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects.CommandBuffers;
using Robust.Shared.GameObjects.EntityBuilders;

namespace Robust.Shared.GameObjects;

public abstract partial class EntityManager
{
    public void ApplyCommandBuffer(CommandBuffer buffer)
    {
        // Time to do the thing.
        var len = buffer.Entries.Count;
        var entries = buffer.Entries.Span;

        for (var i = 0; i < len; i++)
        {
            switch ((CommandBufferEntry.CmdKind)(entries[i].Command & 0xFF))
            {
                case CommandBufferEntry.CmdKind.QueuedActionT:
                {
                    entries[i].InvokeQueuedActionT();
                    break;
                }
                case CommandBufferEntry.CmdKind.QueuedActionTEnt:
                {
                    entries[i].InvokeQueuedActionTEnt();
                    break;
                }
                case CommandBufferEntry.CmdKind.SubBuffer:
                {
                    ApplyCommandBuffer((CommandBuffer)entries[i].Field2!);
                    break;
                }
                case CommandBufferEntry.CmdKind.DeleteEntity:
                {
                    DeleteEntity(entries[i].TargetEnt);
                    break;
                }
                case CommandBufferEntry.CmdKind.SpawnEntity:
                {
                    switch (entries[i].Field2)
                    {
                        case EntityBuilder b:
                        {
                            ApplyEntityBuilder(b);
                            break;
                        }
                        case EntityBuilder[] b:
                        {
                            BulkApplyEntityBuilders(b);
                            break;
                        }
                    }
                    break;
                }
                case CommandBufferEntry.CmdKind.AddComponents:
                {
                    switch (entries[i].Field2)
                    {
                        case IComponent b:
                        {
                            AddComponent(entries[i].TargetEnt, b);
                            break;
                        }
                        case IComponent[] b:
                        {
                            foreach (var comp in b)
                            {
                                AddComponent(entries[i].TargetEnt, comp);
                            }
                            break;
                        }
                    }
                    break;
                }
                case CommandBufferEntry.CmdKind.EnsureComponents:
                {
                    break;
                }
                case CommandBufferEntry.CmdKind.RemoveComponents:
                {
                    break;
                }
                case CommandBufferEntry.CmdKind.Invalid:
                default:
                {
                    throw new InvalidOperationException("Command buffer reached invalid command during execution.");
                }
            }
        }
    }
}
