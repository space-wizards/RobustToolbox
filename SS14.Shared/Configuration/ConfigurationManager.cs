using System;
using System.Collections.Generic;
using Nett;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Log;

namespace SS14.Shared.Configuration
{
    /// <summary>
    ///     Stores and manages global configuration variables.
    /// </summary>
    public class ConfigurationManager : IConfigurationManager
    {
        private const char TABLE_DELIMITER = '_';
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

                foreach (var kvTable in tblRoot)
                {
                    var table = kvTable.Value as TomlTable;

                    // filters keys in root table
                    if (table == null)
                    {
                        Logger.Warning($"[CFG] Object {kvTable.Key} in root is not a table, ignoring.");
                        continue;
                    }

                    foreach (var kvObj in table)
                    {
                        var cVarFullName = kvTable.Key + TABLE_DELIMITER + kvObj.Key;

                        // if the CVar has already been registered
                        if (_configVars.TryGetValue(cVarFullName, out ConfigVar cfgVar))
                        {
                            // overwrite the value with the saved one
                            cfgVar.Value = TypeConvert(kvObj.Value);
                            continue;
                        }

                        //or add another unregistered CVar
                        var cVar = new ConfigVar(cVarFullName, null, CVarFlags.NONE) {Value = TypeConvert(kvObj.Value)};
                        _configVars.Add(cVarFullName, cVar);
                    }
                }

                _configFile = configFile;
                Logger.Info($"[CFG] Configuration Loaded from '{configFile}'");
            }
            catch (Exception e)
            {
                Logger.Warning("[CFG] Unable to load configuration file:\n{0}", e);
            }
        }

        /// <inheritdoc />
        public void SaveToFile()
        {
            if (_configFile == null)
            {
                Logger.Warning("[CFG] Cannot save the config file, because one was never loaded.");
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
                    
                    if(value == null)
                    {
                        Logger.Error($"[CFG] CVar {name} has no value or default value, was the default value registered as null?");
                        continue;
                    }

                    var index = name.LastIndexOf(TABLE_DELIMITER);
                    var tblName = name.Substring(0, index);
                    var keyName = name.Substring(index + 1);

                    if (!tblRoot.TryGetValue(tblName, out TomlObject tblObject))
                        tblObject = tblRoot.AddTable(tblName);

                    // we are controlling tblObject, this should never be null
                    var table = (TomlTable)tblObject;

                    //runtime unboxing, either this or generic hell... ¯\_(ツ)_/¯
                    switch (value)
                    {
                        case Enum val:
                            table.AddValue(keyName, (int) (object) val); // asserts Enum value != (ulong || long)
                            break;
                        case int val:
                            table.AddValue(keyName, val);
                            break;
                        case long val:
                            table.AddValue(keyName, val);
                            break;
                        case bool val:
                            table.AddValue(keyName, val);
                            break;
                        case string val:
                            table.AddValue(keyName, val);
                            break;
                        case float val:
                            table.AddValue(keyName, val);
                            break;
                        case double val:
                            table.AddValue(keyName, val);
                            break;
                        default:
                            Logger.Warning($"[CFG] Cannot serialize '{name}', unsupported type.");
                            break;
                    }
                }

                Toml.WriteFile(tblRoot, _configFile);
                Logger.Info($"[CFG] Server config saved to '{_configFile}'.");
            }
            catch (Exception e)
            {
                Logger.Warning($"[CFG] Cannot save the config file '{_configFile}'.\n {e.Message}");
            }
        }

        /// <inheritdoc />
        public void RegisterCVar(string name, object defaultValue, CVarFlags flags = CVarFlags.NONE)
        {
            if (_configVars.TryGetValue(name, out ConfigVar cVar))
            {
                if (cVar.Registered)
                    Logger.Error($"[CVar] The variable '{name}' has already been registered.");

                cVar.DefaultValue = defaultValue;
                cVar.Flags = flags;
                cVar.Registered = true;
                return;
            }

            _configVars.Add(name, new ConfigVar(name, defaultValue, flags) {Registered = true, Value = defaultValue});
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
                cVar.Value = value;
            else
                throw new Exception($"[CVar] Trying to set unregistered variable '{name}'");
        }

        /// <inheritdoc />
        public T GetCVar<T>(string name)
        {
            if (_configVars.TryGetValue(name, out ConfigVar cVar) && cVar.Registered)
                //TODO: Make flags work, required non-derpy net system.
                return (T) (cVar.Value ?? cVar.DefaultValue);

            throw new Exception($"[CVar] Trying to get unregistered variable '{name}'");
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
                    throw new Exception($"[CFG] Cannot convert {tmlType}.");
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
            public ConfigVar(string name, object defaultValue, CVarFlags flags)
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
            public CVarFlags Flags { get; set; }

            /// <summary>
            ///     The current value of this CVar.
            /// </summary>
            public object Value { get; set; }

            /// <summary>
            ///     Has this CVar been registered in code?
            /// </summary>
            public bool Registered { get; set; }
        }
    }
}
