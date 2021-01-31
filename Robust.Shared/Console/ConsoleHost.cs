using System;
using System.Collections.Generic;
using Robust.Shared.Enums;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.IoC.Exceptions;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Reflection;

namespace Robust.Shared.Console
{
    /// <inheritdoc />
    public abstract class ConsoleHost : IConsoleHost
    {
        protected const string SawmillName = "con";

        [Dependency] protected readonly ILogManager LogManager = default!;
        [Dependency] protected readonly IReflectionManager ReflectionManager = default!;
        [Dependency] protected readonly INetManager NetManager = default!;

        protected readonly Dictionary<string, IConsoleCommand> AvailableCommands = new();

        /// <inheritdoc />
        public bool IsServer => NetManager.IsServer;

        /// <inheritdoc />
        public IConsoleShell LocalShell { get; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, IConsoleCommand> RegisteredCommands => AvailableCommands;

        protected ConsoleHost()
        {
            LocalShell = new ConsoleShell(this, null);
        }

        /// <inheritdoc />
        public event EventHandler? ClearText;

        /// <inheritdoc />
        public void ReloadCommands()
        {
            // search for all client commands in all assemblies, and register them
            AvailableCommands.Clear();
            foreach (var type in ReflectionManager.GetAllChildren<IConsoleCommand>())
            {
                var instance = (IConsoleCommand) Activator.CreateInstance(type, null)!;
                if (RegisteredCommands.TryGetValue(instance.Command, out var duplicate))
                {
                    throw new InvalidImplementationException(instance.GetType(), typeof(IConsoleCommand),
                        $"Command name already registered: {instance.Command}, previous: {duplicate.GetType()}");
                }

                AvailableCommands[instance.Command] = instance;
            }
        }

        /// <inheritdoc />
        public void RegisterCommand(string command, string description, string help, ConCommandCallback callback)
        {
            if (AvailableCommands.ContainsKey(command))
                throw new InvalidOperationException($"Command already registered: {command}");

            var newCmd = new RegisteredCommand(command, description, help, callback);
            AvailableCommands.Add(command, newCmd);
        }

        //TODO: Pull up
        public abstract void ExecuteCommand(ICommonSession? session, string command);

        //TODO: server -> client forwarding, making the system asymmetrical
        public abstract void RemoteExecuteCommand(ICommonSession? session, string command);

        //TODO: IConsoleOutput for [e#1225]
        public abstract void WriteLine(ICommonSession? session, string text);
        public abstract void WriteLine(ICommonSession? session, string text, Color color);

        /// <inheritdoc />
        public void ClearLocalConsole()
        {
            ClearText?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc />
        public IConsoleShell GetSessionShell(ICommonSession session)
        {
            if (!IsServer)
                return LocalShell;

            if (session.Status >= SessionStatus.Disconnected)
                throw new InvalidOperationException("Tried to get the session shell of a disconnected peer.");

            return new ConsoleShell(this, session);
        }

        /// <inheritdoc />
        public void ExecuteCommand(string command)
        {
            ExecuteCommand(null, command);
        }

        /// <summary>
        /// A console command that was registered inline through <see cref="IConsoleHost"/>.
        /// </summary>
        [Reflect(false)]
        private class RegisteredCommand : IConsoleCommand
        {
            private readonly ConCommandCallback _callback;

            /// <inheritdoc />
            public string Command { get; }

            /// <inheritdoc />
            public string Description { get; }

            /// <inheritdoc />
            public string Help { get; }

            /// <summary>
            /// Constructs a new instance of <see cref="RegisteredCommand"/>.
            /// </summary>
            /// <param name="command">Name of the command.</param>
            /// <param name="description">Short description of the command.</param>
            /// <param name="help">Extended description for the command.</param>
            /// <param name="callback">Callback function that is ran when the command is executed.</param>
            public RegisteredCommand(string command, string description, string help, ConCommandCallback callback)
            {
                Command = command;
                Description = description;
                Help = help;
                _callback = callback;
            }

            /// <inheritdoc />
            public void Execute(IConsoleShell shell, string argStr, string[] args)
            {
                _callback(shell, argStr, args);
            }
        }
    }
}
