using System;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.Console;

/// <summary>
/// Contains the result of a command completion.
/// </summary>
[Serializable, NetSerializable]
public sealed record CompletionResult(string[] Options, string? Hint)
{
    /// <summary>
    /// The possible full arguments to complete with. These are from the start of the entire argument.
    /// </summary>
    public string[] Options { get; init; } = Options;

    /// <summary>
    /// Type hint string for the current argument being typed.
    /// </summary>
    public string? Hint { get; init; } = Hint;

    public static readonly CompletionResult Empty = new(Array.Empty<string>(), null);
    public static CompletionResult FromOptions(string[] options) => new(options, null);
    public static CompletionResult FromHint(string hint) => new(Array.Empty<string>(), hint);
}
