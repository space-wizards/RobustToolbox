command-description-tpto =
    Teleport the given entities to some target entity.
command-description-runverbas =
    Runs the given verb on some given entities, using the target entity as the runner.
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
