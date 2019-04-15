using Robust.Client.Interfaces.Console;
using Robust.Shared.Configuration;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.Console.Commands
{
    internal sealed class CVarCommand : SharedCVarCommand, IConsoleCommand
    {
        public bool Execute(IDebugConsole console, params string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                console.AddLine("Must provide exactly one or two arguments.", Color.Red);
                return false;
            }

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            var name = args[0];

            if (!configManager.IsCVarRegistered(name))
            {
                console.AddLine($"CVar '{name}' is not registered.", Color.Red);
                return false;
            }

            if (args.Length == 1)
            {
                // Read CVar
                var value = configManager.GetCVar<object>(name);
                console.AddLine(value.ToString());
            }
            else
            {
                // Write CVar
                var value = args[1];
                var type = configManager.GetCVarType(name);
                var parsed = ParseObject(type, value);
                configManager.SetCVar(name, parsed);
            }

            return false;
        }
    }
}
