using System.Diagnostics;
using System.Text;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.Errors;

// TODO TOOLSHED Localize Errors
// Requires reworking the IConError interface to take in an ILocalizationManager

// TODO TOOLSHED Rework IConError
// A bunch of the errors are structs, but they get boxed anyways.  So might as well make them all inherit from a base
// class, so that we don't need to constantly re-define the properties.

/// <summary>
///     A Toolshed-oriented representation of an error.
///     Contains metadata about where in an executed command it occurred, and supports formatting.
///     <code>
///     > entities runverbas self "yeet"
///     entities runverbas self "yeet"
///                        ^^^^^
///     You must be logged in with a client to use this, the server console isn't workable.
///     </code>
/// </summary>
public interface IConError
{
    /// <summary>
    ///     Returns a user friendly description of the error.
    /// </summary>
    /// <remarks>
    ///     This calls <see cref="M:Robust.Shared.Toolshed.Errors.IConError.DescribeInner"/> for the actual description by default.
    ///     If you fully override this, you should provide your own context provider, as the default implementation includes where in the expression the error occurred.
    /// </remarks>
    public FormattedMessage Describe()
    {
        var msg = new FormattedMessage();
        if (Expression is { } expr && IssueSpan is { } span)
        {
            msg.AddMessage(ConHelpers.HighlightSpan(expr, span, Color.Red));
            msg.PushNewline();
            msg.AddMessage(ConHelpers.ArrowSpan(span));
            msg.PushNewline();
        }
        msg.AddMessage(DescribeInner());
#if TOOLS
        if (Trace is not null)
        {
            msg.PushNewline();
            msg.AddText(Trace.ToString());
        }
#endif
        return msg;
    }

    /// <summary>
    ///     Describes the error, called by <see cref="M:Robust.Shared.Toolshed.Errors.IConError.Describe"/>'s default implementation.
    /// </summary>
    protected FormattedMessage DescribeInner();
    /// <summary>
    ///     The expression this error was raised in or on.
    /// </summary>
    public string? Expression { get; protected set; }
    /// <summary>
    ///     Where in the expression this error was raised.
    /// </summary>
    public Vector2i? IssueSpan { get; protected set; }
    /// <summary>
    ///     The stack trace for this error if any.
    /// </summary>
    /// <remarks>
    ///     This is not present in release builds.
    /// </remarks>
    public StackTrace? Trace { get; protected set; }

    /// <summary>
    ///     Attaches additional context to an error, namely where it occurred.
    /// </summary>
    /// <param name="expression">Expression the error occured in or on.</param>
    /// <param name="issueSpan">Where in the expression it occurred.</param>
    public void Contextualize(string expression, Vector2i issueSpan)
    {
        if (Expression is not null && IssueSpan is not null)
            return;

#if  TOOLS
        Trace = new StackTrace(skipFrames: 1);
#endif

        Expression = expression;
        IssueSpan = issueSpan;
    }
}

/// <summary>
///     Pile of helpers for console formatting.
/// </summary>
public static class ConHelpers
{
    /// <summary>
    ///     Highlights a section of the input a given color.
    /// </summary>
    /// <param name="input">Input text.</param>
    /// <param name="span">Span to highlight.</param>
    /// <param name="color">Color to use.</param>
    /// <returns>A formatted message with highlighting applied.</returns>
    public static FormattedMessage HighlightSpan(string input, Vector2i span, Color color)
    {
        var msg = FormattedMessage.FromUnformatted(input[..span.X]);
        msg.PushColor(color);
        if (span.Y >= input.Length)
        {
            msg.AddText(input[span.X..]);
            msg.Pop();
            return msg;
        }

        msg.AddText(input[span.X..span.Y]);
        msg.Pop();
        msg.AddText(input[span.Y..]);
        return msg;
    }

    /// <summary>
    ///     Creates a string with up arrows (<c>^</c>) under the given span.
    /// </summary>
    /// <param name="span">Span to underline.</param>
    /// <returns>A string of whitespace with (<c>^</c>) under the given span.</returns>
    public static FormattedMessage ArrowSpan(Vector2i span)
    {
        var builder = new StringBuilder();
        builder.Append(' ', span.X);
        builder.Append('^', span.Y - span.X);
        return FormattedMessage.FromUnformatted(builder.ToString());
    }
}

public abstract class ConError : IConError
{
    public abstract FormattedMessage DescribeInner();

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
