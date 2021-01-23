using System;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.Console.Commands
{
    [UsedImplicitly]
    internal sealed class CVarCommand : SharedCVarCommand, IClientCommand
    {
        public bool Execute(IClientConsoleShell shell, string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                shell.WriteLine("Must provide exactly one or two arguments.", Color.Red);
                return false;
            }

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            var name = args[0];

            if (name == "?")
            {
                var cvars = configManager.GetRegisteredCVars().OrderBy(c => c);
                shell.WriteLine(string.Join("\n", cvars));
                return false;
            }

            if (!configManager.IsCVarRegistered(name))
            {
                shell.WriteLine($"CVar '{name}' is not registered. Use 'cvar ?' to get a list of all registered CVars.", Color.Red);
                return false;
            }

            if (args.Length == 1)
            {
                // Read CVar
                var value = configManager.GetCVar<object>(name);
                shell.WriteLine(value.ToString() ?? "");
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

            return false;
        }
    }

    [UsedImplicitly]
    public class SaveConfig : IClientCommand
    {
        public string Command => "saveconfig";
        public string Description => "Saves the client configuration to the config file";
        public string Help => "saveconfig";

        public bool Execute(IClientConsoleShell shell, string[] args)
        {
            IoCManager.Resolve<IConfigurationManager>().SaveToFile();
            return false;
        }
    }

}
