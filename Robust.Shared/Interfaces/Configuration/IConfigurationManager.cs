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
        void RegisterCVar<T>(string name, T defaultValue, CVar flags = CVar.NONE, Action<T>? onValueChanged = null)
            where T : notnull;

        /// <summary>
        /// Is the named CVar already registered?
        /// </summary>
        /// <param name="name">The name of the CVar.</param>
        /// <returns></returns>
        bool IsCVarRegistered(string name);

        /// <summary>
        /// Gets a list of all registered cvars
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetRegisteredCVars();

        /// <summary>
        /// Sets a CVars value.
        /// </summary>
        /// <param name="name">The name of the CVar.</param>
        /// <param name="value">The value to set.</param>
        void SetCVar(string name, object value);

        void SetCVar<T>(CVarDef<T> def, T value) where T : notnull;

        /// <summary>
        /// Get the value of a CVar.
        /// </summary>
        /// <typeparam name="T">The Type of the CVar value.</typeparam>
        /// <param name="name">The name of the CVar.</param>
        /// <returns></returns>
        T GetCVar<T>(string name);

        T GetCVar<T>(CVarDef<T> def) where T : notnull;

        /// <summary>
        ///     Gets the type of a value stored in a CVar.
        /// </summary>
        /// <param name="name">The name of the CVar</param>
        Type GetCVarType(string name);

        void OnValueChanged<T>(CVarDef<T> cVar, Action<T> onValueChanged, bool invokeImmediately = false)
            where T : notnull;

        void OnValueChanged<T>(string name, Action<T> onValueChanged, bool invokeImmediately = false)
            where T : notnull;
    }
}
