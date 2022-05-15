### Localization for engine console commands

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
