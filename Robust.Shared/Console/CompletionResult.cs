using System;
using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Console;

/// <summary>
/// Contains the result of a command completion.
/// </summary>
public sealed record CompletionResult(CompletionOption[] Options, string? Hint)
{
    /// <summary>
    /// The possible full arguments to complete with. These are from the start of the entire argument.
    /// </summary>
    public CompletionOption[] Options { get; init; } = Options;

    /// <summary>
    /// Type hint string for the current argument being typed.
    /// </summary>
    public string? Hint { get; init; } = Hint;

    public static readonly CompletionResult Empty = new(Array.Empty<CompletionOption>(), null);

    public static CompletionResult FromHintOptions(IEnumerable<string> options, string? hint) => new(ConvertOptions(options), hint);
    public static CompletionResult FromHintOptions(IEnumerable<CompletionOption> options, string? hint) => new(options.ToArray(), hint);

    public static CompletionResult FromOptions(IEnumerable<string> options) => new(ConvertOptions(options), null);
    public static CompletionResult FromOptions(IEnumerable<CompletionOption> options) => new(options.ToArray(), null);

    public static CompletionResult FromHint(string hint) => new(Array.Empty<CompletionOption>(), hint);

    private static CompletionOption[] ConvertOptions(IEnumerable<string> stringOpts)
    {
        return stringOpts.Select(c => new CompletionOption(c)).ToArray();
    }
}

/// <summary>
/// Possible option to tab-complete in a <see cref="CompletionResult"/>.
/// </summary>
public record struct CompletionOption(string Value, string? Hint = null, CompletionOptionFlags Flags = default)
{
    /// <summary>
    /// The value that will be filled in if completed.
    /// </summary>
    public string Value { get; set; } = Value;

    /// <summary>
    /// Additional hint value that is shown to users, but not included in the completed value.
    /// </summary>
    public string? Hint { get; set; } = Hint;

    /// <summary>
    /// Flags that control how this completion is used.
    /// </summary>
    public CompletionOptionFlags Flags { get; set; } = Flags;
}

/// <summary>
/// Flag options for <see cref="CompletionOption"/>.
/// </summary>
[Flags]
public enum CompletionOptionFlags
{
    /// <summary>
    /// The completion is "partial", it does complete the whole argument.
    /// Therefore, tab completing it should keep the cursor on the current argument
    /// (instead of adding a space to go to the next one).
    /// </summary>
    PartialCompletion = 1 << 0,
}
