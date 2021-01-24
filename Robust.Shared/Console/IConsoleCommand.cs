namespace Robust.Shared.Console
{
    /// <summary>
    /// Basic abstract to handle console commands.
    /// Note that there is no Execute() function, this is due to chat & client commands needing a client,
    /// While client-side and server console commands don't.
    /// </summary>
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
        /// Executes the client command.
        /// </summary>
        /// <param name="shell">The console that executed this command.</param>
        /// <param name="argStr">Unparsed text of the complete command with arguments.</param>
        /// <param name="args">An array of all the parsed arguments.</param>
        void Execute(IConsoleShell shell, string argStr, string[] args);
    }
}
