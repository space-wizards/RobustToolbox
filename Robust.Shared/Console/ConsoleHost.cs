using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.IoC.Exceptions;
using Robust.Shared.Maths;
using Robust.Shared.Players;

namespace Robust.Shared.Console
{
    public abstract class ConsoleHost : IConsoleHost
    {
        protected const string SawmillName = "con";

        [Dependency] protected readonly ILogManager LogManager = default!;
        [Dependency] protected readonly IReflectionManager ReflectionManager = default!;
        [Dependency] protected readonly INetManager NetManager = default!;

        protected readonly Dictionary<string, IConsoleCommand> AvailableCommands = new();

        public bool IsServer { get; }

        public IConsoleShell LocalShell { get; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, IConsoleCommand> RegisteredCommands => AvailableCommands;

        public ConsoleHost()
        {
            LocalShell = new ConsoleShell(this, null);
            IsServer = NetManager.IsServer;
        }

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

        public void RegisterCommand(string command, string description, string help, ConCommandCallback callback)
        {
            if (AvailableCommands.ContainsKey(command))
                throw new InvalidOperationException($"Command already registered: {command}");

            var newCmd = new RegisteredCommand(command, description, help, callback);
            AvailableCommands.Add(command, newCmd);
        }

        public abstract IConsoleShell GetSessionShell(ICommonSession session);
        public abstract void ExecuteCommand(string command);
        public abstract void ExecuteCommand(ICommonSession? session, string command);
        public abstract void RemoteExecuteCommand(ICommonSession? session, string command);
        public abstract void WriteLine(ICommonSession? session, string text);
        public abstract void WriteLine(ICommonSession? session, string text, Color color);
        public abstract void ClearLocalConsole();
    }
}
