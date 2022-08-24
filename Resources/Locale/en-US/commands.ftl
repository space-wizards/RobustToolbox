### Localization for engine console commands

## generic command errors

cmd-invalid-arg-number-error = Invalid number of arguments.

cmd-parse-failure-integer = {$arg} is not a valid integer.
cmd-parse-failure-float = {$arg} is not a valid float.
cmd-parse-failure-bool = {$arg} is not a valid bool.
cmd-parse-failure-uid = {$arg} is not a valid entity UID.
cmd-parse-failure-entity-exist = UID {$arg} does not correspond to an existing entity.


## 'help' command
cmd-help-desc = Display general help or help text for a specific command
cmd-help-help = Usage: help [command name]
    When no command name is provided, displays general-purpose help text. If a command name is provided, displays help text for that command.

cmd-help-no-args = To display help for a specific command, write 'help <command>'. To list all available commands, write 'list'. To search for commands, use 'list <filter>'.
cmd-help-unknown = Unknown command: { $command }
cmd-help-top = { $command } - { $description }
cmd-help-invalid-args = Invalid amount of arguments.
cmd-help-arg-cmdname = [command name]

## 'cvar' command
cmd-cvar-desc = Gets or sets a CVar.
cmd-cvar-help = Usage: cvar <name | ?> [value]
    If a value is passed, the value is parsed and stored as the new value of the CVar.
    If not, the current value of the CVar is displayed.
    Use 'cvar ?' to get a list of all registered CVars.

cmd-cvar-invalid-args = Must provide exactly one or two arguments.
cmd-cvar-not-registered = CVar '{ $cvar }' is not registered. Use 'cvar ?' to get a list of all registered CVars.
cmd-cvar-parse-error = Input value is in incorrect format for type { $type }
cmd-cvar-compl-list = List available CVars
cmd-cvar-arg-name = <name | ?>
cmd-cvar-value-hidden = <value hidden>

## 'list' command
cmd-list-desc = Lists available commands, with optional search filter
cmd-list-help = Usage: list [filter]
    Lists all available commands. If an argument is provided, it will be used to filter commands by name.

cmd-list-heading = SIDE NAME            DESC{"\u000A"}-------------------------{"\u000A"}

cmd-list-arg-filter = [filter]

## '>' command, aka remote exec
cmd-remoteexec-desc = Executes server-side commands
cmd-remoteexec-help = Usage: > <command> [arg] [arg] [arg...]
    Executes a command on the server. This is necessary if a command with the same name exists on the client, as simply running the command would run the client command first.

## 'gc' command
cmd-gc-desc = Run the GC (Garbage Collector)
cmd-gc-help = Usage: gc [generation]
    Uses GC.Collect() to execute the Garbage Collector.
    If an argument is provided, it is parsed as a GC generation number and GC.Collect(int) is used.
    Use the 'gfc' command to do an LOH-compacting full GC.
cmd-gc-failed-parse = Failed to parse argument.
cmd-gc-arg-generation = [generation]

## 'gcf' command
cmd-gcf-desc = Run the GC, fully, compacting LOH and everything.
cmd-gcf-help = Usage: gcf
    Does a full GC.Collect(2, GCCollectionMode.Forced, true, true) while also compacting LOH.
    This will probably lock up for hundreds of milliseconds, be warned.

## 'gc_mode' command
cmd-gc_mode-desc = Change/Read the GC Latency mode
cmd-gc_mode-help = Usage: gc_mode [type]
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
cmd-mem-desc = Prints managed memory info
cmd-mem-help = Usage: mem

cmd-mem-report = Heap Size: { TOSTRING($heapSize, "N0") }
    Total Allocated: { TOSTRING($totalAllocated, "N0") }

## 'physics' command
cmd-physics-overlay = {$overlay} is not a recognised overlay

## 'lsasm' command
cmd-lsasm-desc = Lists loaded assemblies by load context
cmd-lsasm-help = Usage: lsasm

## 'exec' command
cmd-exec-desc = Executes a script file from the game's writeable user data
cmd-exec-help = Usage: exec <fileName>
    Each line in the file is executed as a single command, unless it starts with a #

cmd-exec-arg-filename = <fileName>

## 'dump_net_comps' command
cmd-dump_net_comps-desc = Prints the table of networked components.
cmd-dump_net_comps-help = Usage: dump_net-comps

cmd-dump_net_comps-error-writeable = Registration still writeable, network ids have not been generated.
cmd-dump_net_comps-header = Networked Component Registrations:

## 'dump_event_tables' command
cmd-dump_event_tables-desc = Prints directed event tables for an entity.
cmd-dump_event_tables-help = Usage: dump_event_tables <entityUid>

cmd-dump_event_tables-missing-arg-entity = Missing entity argument
cmd-dump_event_tables-error-entity = Invalid entity
cmd-dump_event_tables-arg-entity = <entityUid>

## 'monitor' command
cmd-monitor-desc = Toggles a debug monitor in the F3 menu.
cmd-monitor-help = Usage: monitor <name>
    Possible monitors are: { $monitors }
    You can also use the special values "-all" and "+all" to hide or show all monitors, respectively.

cmd-monitor-arg-monitor = <monitor>
cmd-monitor-invalid-name = Invalid monitor name
cmd-monitor-arg-count = Missing monitor argument
cmd-monitor-minus-all-hint = Hides all monitors
cmd-monitor-plus-all-hint = Shows all monitors


## Mapping commands

cmd-savemap-desc = Serializes a map to disk. Will not save a post-init map unless forced.
cmd-savemap-help = savemap <MapID> <Path> [force]
cmd-savemap-not-exist = Target map does not exist.
cmd-savemap-init-warning = Attempted to save a post-init map without forcing the save.
cmd-savemap-attempt = Attempting to save map {$mapId} to {$path}.
cmd-savemap-success = Map successfully saved.
cmd-hint-savemap-id = <MapID>
cmd-hint-savemap-path = <Path>
cmd-hint-savemap-force = [bool]

cmd-loadmap-desc = Loads a map from disk into the game.
cmd-loadmap-help = loadmap <MapID> <Path> [x] [y] [rotation] [consistentUids]
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

cmd-flushcookies-desc = Flush CEF cookie storage to disk
cmd-flushcookies-help = This ensure cookies are properly saved to disk in the event of unclean shutdowns.
    Note that the actual operation is asynchronous.
