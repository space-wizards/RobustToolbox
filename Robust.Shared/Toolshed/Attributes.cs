using System;
using JetBrains.Annotations;

namespace Robust.Shared.Toolshed;

[AttributeUsage(AttributeTargets.Class)]
[MeansImplicitUse]
public sealed class RtShellCommandAttribute : Attribute
{
    public string? Name = null;
}

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

[AttributeUsage(AttributeTargets.Parameter)]
[MeansImplicitUse]
public sealed class PipedArgumentAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Parameter)]
[MeansImplicitUse]
public sealed class CommandArgumentAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Parameter)]
[MeansImplicitUse]
public sealed class CommandInvertedAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Parameter)]
[MeansImplicitUse]
public sealed class CommandInvocationContextAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class TakesPipedTypeAsGeneric : Attribute
{
}
