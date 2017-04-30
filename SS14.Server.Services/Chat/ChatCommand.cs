using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Commands;
using System.Collections.Generic;

// Chat commands, stuff you enter by prepending a /.
namespace SS14.Server.Services.Chat.Commands
{
    public abstract class ChatCommand : IClientCommand
    {
        /// <summary>
        /// Name of the command.
        /// </summary>
        /// <value>
        /// A string as indentifier for this command.
        /// </value>
        public abstract string Command { get; }

        /// <summary>
        /// Short description of the command.
        /// </summary>
        /// <value>
        /// String printed as short summary in the "help" command.
        /// </value>
        public abstract string Description { get; }

        /// <summary>
        /// Extended description for the command.
        /// </summary>
        /// <value>
        /// String printed as summary when "help Command" is used.
        /// </value>
        public abstract string Help { get; }

        /// <summary>
        /// Runs the command.
        /// </summary>
        /// <param name="Client">Client executing this command.</param>
        /// <param name="args">Additional arguments to pass to the command.</param>
        public abstract void Execute(IClient client, params string[] args);

        public virtual void Register(Dictionary<string, IClientCommand> commands)
        {
            commands.Add(Command, this);
        }
    }
}
