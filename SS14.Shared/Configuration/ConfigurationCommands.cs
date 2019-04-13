using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using SS14.Shared.Console;

namespace SS14.Shared.Configuration
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    internal abstract class SharedCVarCommand : ICommand
    {
        public string Command => "cvar";
        public string Description => "Gets or sets a CVar.";

        public string Help => @"cvar <name> [value]
If a value is passed, the value is parsed and stored as the new value of the CVar.
If not, the current value of the CVar is displayed.";

        protected static object ParseObject(Type type, string input)
        {
            if (type == typeof(bool))
            {
                return bool.Parse(input);
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
