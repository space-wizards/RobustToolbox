using System;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;

namespace Robust.Server.Console.Commands
{
    [UsedImplicitly]
    internal sealed class CVarCommand : SharedCVarCommand, IServerCommand
    {
        public void Execute(IServerConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                shell.WriteLine("Must provide exactly one or two arguments.");
                return;
            }

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            var name = args[0];

            if (name == "?")
            {
                var cvars = configManager.GetRegisteredCVars();
                shell.WriteLine(string.Join("\n", cvars));
                return;
            }

            if (!configManager.IsCVarRegistered(name))
            {
                shell.WriteLine($"CVar '{name}' is not registered. Use 'cvar ?' to get a list of all registered CVars.");
                return;
            }

            if (args.Length == 1)
            {
                // Read CVar
                var value = configManager.GetCVar<object>(name);
                shell.WriteLine(value.ToString()!);
            }
            else
            {
                // Write CVar
                var value = args[1];
                var type = configManager.GetCVarType(name);
                try
                {
                    var parsed = ParseObject(type, value);
                    configManager.SetCVar(name, parsed);
                }
                catch (FormatException)
                {
                    shell.WriteLine($"Input value is in incorrect format for type {type}");
                }
            }
        }
    }

}
