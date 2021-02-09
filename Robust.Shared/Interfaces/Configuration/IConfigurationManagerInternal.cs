using System.Collections.Generic;
using System.Reflection;
using Robust.Shared.Configuration;

namespace Robust.Shared.Interfaces.Configuration
{
    internal interface IConfigurationManagerInternal : IConfigurationManager
    {
        void OverrideConVars(IEnumerable<(string key, string value)> cVars);
        void LoadCVarsFromAssembly(Assembly assembly);

        void Initialize(bool isServer);

        /// <summary>
        /// Sets up the ConfigurationManager and loads a TOML configuration file.
        /// </summary>
        /// <param name="configFile">the full name of the config file.</param>
        void LoadFromFile(string configFile);

        /// <summary>
        ///     Specifies the location where the config file should be saved, without trying to load from it.
        /// </summary>
        void SetSaveFile(string configFile);
    }
}
