### Localization for engine console commands

## help command
cmd-help-desc = Display general help or help text for a specific command
cmd-help-help = Usage: help [command name]
    When no command name is provided, displays general-purpose help text. If a command name is provided, displays help text for that command.

cmd-help-no-args = To display help for a specific command, write 'help <command>'. To list all available commands, write 'list'.
cmd-help-unknown = Unknown command: { $command }
cmd-help-top = { $command } - { $description }
cmd-help-invalid-args = Invalid amount of arguments.
cmd-help-arg-cmdname = [command name]

## cvar command
cmd-cvar-desc = Gets or sets a CVar.
cmd-cvar-help = cvar <name | ?> [value]
    If a value is passed, the value is parsed and stored as the new value of the CVar.
    If not, the current value of the CVar is displayed.
    Use 'cvar ?' to get a list of all registered CVars.

cmd-cvar-invalid-args = Must provide exactly one or two arguments.
cmd-cvar-not-registered = CVar '{ $cvar }' is not registered. Use 'cvar ?' to get a list of all registered CVars.
cmd-cvar-parse-error = Input value is in incorrect format for type { $type }
