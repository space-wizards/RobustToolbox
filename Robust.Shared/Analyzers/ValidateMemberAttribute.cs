using System;

namespace Robust.Shared.Analyzers;

/// <summary>
/// Verifies that a string parameter matches the name
/// of a member of the first type argument.
/// </summary>
/// <remarks>
/// This just does a string comparison with the member name.
/// An identically-named member on a different class will be
/// considered valid.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ValidateMemberAttribute : Attribute;
