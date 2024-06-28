using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.IoC.Exceptions;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Console
{
    /// <inheritdoc />
    public abstract class ConsoleHost : IConsoleHost
    {
        protected const string SawmillName = "con";

        [Dependency] protected readonly ILogManager LogManager = default!;
        [Dependency] private readonly IReflectionManager ReflectionManager = default!;
        [Dependency] protected readonly INetManager NetManager = default!;
        [Dependency] private readonly IDynamicTypeFactoryInternal _typeFactory = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] protected readonly ILocalizationManager LocalizationManager = default!;

        [ViewVariables] protected readonly Dictionary<string, IConsoleCommand> RegisteredCommands = new();
        [ViewVariables] private readonly HashSet<string> _autoRegisteredCommands = [];

        private bool _isInRegistrationRegion;

        private readonly CommandBuffer _commandBuffer = new CommandBuffer();

        // TODO add Initialize() method.
        protected ISawmill Sawmill => LogManager.GetSawmill(SawmillName);

        /// <inheritdoc />
        public bool IsServer { get; }

        /// <inheritdoc />
        public IConsoleShell LocalShell { get; }

        /// <inheritdoc />
        public virtual IReadOnlyDictionary<string, IConsoleCommand> AvailableCommands => RegisteredCommands;

        public abstract event ConAnyCommandCallback? AnyCommandExecuted;

        protected ConsoleHost(bool isServer)
        {
            IsServer = isServer;
            LocalShell = new ConsoleShell(this, null, true);
        }

        /// <inheritdoc />
        public event EventHandler? ClearText;

        /// <inheritdoc />
        public void LoadConsoleCommands()
        {
            // search for all client commands in all assemblies, and register them
            foreach (var type in ReflectionManager.GetAllChildren<IConsoleCommand>())
            {
                // This sucks but I can't come up with anything better
                // that won't just be 10x worse complexity for no gain.
                if (type.IsAssignableTo(typeof(IEntityConsoleCommand)))
                    continue;

                var instance = (IConsoleCommand)_typeFactory.CreateInstanceUnchecked(type, true);
                if (AvailableCommands.TryGetValue(instance.Command, out var duplicate))
                {
                    throw new InvalidImplementationException(instance.GetType(), typeof(IConsoleCommand),
                        $"Command name already registered: {instance.Command}, previous: {duplicate.GetType()}");
                }

                RegisteredCommands[instance.Command] = instance;
                _autoRegisteredCommands.Add(instance.Command);
            }
        }

        protected virtual void UpdateAvailableCommands()
        {
        }

        public void BeginRegistrationRegion()
        {
            if (_isInRegistrationRegion)
                throw new InvalidOperationException("Cannot enter registration region twice!");

            _isInRegistrationRegion = true;
        }

        public void EndRegistrationRegion()
        {
            if (!_isInRegistrationRegion)
                throw new InvalidOperationException("Was not in registration region.");

            _isInRegistrationRegion = false;
            UpdateAvailableCommands();
        }

        #region RegisterCommand
        public void RegisterCommand(
            string command,
            string description,
            string help,
            ConCommandCallback callback,
            bool requireServerOrSingleplayer = false)
        {
            if (RegisteredCommands.ContainsKey(command))
                throw new InvalidOperationException($"Command already registered: {command}");

            var newCmd = new RegisteredCommand(command, description, help, callback, requireServerOrSingleplayer);
            RegisterCommand(newCmd);
        }

        public void RegisterCommand(
            string command,
            string description,
            string help,
            ConCommandCallback callback,
            ConCommandCompletionCallback completionCallback,
            bool requireServerOrSingleplayer = false)
        {
            if (RegisteredCommands.ContainsKey(command))
                throw new InvalidOperationException($"Command already registered: {command}");

            var newCmd = new RegisteredCommand(command, description, help, callback, completionCallback, requireServerOrSingleplayer);
            RegisterCommand(newCmd);
        }

        public void RegisterCommand(
            string command,
            string description,
            string help,
            ConCommandCallback callback,
            ConCommandCompletionAsyncCallback completionCallback,
            bool requireServerOrSingleplayer = false)
        {
            if (RegisteredCommands.ContainsKey(command))
                throw new InvalidOperationException($"Command already registered: {command}");

            var newCmd = new RegisteredCommand(command, description, help, callback, completionCallback, requireServerOrSingleplayer);
            RegisterCommand(newCmd);
        }

        public void RegisterCommand(string command, ConCommandCallback callback,
            bool requireServerOrSingleplayer = false)
        {
            var description = LocalizationManager.TryGetString($"cmd-{command}-desc", out var desc) ? desc : "";
            var help = LocalizationManager.TryGetString($"cmd-{command}-help", out var val) ? val : "";
            RegisterCommand(command, description, help, callback, requireServerOrSingleplayer);
        }

        public void RegisterCommand(
            string command,
            ConCommandCallback callback,
            ConCommandCompletionCallback completionCallback,
            bool requireServerOrSingleplayer = false)
        {
            var description = LocalizationManager.TryGetString($"cmd-{command}-desc", out var desc) ? desc : "";
            var help = LocalizationManager.TryGetString($"cmd-{command}-help", out var val) ? val : "";
            RegisterCommand(command, description, help, callback, completionCallback, requireServerOrSingleplayer);
        }

        public void RegisterCommand(
            string command,
            ConCommandCallback callback,
            ConCommandCompletionAsyncCallback completionCallback,
            bool requireServerOrSingleplayer = false)
        {
            var description = LocalizationManager.TryGetString($"cmd-{command}-desc", out var desc) ? desc : "";
            var help = LocalizationManager.TryGetString($"cmd-{command}-help", out var val) ? val : "";
            RegisterCommand(command, description, help, callback, completionCallback, requireServerOrSingleplayer);
        }

        public void RegisterCommand(IConsoleCommand command)
        {
            RegisteredCommands.Add(command.Command, command);

            if (!_isInRegistrationRegion)
                UpdateAvailableCommands();
        }

        #endregion

        /// <inheritdoc />
        public void UnregisterCommand(string command)
        {
            if (!RegisteredCommands.TryGetValue(command, out var cmd))
                throw new KeyNotFoundException($"Command {command} is not registered.");

            if (_autoRegisteredCommands.Contains(command))
                throw new InvalidOperationException(
                    "You cannot unregister commands that have been registered automatically.");

            RegisteredCommands.Remove(command);

            if (!_isInRegistrationRegion)
                UpdateAvailableCommands();
        }

        //TODO: Pull up
        public abstract void ExecuteCommand(ICommonSession? session, string command);

        //TODO: server -> client forwarding, making the system asymmetrical
        public abstract void RemoteExecuteCommand(ICommonSession? session, string command);

        //TODO: IConsoleOutput for [e#1225]
        public abstract void WriteLine(ICommonSession? session, string text);
        public abstract void WriteLine(ICommonSession? session, FormattedMessage msg);

        public abstract void WriteError(ICommonSession? session, string text);

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

            return new ConsoleShell(this, session, false);
        }

        /// <inheritdoc />
        public void ExecuteCommand(string command)
        {
            ExecuteCommand(null, command);
        }

        /// <inheritdoc />
        public void AppendCommand(string command)
        {
            _commandBuffer.Append(command);
        }

        /// <inheritdoc />
        public void InsertCommand(string command)
        {
            _commandBuffer.Insert(command);
        }

        /// <inheritdoc />
        public void CommandBufferExecute()
        {
            _commandBuffer.Tick(_timing.TickRate);

            while (_commandBuffer.TryGetCommand(out var cmd))
            {
                try
                {
                    ExecuteCommand(cmd);
                }
                catch (Exception e)
                {
                    LocalShell.WriteError(e.Message);
                }
            }
        }

        /// <summary>
        /// A console command that was registered inline through <see cref="IConsoleHost"/>.
        /// </summary>
        [Reflect(false)]
        public sealed class RegisteredCommand : IConsoleCommand
        {
            public ConCommandCallback Callback { get; }
            public ConCommandCompletionCallback? CompletionCallback { get; }
            public ConCommandCompletionAsyncCallback? CompletionCallbackAsync { get; }

            /// <inheritdoc />
            public string Command { get; }

            /// <inheritdoc />
            public string Description { get; }

            /// <inheritdoc />
            public string Help { get; }

            /// <inheritdoc />
            public bool RequireServerOrSingleplayer { get; init; }

            /// <summary>
            /// Constructs a new instance of <see cref="RegisteredCommand"/>.
            /// </summary>
            /// <param name="command">Name of the command.</param>
            /// <param name="description">Short description of the command.</param>
            /// <param name="help">Extended description for the command.</param>
            /// <param name="callback">Callback function that is ran when the command is executed.</param>
            /// <param name="completionCallback">Callback function to get console completions.</param>
            public RegisteredCommand(
                string command,
                string description,
                string help,
                ConCommandCallback callback,
                bool requireServerOrSingleplayer = false)
            {
                Command = command;
                // Should these two be localized somehow?
                Description = description;
                Help = help;
                Callback = callback;
                RequireServerOrSingleplayer = requireServerOrSingleplayer;
            }

            /// <summary>
            /// Constructs a new instance of <see cref="RegisteredCommand"/>.
            /// </summary>
            /// <param name="command">Name of the command.</param>
            /// <param name="description">Short description of the command.</param>
            /// <param name="help">Extended description for the command.</param>
            /// <param name="callback">Callback function that is ran when the command is executed.</param>
            /// <param name="completionCallback">Callback function to get console completions.</param>
            public RegisteredCommand(
                string command,
                string description,
                string help,
                ConCommandCallback callback,
                ConCommandCompletionCallback completionCallback,
                bool requireServerOrSingleplayer = false)
                : this(command, description, help, callback, requireServerOrSingleplayer)
            {
                CompletionCallback = completionCallback;
            }

            /// <summary>
            /// Constructs a new instance of <see cref="RegisteredCommand"/>.
            /// </summary>
            /// <param name="command">Name of the command.</param>
            /// <param name="description">Short description of the command.</param>
            /// <param name="help">Extended description for the command.</param>
            /// <param name="callback">Callback function that is ran when the command is executed.</param>
            /// <param name="completionCallback">Asynchronous callback function to get console completions.</param>
            public RegisteredCommand(
                string command,
                string description,
                string help,
                ConCommandCallback callback,
                ConCommandCompletionAsyncCallback completionCallback,
                bool requireServerOrSingleplayer = false)
                : this(command, description, help, callback, requireServerOrSingleplayer)
            {
                CompletionCallbackAsync = completionCallback;
            }


            /// <inheritdoc />
            public void Execute(IConsoleShell shell, string argStr, string[] args)
            {
                Callback(shell, argStr, args);
            }

            public ValueTask<CompletionResult> GetCompletionAsync(
                IConsoleShell shell,
                string[] args,
                string argStr,
                CancellationToken cancel)
            {
                if (CompletionCallbackAsync != null)
                    return CompletionCallbackAsync(shell, args, argStr);

                if (CompletionCallback != null)
                    return ValueTask.FromResult(CompletionCallback(shell, args));

                return ValueTask.FromResult(CompletionResult.Empty);
            }

            public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
            {
                return CompletionCallback?.Invoke(shell, args) ?? CompletionResult.Empty;
            }
        }
    }
}
