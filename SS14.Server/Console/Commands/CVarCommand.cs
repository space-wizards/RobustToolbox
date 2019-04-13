using SS14.Server.Interfaces.Console;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Configuration;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.IoC;

namespace SS14.Server.Console.Commands
{
    internal sealed class CVarCommand : SharedCVarCommand, IClientCommand
    {
        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                shell.SendText(player, "Must provide exactly one or two arguments.");
                return;
            }

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            var name = args[0];

            if (!configManager.IsCVarRegistered(name))
            {
                shell.SendText(player, $"CVar '{name}' is not registered.");
                return;
            }

            if (args.Length == 1)
            {
                // Read CVar
                var value = configManager.GetCVar<object>(name);
                shell.SendText(player, value.ToString());
            }
            else
            {
                // Write CVar
                var value = args[1];
                var type = configManager.GetCVarType(name);
                var parsed = ParseObject(type, value);
                configManager.SetCVar(name, parsed);
            }
        }
    }

}
