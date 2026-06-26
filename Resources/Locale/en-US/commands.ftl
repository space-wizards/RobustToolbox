### Localization for engine console commands

cmd-hint-float = [float]

## generic command errors

cmd-invalid-arg-number-error = Invalid number of arguments.

cmd-parse-failure-integer = {$arg} is not a valid integer.
cmd-parse-failure-float = {$arg} is not a valid float.
cmd-parse-failure-bool = {$arg} is not a valid bool.
cmd-parse-failure-uid = {$arg} is not a valid entity UID.
cmd-parse-failure-mapid = {$arg} is not a valid MapId.
cmd-parse-failure-enum = {$arg} is not a {$enum} Enum.
cmd-parse-failure-grid = {$arg} is not a valid grid.
cmd-parse-failure-cultureinfo = "{$arg}" is not valid CultureInfo.
cmd-parse-failure-entity-exist = UID {$arg} does not correspond to an existing entity.
cmd-parse-failure-session = There is no session with username: {$username}

cmd-error-file-not-found = Could not find file: {$file}.
cmd-error-dir-not-found = Could not find directory: {$dir}.

cmd-failure-no-attached-entity = There is no entity attached to this shell.

## 'help' command
cmd-help-desc = Display general help or help text for a specific command.
cmd-help-help = Usage: {$command} [command name]
    When no command name is provided, displays general-purpose help text. If a command name is provided, displays help text for that command.

cmd-help-no-args = To display help for a specific command, write 'help <command>'. To list all available commands, write 'list'. To search for commands, use 'list <filter>'.
cmd-help-unknown = Unknown command: { $command }
cmd-help-top = { $command } - { $description }
cmd-help-invalid-args = Invalid amount of arguments.
cmd-help-arg-cmdname = [command name]

## 'cvar' command
cmd-cvar-desc = Gets or sets a CVar.
cmd-cvar-help = Usage: {$command} <name | ?> [value]
    If a value is passed, the value is parsed and stored as the new value of the CVar.
    If not, the current value of the CVar is displayed.
    Use 'cvar ?' to get a list of all registered CVars.

cmd-cvar-invalid-args = Must provide exactly one or two arguments.
cmd-cvar-not-registered = CVar '{ $cvar }' is not registered. Use 'cvar ?' to get a list of all registered CVars.
cmd-cvar-parse-error = Input value is in incorrect format for type { $type }
cmd-cvar-compl-list = List available CVars
cmd-cvar-arg-name = <name | ?>
cmd-cvar-value-hidden = <value hidden>

## 'cvar_subs' command
cmd-cvar_subs-desc = Lists the OnValueChanged subscriptions for a CVar.
cmd-cvar_subs-help = Usage: {$command} <name>

cmd-cvar_subs-invalid-args = Must provide exactly one argument.
cmd-cvar_subs-arg-name = <name>

## 'list' command
cmd-list-desc = Lists available commands, with optional search filter.
cmd-list-help = Usage: {$command} [filter]
    Lists all available commands. If an argument is provided, it will be used to filter commands by name.

cmd-list-heading = SIDE NAME            DESC{"\u000A"}-------------------------{"\u000A"}

cmd-list-arg-filter = [filter]

## '>' command, aka remote exec
cmd-remoteexec-desc = Executes server-side commands.
cmd-remoteexec-help = Usage: > <command> [arg] [arg] [arg...]
    Executes a command on the server. This is necessary if a command with the same name exists on the client, as simply running the command would run the client command first.

## 'gc' command
cmd-gc-desc = Run the GC (Garbage Collector).
cmd-gc-help = Usage: {$command} [generation]
    Uses GC.Collect() to execute the Garbage Collector.
    If an argument is provided, it is parsed as a GC generation number and GC.Collect(int) is used.
    Use the 'gfc' command to do an LOH-compacting full GC.
cmd-gc-failed-parse = Failed to parse argument.
cmd-gc-arg-generation = [generation]

## 'gcf' command
cmd-gcf-desc = Run the GC, fully, compacting LOH and everything.
cmd-gcf-help = Usage: {$command}
    Does a full GC.Collect(2, GCCollectionMode.Forced, true, true) while also compacting LOH.
    This will probably lock up for hundreds of milliseconds, be warned.

## 'gc_mode' command
cmd-gc_mode-desc = Change/Read the GC Latency mode.
cmd-gc_mode-help = Usage: {$command} [type]
    If no argument is provided, returns the current GC latency mode.
    If an argument is passed, it is parsed as GCLatencyMode and set as the GC latency mode.

cmd-gc_mode-current = current gc latency mode: { $prevMode }
cmd-gc_mode-possible = possible modes:
cmd-gc_mode-option = - { $mode }
cmd-gc_mode-unknown = unknown gc latency mode: { $arg }
cmd-gc_mode-attempt = attempting gc latency mode change: { $prevMode } -> { $mode }
cmd-gc_mode-result = resulting gc latency mode: { $mode }
cmd-gc_mode-arg-type = [type]

## 'mem' command
cmd-mem-desc = Prints managed memory info.
cmd-mem-help = Usage: {$command}

cmd-mem-report = Heap Size: { TOSTRING($heapSize, "N0") }
    Total Allocated: { TOSTRING($totalAllocated, "N0") }

## 'physics' command
cmd-physics-overlay = {$overlay} is not a recognised overlay

## 'lsasm' command
cmd-lsasm-desc = Lists loaded assemblies by load context.
cmd-lsasm-help = Usage: lsasm

## 'exec' command
cmd-exec-desc = Executes a script file from the game's writeable user data.
cmd-exec-help = Usage: {$command} <fileName>
    Each line in the file is executed as a single command, unless it starts with a #

cmd-exec-arg-filename = <fileName>

## 'dump_net_comps' command
cmd-dump_net_comps-desc = Prints the table of networked components.
cmd-dump_net_comps-help = Usage: {$command}

cmd-dump_net_comps-error-writeable = Registration still writeable, network ids have not been generated.
cmd-dump_net_comps-header = Networked Component Registrations:

## 'dump_event_tables' command
cmd-dump_event_tables-desc = Prints directed event tables for an entity.
cmd-dump_event_tables-help = Usage: {$command} <entityUid>

cmd-dump_event_tables-missing-arg-entity = Missing entity argument
cmd-dump_event_tables-error-entity = Invalid entity
cmd-dump_event_tables-arg-entity = <entityUid>

## 'monitor' command
cmd-monitor-desc = Toggles a debug monitor in the F3 menu.
cmd-monitor-help = Usage: {$command} <name>
    Possible monitors are: { $monitors }
    You can also use the special values "-all" and "+all" to hide or show all monitors, respectively.

cmd-monitor-arg-monitor = <monitor>
cmd-monitor-invalid-name = Invalid monitor name
cmd-monitor-arg-count = Missing monitor argument
cmd-monitor-minus-all-hint = Hides all monitors
cmd-monitor-plus-all-hint = Shows all monitors


## 'setambientlight' command
cmd-set-ambient-light-desc = Allows you to set the ambient light for the specified map, in SRGB.
cmd-set-ambient-light-help = Usage: {$command} [mapid] [r g b a]
cmd-set-ambient-light-parse = Unable to parse args as a byte values for a color.

## Mapping commands

cmd-savemap-desc = Serializes a map to disk. Will not save a post-init map unless forced.
cmd-savemap-help = Usage: {$command} <MapID> <Path> [force]
cmd-savemap-not-exist = Target map does not exist.
cmd-savemap-init-warning = Attempted to save a post-init map without forcing the save.
cmd-savemap-attempt = Attempting to save map {$mapId} to {$path}.
cmd-savemap-success = Map successfully saved.
cmd-savemap-error = Could not save map! See server log for details.
cmd-hint-savemap-id = <MapID>
cmd-hint-savemap-path = <Path>
cmd-hint-savemap-force = [bool]

cmd-loadmap-desc = Loads a map from disk into the game.
cmd-loadmap-help = Usage: {$command} <MapID> <Path> [x] [y] [rotation] [consistentUids]
cmd-loadmap-nullspace = You cannot load into map 0.
cmd-loadmap-exists = Map {$mapId} already exists.
cmd-loadmap-success = Map {$mapId} has been loaded from {$path}.
cmd-loadmap-error = An error occurred while loading map from {$path}.
cmd-hint-loadmap-x-position = [x-position]
cmd-hint-loadmap-y-position = [y-position]
cmd-hint-loadmap-rotation = [rotation]
cmd-hint-loadmap-uids = [float]

cmd-hint-savebp-id = <Grid EntityID>

## 'flushcookies' command
# Note: the flushcookies command is from Robust.Client.WebView, it's not in the main engine code.

cmd-flushcookies-desc = Flush CEF cookie storage to disk.
cmd-flushcookies-help = Usage: {$command}
    This ensure cookies are properly saved to disk in the event of unclean shutdowns.
    Note that the actual operation is asynchronous.

cmd-ldrsc-desc = Pre-caches a resource.
cmd-ldrsc-help = Usage: {$command} <path> <type>

cmd-rldrsc-desc = Reloads a resource.
cmd-rldrsc-help = Usage: {$command} <path> <type>

cmd-gridtc-desc = Gets the tile count of a grid.
cmd-gridtc-help = Usage: {$command} <gridId>


# Client-side commands
cmd-guidump-desc = Dump GUI tree to /guidump.txt in user data.
cmd-guidump-help = Usage: {$command}

cmd-uitest-desc = Open a dummy UI testing window.
cmd-uitest-help = Usage: {$command}

## 'uitest2' command
cmd-uitest2-desc = Opens a UI control testing OS window.
cmd-uitest2-help = Usage: {$command} <tab>
cmd-uitest2-arg-tab = <tab>
cmd-uitest2-error-args = Expected at most one argument
cmd-uitest2-error-tab = Invalid tab: '{$value}'
cmd-uitest2-title = UITest2


cmd-setclipboard-desc = Sets the system clipboard.
cmd-setclipboard-help = Usage: {$command} <text>

cmd-getclipboard-desc = Gets the system clipboard.
cmd-getclipboard-help = Usage: {$command}

cmd-togglelight-desc = Toggles light rendering.
cmd-togglelight-help = Usage: {$command}

cmd-togglefov-desc = Toggles fov for client.
cmd-togglefov-help = Usage: {$command}

cmd-togglehardfov-desc = Toggles hard fov for client. (for debugging space-station-14#2353)
cmd-togglehardfov-help = Usage: {$command}

cmd-toggleshadows-desc = Toggles shadow rendering.
cmd-toggleshadows-help = Usage: {$command}

cmd-togglelightbuf-desc = Toggles lighting rendering. This includes shadows but not FOV.
cmd-togglelightbuf-help = Usage: {$command}

cmd-chunkinfo-desc = Gets info about a chunk under your mouse cursor.
cmd-chunkinfo-help = Usage: {$command}

cmd-rldshader-desc = Reloads all shaders.
cmd-rldshader-help = Usage: {$command}

cmd-cldbglyr-desc = Toggle fov and light debug layers.
cmd-cldbglyr-help= Usage: {$command} <layer>: Toggle <layer>
    cldbglyr: Turn all Layers off

cmd-key-info-desc = Keys key info for a key.
cmd-key-info-help = Usage: {$command} <Key>

## 'bind' command
cmd-bind-desc = Binds an input key combination to an input command.
cmd-bind-help = Usage: {$command} { cmd-bind-arg-key } { cmd-bind-arg-mode } { cmd-bind-arg-command }
    Note that this DOES NOT automatically save bindings.
    Use the 'svbind' command to save binding configuration.

cmd-bind-arg-key = <KeyName>
cmd-bind-arg-mode = <BindMode>
cmd-bind-arg-command = <InputCommand>

cmd-net-draw-interp-desc = Toggles the debug drawing of the network interpolation.
cmd-net-draw-interp-help = Usage: {$command}

cmd-net-watch-ent-desc = Dumps all network updates for an EntityId to the console.
cmd-net-watch-ent-help = Usage: {$command} <0|EntityUid>

cmd-net-refresh-desc = Requests a full server state.
cmd-net-refresh-help = Usage: {$command}

cmd-net-entity-report-desc = Toggles the net entity report panel.
cmd-net-entity-report-help = Usage: {$command}

cmd-fill-desc = Fill up the console for debugging.
cmd-fill-help = Usage: {$command}
                Fills the console with some nonsense for debugging.

cmd-cls-desc = Clears the console.
cmd-cls-help = Usage: {$command}
               Clears the debug console of all messages.

cmd-sendgarbage-desc = Sends garbage to the server.
cmd-sendgarbage-help = Usage: {$command}
                       The server will reply with 'no u'

cmd-loadgrid-desc = Loads a grid from a file into an existing map.
cmd-loadgrid-help = Usage: {$command} <MapID> <Path> [x y] [rotation] [storeUids]

cmd-loc-desc = Prints the absolute location of the player's entity to console.
cmd-loc-help = Usage: {$command}

cmd-tpgrid-desc = Teleports a grid to a new location.
cmd-tpgrid-help = Usage: {$command} <gridId> <X> <Y> [<MapId>]

cmd-rmgrid-desc = Removes a grid from a map. You cannot remove the default grid.
cmd-rmgrid-help = Usage: {$command} <gridId>

cmd-mapinit-desc = Runs map init on a map.
cmd-mapinit-help = Usage: {$command} <mapID>

cmd-lsmap-desc = Lists maps.
cmd-lsmap-help = Usage: {$command}

cmd-lsgrid-desc = Lists grids.
cmd-lsgrid-help = Usage: {$command}

cmd-addmap-desc = Adds a new empty map to the round. If the mapID already exists, this command does nothing.
cmd-addmap-help = Usage: {$command} <mapID> [pre-init]

cmd-rmmap-desc = Removes a map from the world. You cannot remove nullspace.
cmd-rmmap-help = Usage: {$command} <mapId>

cmd-savegrid-desc = Serializes a grid to disk.
cmd-savegrid-help = Usage: {$command} <gridID> <Path>

cmd-testbed-desc = Loads a physics testbed on the specified map.
cmd-testbed-help = Usage: {$command} <mapid> <test>

## 'flushcookies' command
# Note: the flushcookies command is from Robust.Client.WebView, it's not in the main engine code.

## 'addcomp' command
cmd-addcomp-desc = Adds a component to an entity.
cmd-addcomp-help = Usage: {$command} <uid> <componentName>
cmd-addcompc-desc = Adds a component to an entity on the client.
cmd-addcompc-help = Usage: {$command} <uid> <componentName>

## 'rmcomp' command
cmd-rmcomp-desc = Removes a component from an entity.
cmd-rmcomp-help = Usage: {$command} <uid> <componentName>
cmd-rmcompc-desc = Removes a component from an entity on the client.
cmd-rmcompc-help = Usage: {$command} <uid> <componentName>

## 'addview' command
cmd-addview-desc = Allows you to subscribe to an entity's view for debugging purposes.
cmd-addview-help = Usage: {$command} <entityUid>
cmd-addviewc-desc = Allows you to subscribe to an entity's view for debugging purposes.
cmd-addviewc-help = Usage: {$command} <entityUid>

## 'removeview' command
cmd-removeview-desc = Allows you to unsubscribe to an entity's view for debugging purposes.
cmd-removeview-help = Usage: {$command} <entityUid>

## 'loglevel' command
cmd-loglevel-desc = Changes the log level for a provided sawmill.
cmd-loglevel-help = Usage: {$command} <sawmill> <level>
      sawmill: A label prefixing log messages. This is the one you're setting the level for.
      level: The log level. Must match one of the values of the LogLevel enum.

cmd-testlog-desc = Writes a test log to a sawmill.
cmd-testlog-help = Usage: {$command} <sawmill> <level> <message>
    sawmill: A label prefixing the logged message.
    level: The log level. Must match one of the values of the LogLevel enum.
    message: The message to be logged. Wrap this in double quotes if you want to use spaces.

## 'vv' command
cmd-vv-desc = Opens View Variables.
cmd-vv-help = Usage: {$command} <entity ID|IoC interface name|SIoC interface name>

## 'showvelocities' command
cmd-showvelocities-desc = Displays your angular and linear velocities.
cmd-showvelocities-help = Usage: {$command}

## 'setinputcontext' command
cmd-setinputcontext-desc = Sets the active input context.
cmd-setinputcontext-help = Usage: {$command} <context>

## 'forall' command
cmd-forall-desc = Runs a command over all entities with a given component.
cmd-forall-help = Usage: {$command} <bql query> do <command...>

## 'delete' command
cmd-delete-desc = Deletes the entity with the specified ID.
cmd-delete-help = Usage: {$command} <entity UID>

# System commands
cmd-showtime-desc = Shows the server time.
cmd-showtime-help = Usage: {$command}

cmd-restart-desc = Gracefully restarts the server (not just the round).
cmd-restart-help = Usage: {$command}

cmd-shutdown-desc = Gracefully shuts down the server.
cmd-shutdown-help = Usage: {$command}

cmd-saveconfig-desc = Saves the server configuration to the config file.
cmd-saveconfig-help = Usage: {$command}

cmd-netaudit-desc = Prints into about NetMsg security.
cmd-netaudit-help = Usage: {$command}

# Player commands
cmd-tp-desc = Teleports a player to any location in the round.
cmd-tp-help = Usage: {$command} <x> <y> [<mapID>]

cmd-tpto-desc = Teleports the current player or the specified players/entities to the location of the first player/entity.
cmd-tpto-help = Usage: {$command} <username|uid> [username|NetEntity]...
cmd-tpto-destination-hint = destination (NetEntity or username)
cmd-tpto-victim-hint = entity to teleport (NetEntity or username)
cmd-tpto-parse-error = Cant resolve entity or player: {$str}

cmd-listplayers-desc = Lists all players currently connected.
cmd-listplayers-help = Usage: {$command}

cmd-kick-desc = Kicks a connected player out of the server, disconnecting them.
cmd-kick-help = Usage: {$command} <PlayerIndex> [<Reason>]

# Spin command
cmd-spin-desc = Causes an entity to spin. Default entity is the attached player's parent.
cmd-spin-help = Usage: {$command} velocity [drag] [entityUid]

# Localization command
cmd-rldloc-desc = Reloads localization (client & server).
cmd-rldloc-help = Usage: {$command}

# Debug entity controls
cmd-spawn-desc = Spawns an entity with specific type.
cmd-spawn-help = Usage: {$command} <prototype> | {$command} <prototype> <relative entity ID> | {$command} <prototype> <x> <y>
cmd-cspawn-desc = Spawns a client-side entity with specific type at your feet.
cmd-cspawn-help = Usage: {$command} <entity type>

cmd-dumpentities-desc = Dump entity list.
cmd-dumpentities-help = Usage: {$command}
                        Dumps entity list of UIDs and prototype.

cmd-getcomponentregistration-desc = Gets component registration information.
cmd-getcomponentregistration-help = Usage: {$command} <componentName>

cmd-showrays-desc = Toggles debug drawing of physics rays. An integer for <raylifetime> must be provided.
cmd-showrays-help = Usage: {$command} <raylifetime>

cmd-disconnect-desc = Immediately disconnect from the server and go back to the main menu.
cmd-disconnect-help = Usage: {$command}

cmd-entfo-desc = Displays verbose diagnostics for an entity.
cmd-entfo-help = Usage: {$command} <entityuid>
    The entity UID can be prefixed with 'c' to convert it to a client entity UID.

cmd-fuck-desc = Throws an exception.
cmd-fuck-help = Usage: {$command}

cmd-showpos-desc = Show the position of all entities on the screen.
cmd-showpos-help = Usage: {$command}

cmd-showrot-desc = Show the rotation of all entities on the screen.
cmd-showrot-help = Usage: {$command}

cmd-showvel-desc = Show the local velocity of all entites on the screen.
cmd-showvel-help = Usage: {$command}

cmd-showangvel-desc = Show the angular velocity of all entities on the screen.
cmd-showangvel-help = Usage: {$command}

cmd-sggcell-desc = Lists entities on a snap grid cell.
cmd-sggcell-help = Usage: {$command} <gridID> <vector2i>\nThat vector2i param is in the form x<int>,y<int>.

cmd-overrideplayername-desc = Changes the name used when attempting to connect to the server.
cmd-overrideplayername-help = Usage: {$command} <name>

cmd-showanchored-desc = Shows anchored entities on a particular tile.
cmd-showanchored-help = Usage: {$command}

cmd-dmetamem-desc = Dumps a type's members in a format suitable for the sandbox configuration file.
cmd-dmetamem-help = Usage: {$command} <type>

cmd-launchauth-desc = Load authentication tokens from launcher data to aid in testing of live servers.
cmd-launchauth-help = Usage: {$command} <account name>

cmd-lightbb-desc = Toggles whether to show light bounding boxes.
cmd-lightbb-help = Usage: {$command}

cmd-monitorinfo-desc = Monitors info.
cmd-monitorinfo-help = Usage: {$command} <id>

cmd-setmonitor-desc = Set monitor.
cmd-setmonitor-help = Usage: {$command} <id>

cmd-physics-desc = Shows a debug physics overlay. The arg supplied specifies the overlay.
cmd-physics-help = Usage: {$command} <aabbs / com / contactnormals / contactpoints / distance / joints / shapeinfo / shapes>

cmd-hardquit-desc = Kills the game client instantly.
cmd-hardquit-help = Usage: {$command}
                    Kills the game client instantly, leaving no traces. No telling the server goodbye.

cmd-quit-desc = Shuts down the game client gracefully.
cmd-quit-help = Usage: {$command}
                Properly shuts down the game client, notifying the connected server and such.

cmd-csi-desc = Opens a C# interactive console.
cmd-csi-help = Usage: {$command}

cmd-scsi-desc = Opens a C# interactive console on the server.
cmd-scsi-help = Usage: {$command}

cmd-watch-desc = Opens a variable watch window.
cmd-watch-help = Usage: {$command}

cmd-showspritebb-desc = Toggle whether sprite bounds are shown.
cmd-showspritebb-help = Usage: {$command}

cmd-togglelookup-desc = Shows / hides entitylookup bounds via an overlay.
cmd-togglelookup-help = Usage: {$command}

cmd-net_entityreport-desc = Toggles the net entity report panel.
cmd-net_entityreport-help = Usage: {$command}

cmd-net_refresh-desc = Requests a full server state.
cmd-net_refresh-help = Usage: {$command}

cmd-net_graph-desc = Toggles the net statistics panel.
cmd-net_graph-help = Usage: {$command}

cmd-net_watchent-desc = Dumps all network updates for an EntityId to the console.
cmd-net_watchent-help = Usage: {$command} <0|EntityUid>

cmd-net_draw_interp-desc = Toggles the debug drawing of the network interpolation.
cmd-net_draw_interp-help = Usage: {$command} <0|EntityUid>

cmd-vram-desc = Displays video memory usage statics by the game.
cmd-vram-help = Usage: {$command}

cmd-showislands-desc = Shows the current physics bodies involved in each physics island.
cmd-showislands-help = Usage: {$command}

cmd-showgridnodes-desc = Shows the nodes for grid split purposes.
cmd-showgridnodes-help = Usage: {$command}

cmd-profsnap-desc = Make a profiling snapshot.
cmd-profsnap-help = Usage: {$command}

cmd-devwindow-desc = Dev Window.
cmd-devwindow-help = Usage: {$command}

cmd-scene-desc = Immediately changes the UI scene/state.
cmd-scene-help = Usage: {$command} <className>

cmd-szr_stats-desc = Report serializer statistics.
cmd-szr_stats-help = Usage: {$command}

cmd-hwid-desc = Returns the current HWID (HardWare ID).
cmd-hwid-help = Usage: {$command}

cmd-vvread-desc = Retrieve a path's value using VV (View Variables).
cmd-vvread-help = Usage: {$command} <path>

cmd-vvwrite-desc = Modify a path's value using VV (View Variables).
cmd-vvwrite-help = Usage: {$command} <path>

cmd-vvinvoke-desc = Invoke/Call a path with arguments using VV.
cmd-vvinvoke-help = Usage: {$command} <path> [arguments...]

cmd-dump_dependency_injectors-desc = Dump IoCManager's dependency injector cache.
cmd-dump_dependency_injectors-help = Usage: {$command}
cmd-dump_dependency_injectors-total-count = Total count: { $total }

cmd-dump_netserializer_type_map-desc = Dump NetSerializer's type map and serializer hash.
cmd-dump_netserializer_type_map-help = Usage: {$command}

cmd-hub_advertise_now-desc = Immediately advertise to the master hub server.
cmd-hub_advertise_now-help = Usage: {$command}

cmd-echo-desc = Echo arguments back to the console.
cmd-echo-help = Usage: {$command} "<message>"

## 'vfs_ls' command
cmd-vfs_ls-desc = List directory contents in the VFS.
cmd-vfs_ls-help = Usage: {$command} <path>
    Example:
    vfs_list /Assemblies

cmd-vfs_ls-err-args = Need exactly 1 argument.
cmd-vfs_ls-hint-path = <path>

cmd-reloadtiletextures-desc = Reloads the tile texture atlas to allow hot reloading tile sprites.
cmd-reloadtiletextures-help = Usage: {$command}

cmd-audio_length-desc = Shows the length of an audio file
cmd-audio_length-help = Usage: {$command} { cmd-audio_length-arg-file-name }
cmd-audio_length-arg-file-name = <file name>

## PVS
cmd-pvs-override-info-desc = Prints information about any PVS overrides associated with an entity.
cmd-pvs-override-info-empty = Entity {$nuid} has no PVS overrides.
cmd-pvs-override-info-global = Entity {$nuid} has a global override.
cmd-pvs-override-info-clients = Entity {$nuid} has a session override for {$clients}.

cmd-localization_set_culture-desc = Set DefaultCulture for the client LocalizationManager.
cmd-localization_set_culture-help = Usage: {$command} <cultureName>
cmd-localization_set_culture-culture-name = <cultureName>
cmd-localization_set_culture-changed = Localization changed to { $code } ({ $nativeName } / { $englishName })

cmd-addmap-hint-2 = runMapInit [true / false]
