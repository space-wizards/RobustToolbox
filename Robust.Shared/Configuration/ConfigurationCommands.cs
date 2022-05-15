using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Shared.Configuration
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    internal sealed class CVarCommand : IConsoleCommand
    {
        public string Command => "cvar";
        public string Description => "Gets or sets a CVar.";

        public string Help => @"cvar <name> [value]
If a value is passed, the value is parsed and stored as the new value of the CVar.
If not, the current value of the CVar is displayed.
Use 'cvar ?' to get a list of all registered CVars.";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
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
                var cvars = configManager.GetRegisteredCVars().OrderBy(c => c);
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

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            var cfg = IoCManager.Resolve<IConfigurationManager>();
            if (args.Length == 1)
                return CompletionResult.FromOptions(cfg.GetRegisteredCVars().ToArray());

            var cvar = args[0];
            if (!cfg.IsCVarRegistered(cvar))
                return CompletionResult.Empty;

            var type = cfg.GetCVarType(cvar);
            return CompletionResult.FromHint($"<{type.Name}>");
        }

        private static object ParseObject(Type type, string input)
        {
            if (type == typeof(bool))
            {
                if(bool.TryParse(input, out var val))
                    return val;

                if (int.TryParse(input, out var intVal))
                {
                    if (intVal == 0) return false;
                    if (intVal == 1) return true;
                }

                throw new FormatException($"Could not parse bool value: {input}");
            }

            if (type == typeof(string))
            {
                return input;
            }

            if (type == typeof(int))
            {
                return int.Parse(input, CultureInfo.InvariantCulture);
            }

            if (type == typeof(float))
            {
                return float.Parse(input, CultureInfo.InvariantCulture);
            }

            throw new NotImplementedException();
        }
    }
}
