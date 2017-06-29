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
                Logger.Info($"[CFG] Configuration Loaded from '{configFile}'");
            }
            catch (Exception e)
            {
                Logger.Warning("[CFG] Unable to load configuration file:\n{0}", e);
            }
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
                }
                else
                {
                    //or add another unregistered CVar
                    var cVar = new ConfigVar(tablePath, null, CVarFlags.NONE) { Value = TypeConvert(obj) };
                    _configVars.Add(tablePath, cVar);
                }
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

                    var keyIndex = name.LastIndexOf(TABLE_DELIMITER);
                    var tblPath = name.Substring(0, keyIndex).Split(TABLE_DELIMITER);
                    var keyName = name.Substring(keyIndex + 1);

                    // locate the Table in the config tree
                    var table = tblRoot;
                    foreach (var curTblName in tblPath)
                    {
                        if (!table.TryGetValue(curTblName, out TomlObject tblObject))
                            tblObject = table.AddTable(curTblName);

                        table = tblObject as TomlTable ?? throw new Exception($"[CFG] Object {curTblName} is being used like a table, but it is a {tblObject}. Are your CVar names formed properly?");
                    }

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
