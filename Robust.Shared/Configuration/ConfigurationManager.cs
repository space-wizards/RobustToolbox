using Nett;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Log;
using System;
using System.Collections.Generic;
using System.IO;

namespace Robust.Shared.Configuration
{
    /// <summary>
    ///     Stores and manages global configuration variables.
    /// </summary>
    public class ConfigurationManager : IConfigurationManager
    {
        private const char TABLE_DELIMITER = '.';
        private readonly Dictionary<string, ConfigVar> _configVars;
        private string _configFile;

        /// <summary>
        ///     Constructs a new ConfigurationManager.
        /// </summary>
        public ConfigurationManager()
        {
            _configVars = new Dictionary<string, ConfigVar>();
        }

        /// <inheritdoc />
        public void LoadFromFile(string configFile)
        {
            try
            {
                var tblRoot = Toml.ReadFile(configFile);

                ProcessTomlObject(tblRoot);

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
        /// <param name="tablePath">For internal use only, the current path to the node.</param>
        private void ProcessTomlObject(TomlObject obj, string tablePath = "")
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

                    ProcessTomlObject(kvTml.Value, newPath);
                }
            }
            else // this is a key, add CVar
            {
                // if the CVar has already been registered
                if (_configVars.TryGetValue(tablePath, out ConfigVar cfgVar))
                {
                    // overwrite the value with the saved one
                    cfgVar.Value = TypeConvert(obj);
                    cfgVar.ValueChanged?.Invoke(cfgVar.Value);
                }
                else
                {
                    //or add another unregistered CVar
                    var cVar = new ConfigVar(tablePath, null, CVar.NONE) { Value = TypeConvert(obj) };
                    _configVars.Add(tablePath, cVar);
                }
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

                foreach (var kvCVar in _configVars)
                {
                    var cVar = kvCVar.Value;
                    var name = kvCVar.Key;

                    var value = cVar.Value;
                    if (value == null && cVar.Registered)
                    {
                        value = cVar.DefaultValue;
                    }

                    if (value == null)
                    {
                        Logger.ErrorS("cfg", $"CVar {name} has no value or default value, was the default value registered as null?");
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
                        table = tblObject as TomlTable ?? throw new InvalidConfigurationException($"[CFG] Object {curTblName} is being used like a table, but it is a {tblObject}. Are your CVar names formed properly?");
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

                Toml.WriteFile(tblRoot, _configFile);
                Logger.InfoS("cfg", $"config saved to '{_configFile}'.");
            }
            catch (Exception e)
            {
                Logger.WarningS("cfg", $"Cannot save the config file '{_configFile}'.\n {e.Message}");
            }
        }

        public void RegisterCVar<T>(string name, T defaultValue, CVar flags = CVar.NONE, Action<T> onValueChanged = null)
        {
            Action<object> valueChangedDelegate = null;
            if (onValueChanged != null)
            {
                valueChangedDelegate = v => onValueChanged((T) v);
            }
            if (_configVars.TryGetValue(name, out ConfigVar cVar))
            {
                if (cVar.Registered)
                    Logger.ErrorS("cfg", $"The variable '{name}' has already been registered.");

                cVar.DefaultValue = defaultValue;
                cVar.Flags = flags;
                cVar.Registered = true;
                cVar.ValueChanged = valueChangedDelegate;

                if (cVar.OverrideValue != null)
                {
                    cVar.OverrideValueParsed = ParseOverrideValue(cVar.OverrideValue, typeof(T));
                }

                return;
            }

            _configVars.Add(name, new ConfigVar(name, defaultValue, flags)
            {
                Registered = true,
                Value = defaultValue,
                ValueChanged = valueChangedDelegate
            });
        }

        /// <inheritdoc />
        public bool IsCVarRegistered(string name)
        {
            return _configVars.TryGetValue(name, out ConfigVar cVar) && cVar.Registered;
        }

        /// <inheritdoc />
        public void SetCVar(string name, object value)
        {
            //TODO: Make flags work, required non-derpy net system.
            if (_configVars.TryGetValue(name, out ConfigVar cVar) && cVar.Registered)
            {
                cVar.Value = value;
                cVar.ValueChanged?.Invoke(value);

                // Setting an override value just turns off the override, basically.
                cVar.OverrideValue = null;
                cVar.OverrideValueParsed = null;
            }
            else
                throw new InvalidConfigurationException($"Trying to set unregistered variable '{name}'");
        }

        /// <inheritdoc />
        public T GetCVar<T>(string name)
        {
            if (_configVars.TryGetValue(name, out ConfigVar cVar) && cVar.Registered)
                //TODO: Make flags work, required non-derpy net system.
                return (T)(cVar.OverrideValueParsed ?? cVar.Value ?? cVar.DefaultValue);

            throw new InvalidConfigurationException($"Trying to get unregistered variable '{name}'");
        }

        public Type GetCVarType(string name)
        {
            var cVar = _configVars[name];

            // If it's null it's a string, since the rest is primitives which aren't null.
            return cVar.Value?.GetType() ?? typeof(string);
        }

        public void OverrideConVars(IReadOnlyCollection<(string key, string value)> cVars)
        {
            foreach (var (key, value) in cVars)
            {
                if (_configVars.TryGetValue(key, out var cfgVar))
                {
                    cfgVar.OverrideValue = value;
                    cfgVar.OverrideValueParsed = ParseOverrideValue(value, cfgVar.DefaultValue?.GetType());
                    cfgVar.ValueChanged?.Invoke(cfgVar.OverrideValueParsed);
                }
                else
                {
                    //or add another unregistered CVar
                    var cVar = new ConfigVar(key, null, CVar.NONE) { OverrideValue = value };
                    _configVars.Add(key, cVar);
                }
            }
        }

        private object ParseOverrideValue(string value, Type type)
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
                return float.Parse(value);
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

        /// <summary>
        ///     Holds the data for a single configuration variable.
        /// </summary>
        private class ConfigVar
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
            public object Value { get; set; }

            /// <summary>
            ///     Has this CVar been registered in code?
            /// </summary>
            public bool Registered { get; set; }

            /// <summary>
            ///     Invoked when the value of this CVar is changed.
            /// </summary>
            public Action<object> ValueChanged { get; set; }

            // We don't know what the type of the var is until it's registered.
            // So we can't actually parse them until then.
            // So we keep the raw string around.
            public string OverrideValue { get; set; }
            public object OverrideValueParsed { get; set; }
        }
    }

    [System.Serializable]
    public class InvalidConfigurationException : System.Exception
    {
        public InvalidConfigurationException()
        {
        }
        public InvalidConfigurationException(string message) : base(message)
        {
        }
        public InvalidConfigurationException(string message, System.Exception inner) : base(message, inner)
        {
        }
        protected InvalidConfigurationException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
