using System;
using JetBrains.Annotations;

namespace Robust.Shared.Toolshed;

/// <summary>
///     Used to mark a class so that <see cref="ToolshedManager"/> automatically discovers and registers it.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
[MeansImplicitUse]
public sealed class ToolshedCommandAttribute : Attribute
{
    public string? Name = null;
}

/// <summary>
///     Marks a function in a <see cref="ToolshedCommand"/> as being an implementation of that command, so that Toolshed will use it's signature for parsing/etc.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
[MeansImplicitUse]
public sealed class CommandImplementationAttribute :  Attribute
{
    public readonly string? SubCommand = null;

    public CommandImplementationAttribute(string? subCommand = null)
    {
        SubCommand = subCommand;
    }
}

/// <summary>
///     Marks an argument in a function in a <see cref="ToolshedCommand"/> as being the "piped" argument, the return value of the prior command in the chain.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
[MeansImplicitUse]
public sealed class PipedArgumentAttribute : Attribute
{
}

/// <summary>
///     Marks an argument in a function as being an argument of a <see cref="ToolshedCommand"/>.
///     This will make it so the argument will get parsed.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
[MeansImplicitUse]
public sealed class CommandArgumentAttribute : Attribute
{
}

/// <summary>
///     Marks an argument in a function as specifying whether or not this call to a <see cref="ToolshedCommand"/> is inverted.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
[MeansImplicitUse]
public sealed class CommandInvertedAttribute : Attribute
{
}

/// <summary>
///     Marks an argument in a function as being where the invocation context should be provided in a <see cref="ToolshedCommand"/>.
/// </summary>
/// <seealso cref="IInvocationContext"/>
[AttributeUsage(AttributeTargets.Parameter)]
[MeansImplicitUse]
public sealed class CommandInvocationContextAttribute : Attribute
{
}

/// <summary>
///     Marks a command implementation as taking the type of the previous command in sequence as a generic argument.
/// </summary>
/// <remarks>
///     If the argument marked with <see cref="PipedArgumentAttribute"/> is not <c>T</c> but instead a pattern like <c>IEnumerable&lt;T&gt;</c>, Toolshed will account for this.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TakesPipedTypeAsGenericAttribute : Attribute
{
}

// Internal because this is just a hack at the moment and should be replaced with proper inference later!
// Overrides type argument parsing to parse a block and then use it's return type as the sole type argument.
internal sealed class MapLikeCommandAttribute : Attribute
{
    public bool TakesPipedType;

    public MapLikeCommandAttribute(bool takesPipedType = true)
    {
        TakesPipedType = takesPipedType;
    }
}
