command-description-tpto =
    Teleport the given entities to some target entity.
command-description-player-list =
    Returns a list of all player sessions.
command-description-player-self =
    Returns the current player session.
command-description-player-imm =
    Returns the session associated with the player given as argument.
command-description-player-entity =
    Returns the entities of the input sessions.
command-description-self =
    Returns the current attached entity.
command-description-physics-velocity =
    Returns the velocity of the input entities.
command-description-physics-angular-velocity =
    Returns the angular velocity of the input entities.
command-description-buildinfo =
    Provides information about the build of the game.
command-description-cmd-list =
    Returns a list of all commands, for this side.
command-description-explain =
    Explains the given expression, providing command descriptions and signatures.
command-description-search =
    Searches through the input for the provided value.
command-description-stopwatch =
    Measures the execution time of the given expression.
command-description-types-consumers =
    Provides all commands that can consume the given type.
command-description-types-tree =
    Debug tool to return all types the command interpreter can downcast the input to.
command-description-types-gettype =
    Returns the type of the input.
command-description-types-fullname =
    Returns the full name of the input type according to CoreCLR.
command-description-as =
    Casts the input to the given type.
    Effectively a type hint if you know the type but the interpreter does not.
command-description-count =
    Counts the amount of entries in it's input, returning an integer.
command-description-map =
    Maps the input over the given block, with the provided expected return type.
    This command may be modified to not need an explicit return type in the future.
command-description-select =
    Selects N objects or N% of objects from the input.
    One can additionally invert this command with not to make it select everything except N objects instead.
command-description-comp =
    Returns the given component from the input entities, discarding entities without that component.
command-description-delete =
    Deletes the input entities.
command-description-ent =
    Returns the provided entity ID.
command-description-entities =
    Returns all entities on the server.
command-description-paused =
    Filters the input entities by whether or not they are paused.
    This command can be inverted with not.
command-description-with =
    Filters the input entities by whether or not they have the given component.
    This command can be inverted with not.
command-description-fuck =
    Throws an exception.
command-description-ecscomp-listty =
    Lists every type of component registered.
command-description-cd =
    Changes the session's current directory to the given relative or absolute path.
command-description-ls-here =
    Lists the contents of the current directory.
command-description-ls-in =
    Lists the contents of the given relative or absolute path.
command-description-methods-get =
    Returns all methods associated with the input type.
command-description-methods-overrides =
    Returns all methods overriden on the input type.
command-description-methods-overridesfrom =
    Returns all methods overriden from the given type on the input type.
command-description-cmd-moo =
    Asks the important questions.
command-description-cmd-descloc =
    Returns the localization string for a command's description.
command-description-cmd-getshim =
    Returns a command's execution shim.
command-description-help =
    Provides a quick rundown of how to use toolshed.
command-description-ioc-registered =
    Returns all the types registered with IoCManager on the current thread (usually the game thread)
command-description-ioc-get =
    Gets an instance of an IoC registration.
command-description-loc-tryloc =
    Tries to get a localization string, returning null if unable.
command-description-loc-loc =
    Gets a localization string, returning the unlocalized string if unable.
command-description-physics-angular_velocity =
    Returns the angular velocity of the given entities.
command-description-vars =
    Provides a list of all variables set in this session.
command-description-any =
    Returns true if there's any values in the input, otherwise false.
command-description-ArrowCommand =
    Assigns the input to a variable.
command-description-isempty =
    Returns true if the input is empty, otherwise false.
command-description-isnull =
    Returns true if the input is null, otherwise false.
command-description-unique =
    Filters the input sequence for uniqueness, removing duplicate values.
command-description-where =
    Given some input sequence IEnumerable<T>, takes a block of signature T -> bool that decides if each input value should be included in the output sequence.
command-description-do =
    Backwards compatibility with BQL, applies the given old commands over the input sequence.
command-description-named =
    Filters the input entities by their name, with the regex $selector^.
command-description-prototyped =
    Filters the input entities by their prototype.
command-description-nearby =
    Creates a new list of all entities nearby the inputs within the given range.
command-description-first =
    Returns the first entry of the given enumerable.
command-description-splat =
    "Splats" a block, value, or variable, creating N copies of it in a list.
command-description-val =
    Casts the given value, block, or variable to the given type. This is mostly a workaround for current limitations of variables.
command-description-actor-controlled =
    Filters entities by whether or not they're actively controlled.
command-description-actor-session =
    Returns the sessions associated with the input entities.
command-description-physics-parent =
    Returns the parent(s) of the input entities.
command-description-emplace =
    Runs the given block over it's inputs, with the input value placed into the variable $value within the block.
    Additionally breaks out $wx, $wy, $proto, $desc, $name, and $paused for entities.
    Can also have breakout values for other types, consult the documentation for that type for further info.
command-description-AddCommand =
    Performs numeric addition.
command-description-SubtractCommand =
    Performs numeric subtraction.
command-description-MultiplyCommand =
    Performs numeric multiplication.
command-description-DivideCommand =
    Performs numeric division.
command-description-min =
    Returns the minimum of two values.
command-description-max =
    Returns the maximum of two values.
command-description-BitAndCommand =
    Performs bitwise AND.
command-description-BitOrCommand =
    Performs bitwise OR.
command-description-BitXorCommand =
    Performs bitwise XOR.
command-description-neg =
    Negates the input.
command-description-GreaterThanCommand =
    Performs a greater-than comparison, x > y.
command-description-LessThanCommand =
    Performs a less-than comparison, x < y.
command-description-GreaterThanOrEqualCommand =
    Performs a greater-than-or-equal comparison, x >= y.
command-description-LessThanOrEqualCommand =
    Performs a less-than-or-equal comparison, x <= y.
command-description-EqualCommand =
    Performs an equality comparison, returning true if the inputs are equal.
command-description-NotEqualCommand =
    Performs an equality comparison, returning true if the inputs are not equal.
