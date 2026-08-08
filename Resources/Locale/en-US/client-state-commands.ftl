# Loc strings for various entity state & client-side PVS related commands

cmd-reset-ent-help = Usage: {$command} <Entity UID>
cmd-reset-ent-desc = Reset an entity to the most recently received server state. This will also reset entities that have been detached to null-space.

cmd-reset-all-ents-help = Usage: {$command}
cmd-reset-all-ents-desc = Resets all entities to the most recently received server state. This only impacts entities that have not been detached to null-space.

cmd-detach-ent-help = Usage: {$command} <Entity UID>
cmd-detach-ent-desc = Detach an entity to null-space, as if it had left PVS range.

cmd-local-delete-help = Usage: {$command} <Entity UID>
cmd-local-delete-desc = Deletes an entity. Unlike the normal delete command, this is CLIENT-SIDE. Unless the entity is a client-side entity, this will likely cause errors.

cmd-full-state-reset-help = Usage: {$command}
cmd-full-state-reset-desc = Discards any entity state information and requests a full-state from the server.
