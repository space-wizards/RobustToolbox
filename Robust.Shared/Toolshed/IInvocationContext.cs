using System.Collections.Generic;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed;

/// <summary>
///     A context in which Toolshed commands can be run using <see cref="M:Robust.Shared.Toolshed.ToolshedManager.InvokeCommand(Robust.Shared.Toolshed.IInvocationContext,System.String,System.Object,System.Object@)"/>.
/// </summary>
public interface IInvocationContext
{
    /// <summary>
    ///     Test if the given command spec can be invoked in this context.
    /// </summary>
    /// <param name="command">Command to test.</param>
    /// <param name="error">An error to report, if any.</param>
    /// <returns>Whether or not the given command can be invoked</returns>
    /// <remarks>
    ///     THIS IS A SECURITY BOUNDARY.
    ///     If you want to avoid players being able to just reboot your server, you should probably implement this!
    ///     The default implementation defers to the active permission controller.
    /// </remarks>
    public bool CheckInvokable(CommandSpec command, out IConError? error)
    {
        if (Toolshed.ActivePermissionController is { } controller)
            return controller.CheckInvokable(command, Session, out error);

        error = null;
        return true;
    }

    ToolshedEnvironment Environment { get; }

    /// <summary>
    ///     The session for the <see cref="User"/>, if any currently exists.
    /// </summary>
    ICommonSession? Session { get; }

    /// <summary>
    ///     The session this context is for, if any.
    /// </summary>
    NetUserId? User { get; }

    ToolshedManager Toolshed { get; }

    /// <summary>
    ///     Writes a line to this context's output.
    /// </summary>
    /// <param name="line">The text to print.</param>
    /// <remarks>
    ///     This can be stubbed safely, there's no requirement that the side effects of this function be observable.
    /// </remarks>
    public void WriteLine(string line);

    /// <summary>
    ///     Writes a formatted message to this context's output.
    /// </summary>
    /// <param name="line">The formatted message to print.</param>
    /// <remarks>
    ///     This can be stubbed safely, there's no requirement that the side effects of this function be observable.
    /// </remarks>
    public void WriteLine(FormattedMessage line)
    {
        // Cut markup for server.
        if (Session is null)
        {
            WriteLine(line.ToString());
            return;
        }

        WriteLine(line.ToMarkup());
    }

    /// <summary>
    ///     Writes the given markup to this context's output.
    /// </summary>
    /// <param name="markup">The markup to print.</param>
    /// <remarks>
    ///     This can be stubbed safely, there's no requirement that the side effects of this function be observable.
    /// </remarks>
    public void WriteMarkup(string markup)
    {
        WriteLine(FormattedMessage.FromMarkup(markup));
    }

    /// <summary>
    ///     Writes the given error to the context's output.
    /// </summary>
    /// <param name="error">The error to write out.</param>
    /// <remarks>
    ///     This can be stubbed safely, there's no requirement that the side effects of this function be observable.
    /// </remarks>
    public void WriteError(IConError error)
    {
        WriteLine(error.Describe());
    }

    /// <summary>
    ///     Reports the given error to the context.
    /// </summary>
    /// <param name="err">Error to report.</param>
    /// <remarks>
    ///     This may have arbitrary side effects. Usually, it'll push the error to some list you can retrieve with GetErrors().
    /// </remarks>
    public void ReportError(IConError err);

    /// <summary>
    ///     Gets the list of unobserved errors.
    /// </summary>
    /// <returns>An enumerable of console errors.</returns>
    /// <remarks>
    ///     This is not required to contain anything, may contain errors you did not ReportError(), and may not contain errors you did ReportError().
    /// </remarks>
    public IEnumerable<IConError> GetErrors();

    /// <summary>
    ///     Clears the list of unobserved errors.
    /// </summary>
    /// <remarks>
    ///     After calling this, assuming atomicity (no threads), GetErrors() MUST be empty in order for an IInvocationContext to be compliant.
    /// </remarks>
    public void ClearErrors();

    /// <summary>
    ///     The backing variable storage.
    /// </summary>
    /// <remarks>
    ///     You don't have to use this at all.
    /// </remarks>
    protected Dictionary<string, object?> Variables { get; }

    /// <summary>
    ///     Reads the given variable from the context.
    /// </summary>
    /// <param name="name">The name of the variable.</param>
    /// <returns>The contents of the variable.</returns>
    /// <remarks>
    ///     This may behave arbitrarily, but it's advised it behave somewhat sanely.
    /// </remarks>
    public virtual object? ReadVar(string name)
    {
        Variables.TryGetValue(name, out var res);
        return res;
    }

    /// <summary>
    ///     Writes the given variable to the context.
    /// </summary>
    /// <param name="name">The name of the variable.</param>
    /// <param name="value">The contents of the variable.</param>
    /// <remarks>
    ///     Writes may be ignored or manipulated.
    /// </remarks>
    public virtual void WriteVar(string name, object? value)
    {
        Variables[name] = value;
    }

    /// <summary>
    ///     Provides a list of all variables that have been written to at some point.
    /// </summary>
    /// <returns>List of all variables.</returns>
    public virtual IEnumerable<string> GetVars()
    {
        return Variables.Keys;
    }
}
