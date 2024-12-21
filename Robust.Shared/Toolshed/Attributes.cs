using System;
using JetBrains.Annotations;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

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
public sealed class PipedArgumentAttribute : Attribute;

/// <summary>
/// Marks an argument in a function as being an argument of a <see cref="ToolshedCommand"/>. Unless a custom parser is
/// specified, the default parser for the argument's type will be used. This attribute is implicitly present if a
/// parameter has no other relevant attributes and the parameter type is not <see cref="IInvocationContext"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
[MeansImplicitUse]
public sealed class CommandArgumentAttribute : Attribute
{
    public CommandArgumentAttribute(Type? customParser = null)
    {
        if (customParser == null)
            return;

        CustomParser = customParser;
        DebugTools.Assert(customParser.IsCustomParser(),
            $"Custom parser {customParser.PrettyName()} does not inherit from {typeof(CustomTypeParser<>).PrettyName()}");
    }

    public Type? CustomParser { get; }
}

/// <summary>
///     Marks an argument in a function as specifying whether or not this call to a <see cref="ToolshedCommand"/> is inverted.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
[MeansImplicitUse]
public sealed class CommandInvertedAttribute : Attribute;

/// <summary>
/// Marks an argument in a function as being where the invocation context should be provided in a
/// <see cref="ToolshedCommand"/>. This attribute is implicitly present if one of the arguments is of type
/// <see cref="IInvocationContext"/> and has no other relevant attributes.
/// </summary>
/// <seealso cref="IInvocationContext"/>
[AttributeUsage(AttributeTargets.Parameter)]
[MeansImplicitUse]
public sealed class CommandInvocationContextAttribute : Attribute;

/// <summary>
///     Marks a command implementation as taking the type of the previous command in sequence as a generic argument. Supports only one generic type.
/// </summary>
/// <remarks>
///     If the argument marked with <see cref="PipedArgumentAttribute"/> is not <c>T</c> but instead a pattern like <c>IEnumerable&lt;T&gt;</c>,
///     Toolshed will account for this by using <see cref="ReflectionExtensions.IntersectWithGeneric"/>. It's not very precise.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TakesPipedTypeAsGenericAttribute : Attribute;
