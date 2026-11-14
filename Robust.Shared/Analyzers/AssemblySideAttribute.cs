using System;

namespace Robust.Shared.Analyzers;

/// <summary>
/// Indicates that an assembly belongs to a particular network side,
/// either Server, Client, or Shared.
/// </summary>
/// <remarks>
/// Used to indicate project structure to analyzers.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class AssemblySideAttribute(AssemblySide side) : Attribute
{
    public readonly AssemblySide Side = side;
}

/// <summary>
/// Represents a network side that an assembly can belong to.
/// </summary>
public enum AssemblySide
{
    Shared,
    Server,
    Client
}
