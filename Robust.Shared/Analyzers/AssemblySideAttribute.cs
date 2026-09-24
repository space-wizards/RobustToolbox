using System;

namespace Robust.Shared.Analyzers;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class AssemblySideAttribute(AssemblySide side) : Attribute
{
    public readonly AssemblySide Side = side;
}

public enum AssemblySide
{
    Shared,
    Server,
    Client
}
