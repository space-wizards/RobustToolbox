using System;
using System.Collections.Generic;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.IoC.Exceptions;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

namespace Robust.Shared.Console
{
    public class AddStringArgs : EventArgs
    {
        public string Text { get; }

        public bool Local { get; }

        public bool Error { get; }

        public AddStringArgs(string text, bool local, bool error)
        {
            Text = text;
            Local = local;
            Error = error;
        }
    }

    /// <inheritdoc />
    public class ConsoleHost : IConsoleHost
    {
        protected const string SawmillName = "con";

        [Dependency] protected readonly ILogManager LogManager = default!;
        [Dependency] protected readonly IReflectionManager ReflectionManager = default!;

        protected readonly Dictionary<string, IConsoleCommand> AvailableCommands = new();

        /// <inheritdoc />
        public virtual bool IsServer => false;

        /// <inheritdoc />
        public IConsoleShell LocalShell { get; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, IConsoleCommand> RegisteredCommands => AvailableCommands;

        public ConsoleHost()
        {
            LocalShell = new ConsoleShell(this, null);
        }

        /// <inheritdoc />
        public event EventHandler<AddStringArgs>? AddString;

        /// <inheritdoc />
        public event EventHandler? ClearText;

        /// <inheritdoc />
        public void LoadConsoleCommands()
        {
            // search for all client commands in all assemblies, and register them
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

        /// <inheritdoc />
        public virtual void ExecuteCommand(ICommonSession? session, string command)
        {
            var shell = new ConsoleShell(this, session);

            try
            {
                var args = new List<string>();
                CommandParsing.ParseArguments(command, args);

                // missing cmdName
                if (args.Count == 0)
                    return;

                var cmdName = args[0];

                if (AvailableCommands.TryGetValue(cmdName, out var conCmd)) // command registered
                {
                    args.RemoveAt(0);
                    conCmd.Execute(shell, command, args.ToArray());
                }
                else
                {
                    shell.WriteError($"Unknown command: '{cmdName}'");
                }
            }
            catch (Exception e)
            {
                LogManager.GetSawmill(SawmillName).Warning($"ExecuteError - {command}:\n{e}");
                shell.WriteError($"There was an error while executing the command: {e}");
            }
        }

        public virtual void RemoteExecuteCommand(ICommonSession? session, string command)
        {
        }

        //TODO: IConsoleOutput for [e#1225]
        public virtual void WriteLine(ICommonSession? session, string text)
        {
            OutputText(text, true, false);
        }

        public virtual void WriteError(ICommonSession? session, string text)
        {
            OutputText(text, true, true);
        }

        /// <inheritdoc />
        public void ClearLocalConsole()
        {
            ClearText?.Invoke(this, EventArgs.Empty);
        }

        private protected void OutputText(string text, bool local, bool error)
        {
            AddString?.Invoke(this, new AddStringArgs(text, local, error));
        }

        /// <inheritdoc />
        public virtual IConsoleShell GetSessionShell(ICommonSession session)
        {
            return LocalShell;
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
