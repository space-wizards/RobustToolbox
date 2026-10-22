using System;
using JetBrains.Annotations;

namespace Robust.UnitTesting.Pool;

/// <summary>
/// Attribute that indicates that a string contains yaml prototype data that should be loaded by integration tests.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
[MeansImplicitUse]
public sealed class TestPrototypesAttribute : Attribute
{
}
