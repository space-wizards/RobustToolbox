using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Robust.Shared.Console
{
    /// <summary>
    /// Basic interface to handle console commands. Any class implementing this will be
    /// registered with the console system through reflection.
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
    public interface IConsoleCommand
    {
        /// <summary>
        /// Name of the command.
        /// </summary>
        /// <value>
        /// A string as identifier for this command.
        /// </value>
        string Command { get; }

        /// <summary>
        /// Short description of the command.
        /// </summary>
        /// <value>
        /// String printed as short summary in the "help" command.
        /// </value>
        string Description { get; }

        /// <summary>
        /// Extended description for the command.
        /// </summary>
        /// <value>
        /// String printed as summary when "help Command" is used.
        /// </value>
        string Help { get; }

        /// <summary>
        /// If true, this command will be unavailable to clients while they are connected to a server. Has no effect on servers.
        /// </summary>
        bool RequireServerOrSingleplayer => false;

        /// <summary>
        /// Executes the client command.
        /// </summary>
        /// <param name="shell">The console that executed this command.</param>
        /// <param name="argStr">Unparsed text of the complete command with arguments.</param>
        /// <param name="args">An array of all the parsed arguments.</param>
        void Execute(IConsoleShell shell, string argStr, string[] args);

        /// <summary>
        /// Fetches completion results for a typing a command.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Refrain from doing simple <c>.StartsWith(</c> filtering based on the currently typing command.
        /// The client already does this filtering on its own,
        /// so doing it manually would reduce responsiveness thanks to network lag.
        /// It may however be desirable to do larger-scale filtering.
        /// For example when typing out a resource path you could manually filter level-by-level as it's being typed.
        /// </para>
        /// <para>
        /// Only arguments to the left of the cursor are passed.
        /// If the user puts their cursor in the middle of a line and starts typing, anything to the right is ignored.
        /// </para>
        /// </remarks>
        /// <param name="shell">The console that is typing this command.</param>
        /// <param name="args">The set of commands currently being typed.
        /// If the last parameter is an empty string, it basically represents that the user hit space after the previous term and should already get completion results,
        /// even if they haven't started typing the new argument yet.</param>
        /// <returns>The possible completion results presented to the user.</returns>
        /// <seealso cref="GetCompletionAsync"/>
        CompletionResult GetCompletion(IConsoleShell shell, string[] args) => CompletionResult.Empty;

        /// <summary>
        /// Fetches completion results for typing a command, async variant. See <see cref="GetCompletion"/> for details.
        /// </summary>
        /// <remarks>
        /// If this method is implemented, <see cref="GetCompletion"/> will not be automatically called.
        /// </remarks>
        ValueTask<CompletionResult> GetCompletionAsync(IConsoleShell shell, string[] args, string argStr, CancellationToken cancel)
        {
            return ValueTask.FromResult(GetCompletion(shell, args));
        }
    }

    /// <summary>
    /// Special marker interface used to indicate "entity" commands.
    /// See <see cref="LocalizedEntityCommands"/> for an overview.
    /// </summary>
    /// <seealso cref="EntityConsoleHost"/>
    internal interface IEntityConsoleCommand : IConsoleCommand;
}
