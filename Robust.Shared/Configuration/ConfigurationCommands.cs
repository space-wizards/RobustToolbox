using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Robust.Shared.Console;

namespace Robust.Shared.Configuration
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    internal abstract class SharedCVarCommand : IConsoleCommand
    {
        public string Command => "cvar";
        public string Description => "Gets or sets a CVar.";

        public string Help => @"cvar <name> [value]
If a value is passed, the value is parsed and stored as the new value of the CVar.
If not, the current value of the CVar is displayed.
Use 'cvar ?' to get a list of all registered CVars.";

        public abstract void Execute(IConsoleShell shell, string argStr, string[] args);

        protected static object ParseObject(Type type, string input)
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
