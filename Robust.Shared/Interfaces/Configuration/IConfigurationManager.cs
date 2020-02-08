using System;
using System.Collections.Generic;
using Robust.Shared.Configuration;

namespace Robust.Shared.Interfaces.Configuration
{
    /// <summary>
    /// Stores and manages global configuration variables.
    /// </summary>
    public interface IConfigurationManager
    {
        /// <summary>
        /// Sets up the ConfigurationManager and loads a TOML configuration file.
        /// </summary>
        /// <param name="configFile">the full name of the config file.</param>
        void LoadFromFile(string configFile);

        /// <summary>
        ///     Specifies the location where the config file should be saved, without trying to load from it.
        /// </summary>
        void SetSaveFile(string configFile);

        /// <summary>
        /// Saves the configuration file to disk.
        /// </summary>
        void SaveToFile();

        /// <summary>
        /// Register a CVar with the system. This must be done before the CVar is accessed.
        /// </summary>
        /// <param name="name">The name of the CVar. This needs to contain only printable characters.
        /// Periods '.' are reserved. Everything before the last period is a nested table identifier,
        /// everything after is the CVar name in the TOML document.</param>
        /// <param name="defaultValue">The default Value of the CVar.</param>
        /// <param name="flags">Optional flags to change behavior of the CVar.</param>
        /// <param name="onValueChanged">Invoked whenever the CVar value changes.</param>
        void RegisterCVar<T>(string name, T defaultValue, CVar flags = CVar.NONE, Action<T> onValueChanged=null);

        /// <summary>
        /// Is the named CVar already registered?
        /// </summary>
        /// <param name="name">The name of the CVar.</param>
        /// <returns></returns>
        bool IsCVarRegistered(string name);

        /// <summary>
        /// Sets a CVars value.
        /// </summary>
        /// <param name="name">The name of the CVar.</param>
        /// <param name="value">The value to set.</param>
        void SetCVar(string name, object value);

        /// <summary>
        /// Get the value of a CVar.
        /// </summary>
        /// <typeparam name="T">The Type of the CVar value.</typeparam>
        /// <param name="name">The name of the CVar.</param>
        /// <returns></returns>
        T GetCVar<T>(string name);

        /// <summary>
        ///     Gets the type of a value stored in a CVar.
        /// </summary>
        /// <param name="name">The name of the CVar</param>
        Type GetCVarType(string name);

        void OverrideConVars(IReadOnlyCollection<(string key, string value)> cVars);
    }
}
