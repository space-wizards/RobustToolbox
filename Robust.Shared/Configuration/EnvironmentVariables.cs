using System;
using System.Collections;
using System.Collections.Generic;

namespace Robust.Shared.Configuration
{
    internal static class EnvironmentVariables
    {
        /// <summary>
        /// The environment variable for configuring CVar overrides. The value
        /// of the variable should be passed as key-value equalities separated by
        /// semicolons.
        /// </summary>
        public const string ConfigVarEnvironmentVariable = "ROBUST_CVARS";

        public const string SingleVarPrefix = "ROBUST_CVAR_";

        /// <summary>
        /// Get the CVar overrides defined in the relevant environment variable.
        /// </summary>
        internal static IEnumerable<(string, string)> GetEnvironmentCVars()
        {
            // Handle ROBUST_CVARS.
            var eVarString = Environment.GetEnvironmentVariable(ConfigVarEnvironmentVariable) ?? "";

            foreach (var cVarPair in eVarString.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var pairParts = cVarPair.Split('=', 2);
                yield return (pairParts[0], pairParts[1]);
            }

            // Handle ROBUST_CVAR_*

            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
            {
                var key = (string)entry.Key;
                var value = (string?)entry.Value;

                if (value == null)
                    continue;

                if (!key.StartsWith(SingleVarPrefix))
                    continue;

                var varName = key[SingleVarPrefix.Length..].Replace("__", ".");
                yield return (varName, value);
            }
        }
    }
}
