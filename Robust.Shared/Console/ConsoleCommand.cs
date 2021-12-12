using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Robust.Shared.Console
{
    public abstract class ConsoleCommand : IConsoleCommand
    {
        [Dependency]
        private readonly ILocalizationManager _loc = default!;

        /// <inheritdoc />
        public abstract string Command { get; }

        /// <inheritdoc />
        public string Description => _loc.GetCommandData(Command).Desc;

        /// <inheritdoc />
        public string Help => _loc.GetCommandData(Command).Help;

        /// <inheritdoc />
        public abstract void Execute( IConsoleShell shell, string argStr, string[] args );
    }
}
