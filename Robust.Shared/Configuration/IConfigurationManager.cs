using System;
using System.Collections.Generic;
using System.IO;
using Robust.Shared.Timing;

namespace Robust.Shared.Configuration
{
    /// <summary>
    /// Additional information about a CVar change.
    /// </summary>
    public readonly struct CVarChangeInfo
    {
        /// <summary>
        /// The name of the cvar that was changed.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The tick this CVar changed at.
        /// </summary>
        /// <remarks>
        /// Nominally this should always be the current tick,
        /// however due to poor network conditions it is possible for CVars to get applied late.
        /// In this case, this is effectively "this is the tick it was SUPPOSED to have been applied at".
        /// </remarks>
        public readonly GameTick TickChanged;

        /// <summary>
        /// The new value.
        /// </summary>
        public readonly object NewValue;

        /// <summary>
        /// The previous value.
        /// </summary>
        public readonly object OldValue;

        internal CVarChangeInfo(string name, GameTick tickChanged, object newValue, object oldValue)
        {
            Name = name;
            TickChanged = tickChanged;
            NewValue = newValue;
            OldValue = oldValue;
        }
    }

    public delegate void CVarChanged<in T>(T newValue, in CVarChangeInfo info);

    /// <summary>
    /// Stores and manages global configuration variables.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Accessing (getting/setting) main CVars is thread safe.
    /// Note that value-changed callbacks are ran synchronously from the thread using <see cref="SetCVar"/>,
    /// so it is not recommended to modify CVars from other threads.
    /// </para>
    /// </remarks>
    public interface IConfigurationManager
    {
        /// <summary>
        /// Saves the configuration file to disk.
        /// </summary>
        void SaveToFile();

        /// <summary>
        /// Serializes a list of cvars to a toml.
        /// </summary>
        void SaveToTomlStream(Stream stream, IEnumerable<string> cvars);

        HashSet<string> LoadFromTomlStream(Stream stream);

        /// <summary>
        /// Load a TOML config file and use the CVar values specified as an <see cref="OverrideDefault"/>.
        /// </summary>
        /// <remarks>
        /// All CVars in the TOML file must be registered when this function is called.
        /// </remarks>
        /// <returns>A set of all CVars touched.</returns>
        HashSet<string> LoadDefaultsFromTomlStream(Stream stream);

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
        /// Get the CVar flags of a registered cvar.
        /// </summary>
        CVar GetCVarFlags(string name);

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
        /// <param name="force">If true, this will set the cvar even if it should not be settable (e.g., server
        /// authoritative cvars being set by clients).</param>
        void SetCVar(string name, object value, bool force = false);

        void SetCVar<T>(CVarDef<T> def, T value, bool force = false) where T : notnull;

        /// <summary>
        /// Change the default value for a CVar.
        /// This means the CVar value is changed *only* if it has not been changed by config file or <c>OverrideConVars</c>.
        /// </summary>
        /// <param name="name">The name of the CVar to change the default for.</param>
        /// <param name="value">The new default value of the CVar.</param>
        void OverrideDefault(string name, object value);

        /// <summary>
        /// Change the default value for a CVar.
        /// This means the CVar value is changed *only* if it has not been changed by config file or <c>OverrideConVars</c>.
        /// </summary>
        /// <param name="def">The definition of the CVar to change the default for.</param>
        /// <param name="value">The new default value of the CVar.</param>
        void OverrideDefault<T>(CVarDef<T> def, T value) where T : notnull;

        /// <summary>
        /// Get the value of a CVar.
        /// </summary>
        /// <param name="name">The name of the CVar.</param>
        /// <returns></returns>
        object GetCVar(string name);

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

        /// <summary>
        /// Listen for an event for if the config value changes.
        /// </summary>
        /// <remarks>
        /// This is an O(n) operation.
        /// </remarks>
        /// <param name="cVar">The CVar to listen for.</param>
        /// <param name="onValueChanged">The delegate to run when the value was changed.</param>
        /// <param name="invokeImmediately">
        /// Whether to run the callback immediately in this method. Can help reduce boilerplate
        /// </param>
        /// <typeparam name="T">The type of value contained in this CVar.</typeparam>
        /// <seealso cref="UnsubValueChanged{T}(Robust.Shared.Configuration.CVarDef{T},System.Action{T})"/>
        void OnValueChanged<T>(CVarDef<T> cVar, Action<T> onValueChanged, bool invokeImmediately = false)
            where T : notnull;

        /// <summary>
        /// Listen for an event for if the config value changes.
        /// </summary>
        /// <remarks>
        /// This is an O(n) operation.
        /// </remarks>
        /// <param name="name">The name of the CVar to listen for.</param>
        /// <param name="onValueChanged">The delegate to run when the value was changed.</param>
        /// <param name="invokeImmediately">
        /// Whether to run the callback immediately in this method. Can help reduce boilerplate
        /// </param>
        /// <typeparam name="T">The type of value contained in this CVar.</typeparam>
        /// <seealso cref="UnsubValueChanged{T}(string,System.Action{T})"/>
        void OnValueChanged<T>(string name, Action<T> onValueChanged, bool invokeImmediately = false)
            where T : notnull;

        /// <summary>
        /// Unsubscribe an event previously registered with <see cref="OnValueChanged{T}(Robust.Shared.Configuration.CVarDef{T},System.Action{T},bool)"/>.
        /// </summary>
        /// <remarks>
        /// This is an O(n) operation.
        /// </remarks>
        /// <param name="cVar">The CVar to unsubscribe from.</param>
        /// <param name="onValueChanged">The delegate to unsubscribe.</param>
        /// <typeparam name="T">The type of value contained in this CVar.</typeparam>
        void UnsubValueChanged<T>(CVarDef<T> cVar, Action<T> onValueChanged)
            where T : notnull;

        /// <summary>
        /// Unsubscribe an event previously registered with <see cref="OnValueChanged{T}(string,System.Action{T},bool)"/>.
        /// </summary>
        /// <remarks>
        /// This is an O(n) operation.
        /// </remarks>
        /// <param name="name">The name of the CVar to unsubscribe from.</param>
        /// <param name="onValueChanged">The delegate to unsubscribe.</param>
        /// <typeparam name="T">The type of value contained in this CVar.</typeparam>
        void UnsubValueChanged<T>(string name, Action<T> onValueChanged)
            where T : notnull;

        /// <summary>
        /// Listen for an event for if the config value changes.
        /// </summary>
        /// <remarks>
        /// This is an O(n) operation.
        /// </remarks>
        /// <param name="cVar">The CVar to listen for.</param>
        /// <param name="onValueChanged">The delegate to run when the value was changed.</param>
        /// <param name="invokeImmediately">
        /// Whether to run the callback immediately in this method. Can help reduce boilerplate
        /// </param>
        /// <typeparam name="T">The type of value contained in this CVar.</typeparam>
        /// <seealso cref="UnsubValueChanged{T}(Robust.Shared.Configuration.CVarDef{T},System.Action{T})"/>
        void OnValueChanged<T>(CVarDef<T> cVar, CVarChanged<T> onValueChanged, bool invokeImmediately = false)
            where T : notnull;

        /// <summary>
        /// Listen for an event for if the config value changes.
        /// </summary>
        /// <remarks>
        /// This is an O(n) operation.
        /// </remarks>
        /// <param name="name">The name of the CVar to listen for.</param>
        /// <param name="onValueChanged">The delegate to run when the value was changed.</param>
        /// <param name="invokeImmediately">
        /// Whether to run the callback immediately in this method. Can help reduce boilerplate
        /// </param>
        /// <typeparam name="T">The type of value contained in this CVar.</typeparam>
        /// <seealso cref="UnsubValueChanged{T}(string,System.Action{T})"/>
        void OnValueChanged<T>(string name, CVarChanged<T> onValueChanged, bool invokeImmediately = false)
            where T : notnull;

        /// <summary>
        /// Unsubscribe an event previously registered with <see cref="OnValueChanged{T}(Robust.Shared.Configuration.CVarDef{T},System.Action{T},bool)"/>.
        /// </summary>
        /// <remarks>
        /// This is an O(n) operation.
        /// </remarks>
        /// <param name="cVar">The CVar to unsubscribe from.</param>
        /// <param name="onValueChanged">The delegate to unsubscribe.</param>
        /// <typeparam name="T">The type of value contained in this CVar.</typeparam>
        void UnsubValueChanged<T>(CVarDef<T> cVar, CVarChanged<T> onValueChanged)
            where T : notnull;

        /// <summary>
        /// Unsubscribe an event previously registered with <see cref="OnValueChanged{T}(string,System.Action{T},bool)"/>.
        /// </summary>
        /// <remarks>
        /// This is an O(n) operation.
        /// </remarks>
        /// <param name="name">The name of the CVar to unsubscribe from.</param>
        /// <param name="onValueChanged">The delegate to unsubscribe.</param>
        /// <typeparam name="T">The type of value contained in this CVar.</typeparam>
        void UnsubValueChanged<T>(string name, CVarChanged<T> onValueChanged)
            where T : notnull;

        public event Action<CVarChangeInfo>? OnCVarValueChanged;
    }
}
