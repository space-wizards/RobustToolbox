using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Nett;
using Robust.Shared.Collections;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.Configuration
{
    /// <summary>
    ///     Stores and manages global configuration variables.
    /// </summary>
    [Virtual]
    internal class ConfigurationManager : IConfigurationManagerInternal
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private const char TABLE_DELIMITER = '.';
        protected readonly Dictionary<string, ConfigVar> _configVars = new();
        private string? _configFile;
        protected bool _isServer;

        protected readonly ReaderWriterLockSlim Lock = new();

        /// <summary>
        ///     Constructs a new ConfigurationManager.
        /// </summary>
        public ConfigurationManager()
        {
        }

        public void Initialize(bool isServer)
        {
            _isServer = isServer;
        }

        public virtual void Shutdown()
        {
            using var _ = Lock.WriteGuard();

            _configVars.Clear();
            _configFile = null;
        }

        /// <inheritdoc />
        public void LoadFromFile(string configFile)
        {
            try
            {
                var tblRoot = Toml.ReadFile(configFile);

                var callbackEvents = new ValueList<ValueChangedInvoke>();

                // Ensure callbacks are raised OUTSIDE the write lock.
                using (Lock.WriteGuard())
                {
                    ProcessTomlObject(tblRoot, ref callbackEvents);
                }

                foreach (var callback in callbackEvents)
                {
                    InvokeValueChanged(callback);
                }

                _configFile = configFile;
                Logger.InfoS("cfg", $"Configuration Loaded from '{Path.GetFullPath(configFile)}'");
            }
            catch (Exception e)
            {
                Logger.WarningS("cfg", "Unable to load configuration file:\n{0}", e);
            }
        }

        public void SetSaveFile(string configFile)
        {
            _configFile = configFile;
        }

        /// <summary>
        /// A recursive function that walks over the config tree, transforming all key nodes into CVars.
        /// </summary>
        /// <param name="obj">The root table of the TOML document.</param>
        /// <param name="changedInvokes">List of CVars that will need to have their InvokeValueChanged ran.</param>
        /// <param name="tablePath">For internal use only, the current path to the node.</param>
        private void ProcessTomlObject(
            TomlObject obj,
            ref ValueList<ValueChangedInvoke> changedInvokes,
            string tablePath = "")
        {
            if (obj is TomlTable table) // this is a table
            {
                foreach (var kvTml in table)
                {
                    string newPath;

                    if ((kvTml.Value is TomlTable))
                        newPath = tablePath + kvTml.Key + TABLE_DELIMITER;
                    else
                        newPath = tablePath + kvTml.Key;

                    ProcessTomlObject(kvTml.Value, ref changedInvokes, newPath);
                }
            }
            else // this is a key, add CVar
            {
                // if the CVar has already been registered
                var tomlValue = TypeConvert(obj);
                if (_configVars.TryGetValue(tablePath, out var cfgVar))
                {
                    // overwrite the value with the saved one
                    cfgVar.Value = tomlValue;
                    if (SetupInvokeValueChanged(cfgVar, tomlValue) is { } invoke)
                        changedInvokes.Add(invoke);
                }
                else
                {
                    //or add another unregistered CVar
                    //Note: the defaultValue is arbitrarily 0, it will get overwritten when the cvar is registered.
                    cfgVar = new ConfigVar(tablePath, 0, CVar.NONE) { Value = tomlValue };
                    _configVars.Add(tablePath, cfgVar);
                }

                cfgVar.ConfigModified = true;
            }
        }

        /// <inheritdoc />
        public void SaveToFile()
        {
            if (_configFile == null)
            {
                Logger.WarningS("cfg", "Cannot save the config file, because one was never loaded.");
                return;
            }

            try
            {
                var tblRoot = Toml.Create();

                using (Lock.ReadGuard())
                {
                    foreach (var (name, cVar) in _configVars)
                    {
                        var value = cVar.Value;
                        if (value == null && cVar.Registered)
                        {
                            value = cVar.DefaultValue;
                        }

                        if (value == null)
                        {
                            Logger.ErrorS("cfg",
                                $"CVar {name} has no value or default value, was the default value registered as null?");
                            continue;
                        }

                        // Don't write if Archive flag is not set.
                        // Don't write if the cVar is the default value.
                        if (!cVar.ConfigModified &&
                            (cVar.Flags & CVar.ARCHIVE) == 0 || value.Equals(cVar.DefaultValue))
                        {
                            continue;
                        }

                        var keyIndex = name.LastIndexOf(TABLE_DELIMITER);
                        var tblPath = name.Substring(0, keyIndex).Split(TABLE_DELIMITER);
                        var keyName = name.Substring(keyIndex + 1);

                        // locate the Table in the config tree
                        var table = tblRoot;
                        foreach (var curTblName in tblPath)
                        {
                            if (!table.TryGetValue(curTblName, out TomlObject tblObject))
                            {
                                tblObject = table.Add(curTblName, new Dictionary<string, TomlObject>()).Added;
                            }

                            table = tblObject as TomlTable ?? throw new InvalidConfigurationException(
                                $"[CFG] Object {curTblName} is being used like a table, but it is a {tblObject}. Are your CVar names formed properly?");
                        }

                        //runtime unboxing, either this or generic hell... ¯\_(ツ)_/¯
                        switch (value)
                        {
                            case Enum val:
                                table.Add(keyName, (int)(object)val); // asserts Enum value != (ulong || long)
                                break;
                            case int val:
                                table.Add(keyName, val);
                                break;
                            case long val:
                                table.Add(keyName, val);
                                break;
                            case bool val:
                                table.Add(keyName, val);
                                break;
                            case string val:
                                table.Add(keyName, val);
                                break;
                            case float val:
                                table.Add(keyName, val);
                                break;
                            case double val:
                                table.Add(keyName, val);
                                break;
                            default:
                                Logger.WarningS("cfg", $"Cannot serialize '{name}', unsupported type.");
                                break;
                        }
                    }
                }

                Toml.WriteFile(tblRoot, _configFile);
                Logger.InfoS("cfg", $"config saved to '{_configFile}'.");
            }
            catch (Exception e)
            {
                Logger.WarningS("cfg", $"Cannot save the config file '{_configFile}'.\n {e}");
            }
        }

        public void RegisterCVar<T>(string name, T defaultValue, CVar flags = CVar.NONE,
            Action<T>? onValueChanged = null)
            where T : notnull
        {
            RegisterCVar(name, typeof(T), defaultValue, flags);

            if (onValueChanged != null)
                OnValueChanged(name, onValueChanged);
        }

        private void RegisterCVar(string name, Type type, object defaultValue, CVar flags)
        {
            DebugTools.Assert(!type.IsEnum || type.GetEnumUnderlyingType() == typeof(int),
                $"{name}: Enum cvars must have int as underlying type.");

            var only = _isServer ? CVar.CLIENTONLY : CVar.SERVERONLY;

            if ((flags & only) != 0)
            {
                // Ignored on this side.
                return;
            }

            using var _ = Lock.WriteGuard();

            if (_configVars.TryGetValue(name, out var cVar))
            {
                if (cVar.Registered)
                    Logger.ErrorS("cfg", $"The variable '{name}' has already been registered.");

                cVar.DefaultValue = defaultValue;
                cVar.Flags = flags;
                cVar.Registered = true;

                if (cVar.OverrideValue != null)
                {
                    cVar.OverrideValueParsed = ParseOverrideValue(cVar.OverrideValue, type);
                }

                return;
            }

            _configVars.Add(name, new ConfigVar(name, defaultValue, flags)
            {
                Registered = true
            });
        }

        public void OnValueChanged<T>(CVarDef<T> cVar, Action<T> onValueChanged, bool invokeImmediately = false)
            where T : notnull
        {
            OnValueChanged(cVar.Name, onValueChanged, invokeImmediately);
        }

        public void OnValueChanged<T>(string name, Action<T> onValueChanged, bool invokeImmediately = false)
            where T : notnull
        {
            using (Lock.WriteGuard())
            {
                var reg = _configVars[name];

                reg.ValueChanged.AddInPlace(
                    (object value, in CVarChangeInfo _) => onValueChanged((T)value),
                    onValueChanged);
            }

            if (invokeImmediately)
            {
                onValueChanged(GetCVar<T>(name));
            }
        }

        public void UnsubValueChanged<T>(CVarDef<T> cVar, Action<T> onValueChanged) where T : notnull
        {
            UnsubValueChanged(cVar.Name, onValueChanged);
        }

        public void UnsubValueChanged<T>(string name, Action<T> onValueChanged) where T : notnull
        {
            using var _ = Lock.WriteGuard();

            var reg = _configVars[name];
            reg.ValueChanged.RemoveInPlace(onValueChanged);
        }

        public void OnValueChanged<T>(CVarDef<T> cVar, CVarChanged<T> onValueChanged, bool invokeImmediately = false)
            where T : notnull
        {
            OnValueChanged(cVar.Name, onValueChanged, invokeImmediately);
        }

        public void OnValueChanged<T>(string name, CVarChanged<T> onValueChanged, bool invokeImmediately = false)
            where T : notnull
        {
            using (Lock.WriteGuard())
            {
                var reg = _configVars[name];

                reg.ValueChanged.AddInPlace(
                    (object value, in CVarChangeInfo info) => onValueChanged((T)value, info),
                    onValueChanged);
            }

            if (invokeImmediately)
            {
                onValueChanged(GetCVar<T>(name), new CVarChangeInfo(_gameTiming.CurTick));
            }
        }

        public void UnsubValueChanged<T>(CVarDef<T> cVar, CVarChanged<T> onValueChanged) where T : notnull
        {
            UnsubValueChanged(cVar.Name, onValueChanged);
        }

        public void UnsubValueChanged<T>(string name, CVarChanged<T> onValueChanged) where T : notnull
        {
            using var _ = Lock.WriteGuard();

            var reg = _configVars[name];
            reg.ValueChanged.RemoveInPlace(onValueChanged);
        }

        public void LoadCVarsFromAssembly(Assembly assembly)
        {
            foreach (var defField in assembly
                         .GetTypes()
                         .Where(p => Attribute.IsDefined(p, typeof(CVarDefsAttribute)))
                         .SelectMany(p => p.GetFields(BindingFlags.Public | BindingFlags.Static)))
            {
                var fieldType = defField.FieldType;
                if (!fieldType.IsGenericType || fieldType.GetGenericTypeDefinition() != typeof(CVarDef<>))
                {
                    continue;
                }

                var type = fieldType.GetGenericArguments()[0];

                if (!defField.IsInitOnly)
                {
                    throw new InvalidOperationException(
                        $"Found CVarDef '{defField.Name}' on '{defField.DeclaringType?.FullName}' that is not readonly. Please mark it as readonly.");
                }

                var def = (CVarDef?)defField.GetValue(null);

                if (def == null)
                {
                    throw new InvalidOperationException(
                        $"CVarDef '{defField.Name}' on '{defField.DeclaringType?.FullName}' is null.");
                }

                RegisterCVar(def.Name, type, def.DefaultValue, def.Flags);
            }
        }

        /// <inheritdoc />
        public bool IsCVarRegistered(string name)
        {
            using var _ = Lock.ReadGuard();
            return _configVars.TryGetValue(name, out var cVar) && cVar.Registered;
        }

        public CVar GetCVarFlags(string name)
        {
            using var _ = Lock.ReadGuard();
            return _configVars[name].Flags;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetRegisteredCVars()
        {
            using var _ = Lock.ReadGuard();
            // Have to .ToArray() so the lock is held for the whole iteration operation.
            // This function is only currently used for the cvar ? command so I'm not too worried.
            return _configVars.Where(c => c.Value.Registered).Select(p => p.Key).ToArray();
        }

        /// <inheritdoc />
        public virtual void SetCVar(string name, object value)
        {
            SetCVarInternal(name, value, _gameTiming.CurTick);
        }

        protected void SetCVarInternal(string name, object value, GameTick intendedTick)
        {
            ValueChangedInvoke? invoke = null;

            using (Lock.WriteGuard())
            {
                //TODO: Make flags work, required non-derpy net system.
                if (_configVars.TryGetValue(name, out var cVar) && cVar.Registered)
                {
                    if (!Equals(cVar.OverrideValueParsed ?? cVar.Value, value))
                    {
                        // Setting an overriden var just turns off the override, basically.
                        cVar.OverrideValue = null;
                        cVar.OverrideValueParsed = null;

                        cVar.Value = value;
                        invoke = SetupInvokeValueChanged(cVar, value, intendedTick);
                    }
                }
                else
                    throw new InvalidConfigurationException($"Trying to set unregistered variable '{name}'");
            }

            if (invoke != null)
                InvokeValueChanged(invoke.Value);
        }

        public void SetCVar<T>(CVarDef<T> def, T value) where T : notnull
        {
            SetCVar(def.Name, value);
        }

        public void OverrideDefault(string name, object value)
        {
            ValueChangedInvoke? invoke = null;

            using (Lock.WriteGuard())
            {
                //TODO: Make flags work, required non-derpy net system.
                if (!_configVars.TryGetValue(name, out var cVar) || !cVar.Registered)
                    throw new InvalidConfigurationException($"Trying to set unregistered variable '{name}'");

                cVar.DefaultValue = value;

                if (cVar.OverrideValue == null && cVar.Value == null)
                    invoke = SetupInvokeValueChanged(cVar, value);
            }

            if (invoke != null)
                InvokeValueChanged(invoke.Value);
        }

        public void OverrideDefault<T>(CVarDef<T> def, T value) where T : notnull
        {
            OverrideDefault(def.Name, value);
        }

        /// <inheritdoc />
        public T GetCVar<T>(string name)
        {
            using var _ = Lock.ReadGuard();
            if (_configVars.TryGetValue(name, out var cVar) && cVar.Registered)
                //TODO: Make flags work, required non-derpy net system.
                return (T)(GetConfigVarValue(cVar))!;

            throw new InvalidConfigurationException($"Trying to get unregistered variable '{name}'");
        }

        public T GetCVar<T>(CVarDef<T> def) where T : notnull
        {
            return GetCVar<T>(def.Name);
        }

        public Type GetCVarType(string name)
        {
            using var _ = Lock.ReadGuard();
            if (!_configVars.TryGetValue(name, out var cVar) || !cVar.Registered)
            {
                throw new InvalidConfigurationException($"Trying to get type of unregistered variable '{name}'");
            }

            // If it's null it's a string, since the rest is primitives which aren't null.
            return cVar.DefaultValue.GetType();
        }

        protected static object GetConfigVarValue(ConfigVar cVar)
        {
            return cVar.OverrideValueParsed ?? cVar.Value ?? cVar.DefaultValue;
        }

        public void OverrideConVars(IEnumerable<(string key, string value)> cVars)
        {
            var invokes = new ValueList<ValueChangedInvoke>();

            using (Lock.WriteGuard())
            {
                foreach (var (key, value) in cVars)
                {
                    if (_configVars.TryGetValue(key, out var cfgVar))
                    {
                        cfgVar.OverrideValue = value;
                        if (cfgVar.Registered)
                        {
                            cfgVar.OverrideValueParsed = ParseOverrideValue(value, cfgVar.DefaultValue?.GetType());
                            if (SetupInvokeValueChanged(cfgVar, cfgVar.OverrideValueParsed) is { } invoke)
                                invokes.Add(invoke);
                        }
                    }
                    else
                    {
                        //or add another unregistered CVar
                        //Note: the defaultValue is arbitrarily 0, it will get overwritten when the cvar is registered.
                        var cVar = new ConfigVar(key, 0, CVar.NONE) { OverrideValue = value };
                        _configVars.Add(key, cVar);
                    }
                }
            }

            foreach (var invoke in invokes)
            {
                InvokeValueChanged(invoke);
            }
        }

        private static object ParseOverrideValue(string value, Type? type)
        {
            if (type == typeof(int))
            {
                return int.Parse(value);
            }

            if (type == typeof(bool))
            {
                return bool.Parse(value);
            }

            if (type == typeof(float))
            {
                return float.Parse(value, CultureInfo.InvariantCulture);
            }

            if (type?.IsEnum ?? false)
            {
                return Enum.Parse(type, value);
            }

            // Must be a string.
            return value;
        }

        /// <summary>
        ///     Converts a TomlObject into its native type.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <returns>The boxed native type of the TomlObject.</returns>
        private static object TypeConvert(TomlObject obj)
        {
            var tmlType = obj.TomlType;
            switch (tmlType)
            {
                case TomlObjectType.Bool:
                    return obj.Get<bool>();

                case TomlObjectType.Float:
                    return obj.Get<float>();

                case TomlObjectType.Int:
                    return obj.Get<int>();

                case TomlObjectType.String:
                    return obj.Get<string>();

                default:
                    throw new InvalidConfigurationException($"Cannot convert {tmlType}.");
            }
        }

        private static void InvokeValueChanged(in ValueChangedInvoke invoke)
        {
            foreach (var entry in invoke.Invoke.Entries)
            {
                entry.Value!.Invoke(invoke.Value, in invoke.Info);
            }
        }

        private ValueChangedInvoke? SetupInvokeValueChanged(ConfigVar var, object value, GameTick? tick = null)
        {
            if (var.ValueChanged.Count == 0)
                return null;

            return new ValueChangedInvoke
            {
                Info = new CVarChangeInfo(tick ?? _gameTiming.CurTick),
                Invoke = var.ValueChanged,
                Value = value
            };
        }

        /// <summary>
        ///     Holds the data for a single configuration variable.
        /// </summary>
        protected sealed class ConfigVar
        {
            /// <summary>
            ///     Constructs a CVar.
            /// </summary>
            /// <param name="name">The name of the CVar. This needs to contain only printable characters.
            /// Underscores '_' are reserved. Everything before the last underscore is a table identifier,
            /// everything after is the CVar name in the TOML document.</param>
            /// <param name="defaultValue">The default value of this CVar.</param>
            /// <param name="flags">Optional flags to modify the behavior of this CVar.</param>
            public ConfigVar(string name, object defaultValue, CVar flags)
            {
                Name = name;
                DefaultValue = defaultValue;
                Flags = flags;
            }

            /// <summary>
            ///     The name of the CVar.
            /// </summary>
            public string Name { get; }

            /// <summary>
            ///     The default value of this CVar.
            /// </summary>
            public object DefaultValue { get; set; }

            /// <summary>
            ///     Optional flags to modify the behavior of this CVar.
            /// </summary>
            public CVar Flags { get; set; }

            /// <summary>
            ///     The current value of this CVar.
            /// </summary>
            public object? Value { get; set; }

            /// <summary>
            ///     Has this CVar been registered in code?
            /// </summary>
            public bool Registered { get; set; }

            /// <summary>
            ///     Was the CVar present in the config file?
            ///     If so we need to always re-save it even if it's not ARCHIVE.
            /// </summary>
            public bool ConfigModified;

            public InvokeList<ValueChangedDelegate> ValueChanged;

            // We don't know what the type of the var is until it's registered.
            // So we can't actually parse them until then.
            // So we keep the raw string around.
            public string? OverrideValue { get; set; }
            public object? OverrideValueParsed { get; set; }
        }

        /// <summary>
        /// All data we need to invoke a deferred ValueChanged handler outside of a write lock.
        /// </summary>
        private struct ValueChangedInvoke
        {
            public InvokeList<ValueChangedDelegate> Invoke;
            public object Value;
            public CVarChangeInfo Info;
        }

        protected delegate void ValueChangedDelegate(object value, in CVarChangeInfo info);
    }

    [Serializable]
    [Virtual]
    public class InvalidConfigurationException : Exception
    {
        public InvalidConfigurationException()
        {
        }

        public InvalidConfigurationException(string message) : base(message)
        {
        }

        public InvalidConfigurationException(string message, Exception inner) : base(message, inner)
        {
        }

        protected InvalidConfigurationException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
    }
}
