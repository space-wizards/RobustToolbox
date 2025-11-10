using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    internal partial class ConfigurationManager : IConfigurationManagerInternal
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly ILogManager _logManager = default!;

        private const char TABLE_DELIMITER = '.';
        protected readonly Dictionary<string, ConfigVar> _configVars = new();
        private ConfigFileStorage? _configFile;
        protected bool _isServer;

        protected readonly ReaderWriterLockSlim Lock = new();

        private ISawmill _sawmill = default!;

        public event Action<CVarChangeInfo>? OnCVarValueChanged;

        /// <summary>
        ///     Constructs a new ConfigurationManager.
        /// </summary>
        public ConfigurationManager()
        {
        }

        public void Initialize(bool isServer)
        {
            _isServer = isServer;

            _sawmill = _logManager.GetSawmill("cfg");
        }

        public virtual void Shutdown()
        {
            using var _ = Lock.WriteGuard();

            _configVars.Clear();
            _configFile = null;
        }

        /// <inheritdoc />
        public HashSet<string> LoadFromTomlStream(Stream file)
        {
            TomlTable tblRoot;
            try
            {
                tblRoot = Toml.ReadStream(file);
            }
            catch (Exception e)
            {
                _sawmill.Error("Unable to load configuration from table:\n{0}", e);
                return [];
            }

            return LoadFromTomlTable(tblRoot);
        }

        private HashSet<string> LoadFromTomlTable(TomlTable table)
        {
            var loaded = new HashSet<string>();
            var callbackEvents = new ValueList<ValueChangedInvoke>();
            try
            {
                // Ensure callbacks are raised OUTSIDE the write lock.
                using (Lock.WriteGuard())
                {
                    foreach (var (cvar, value) in ParseCVarValuesFromToml(table))
                    {
                        loaded.Add(cvar);
                        LoadParsedVar(cvar, value, ref callbackEvents);
                    }
                }
            }
            finally
            {
                RunDeferredInvokeCallbacks(in callbackEvents);
            }

            return loaded;
        }

        private void RunDeferredInvokeCallbacks(in ValueList<ValueChangedInvoke> callbackEvents)
        {
            foreach (var callback in callbackEvents)
            {
                InvokeValueChanged(callback);
            }
        }

        private void LoadParsedVar(
            string cvar,
            object value,
            ref ValueList<ValueChangedInvoke> changedInvokes)
        {
            if (_configVars.TryGetValue(cvar, out var cfgVar))
            {
                // overwrite the value with the saved one
                var oldValue = GetConfigVarValue(cfgVar);

                var convertedValue = value;
                if (cfgVar.Type != value.GetType())
                {
                    try
                    {
                        convertedValue = ConvertToCVarType(value, cfgVar.Type!);
                    }
                    catch
                    {
                        _sawmill.Error($"Parsed cvar does not match registered cvar type. Name: {cvar}. Code Type: {cfgVar.Type}. Parsed type: {value.GetType()}");
                        return;
                    }
                }

                changedInvokes.Add(SetupInvokeValueChanged(cfgVar, convertedValue, oldValue));
                cfgVar.Value = convertedValue;
            }
            else
            {
                //or add another unregistered CVar
                cfgVar = AddUnregisteredCVar(cvar, value);
            }

            cfgVar.ConfigModified = true;
        }

        private ConfigVar AddUnregisteredCVar(string name, object value)
        {
            //Note: the initial defaultValue is null, but it will get overwritten when the cvar is registered.
            var cfgVar = new ConfigVar(name, null!, CVar.NONE) { Value = value };
            _configVars.Add(name, cfgVar);
            return cfgVar;
        }

        public HashSet<string> LoadDefaultsFromTomlStream(Stream stream)
        {
            var tblRoot = Toml.ReadStream(stream);

            var loaded = new HashSet<string>();

            var callbackEvents = new ValueList<ValueChangedInvoke>();

            // Ensure callbacks are raised OUTSIDE the write lock.
            using (Lock.WriteGuard())
            {
                foreach (var (cVarName, value) in ParseCVarValuesFromToml(tblRoot))
                {
                    if (!_configVars.TryGetValue(cVarName, out var cVar) || !cVar.Registered)
                    {
                        _sawmill.Error($"Trying to set unregistered variable '{cVarName}'");
                        continue;
                    }

                    var convertedValue = value;
                    if (cVar.Type != value.GetType())
                    {
                        try
                        {
                            convertedValue = ConvertToCVarType(value, cVar.Type!);
                        }
                        catch
                        {
                            _sawmill.Error($"Override TOML parsed cvar does not match registered cvar type. Name: {cVarName}. Code Type: {cVar.Type}. Toml type: {value.GetType()}");
                            continue;
                        }
                    }

                    if (cVar.OverrideValue == null && cVar.Value == null)
                    {
                        var oldValue = GetConfigVarValue(cVar);
                        callbackEvents.Add(SetupInvokeValueChanged(cVar, convertedValue, oldValue));
                    }

                    cVar.DefaultValue = convertedValue;
                }
            }

            foreach (var callback in callbackEvents)
            {
                InvokeValueChanged(callback);
            }

            return loaded;
        }

        /// <inheritdoc />
        public HashSet<string> LoadFromFile(string configFile)
        {
            try
            {
                HashSet<string> result;
                using (var file = File.OpenRead(configFile))
                {
                    result = LoadFromTomlStream(file);
                }
                SetSaveFile(configFile);
                ApplyRollback();
                _sawmill.Info($"Configuration loaded from file");
                return result;
            }
            catch (Exception e)
            {
                _sawmill.Error("Unable to load configuration file:\n{0}", e);
                return new HashSet<string>(0);
            }
        }

        public void SetSaveFile(string configFile)
        {
            _configFile = new ConfigFileStorageDisk { Path = configFile };
        }

        public void SetVirtualConfig()
        {
            _configFile = new ConfigFileStorageVirtual();
        }

        public void CheckUnusedCVars()
        {
            if (!GetCVar(CVars.CfgCheckUnused))
                return;

            using (Lock.ReadGuard())
            {
                foreach (var cVar in _configVars.Values)
                {
                    if (cVar.Registered)
                        continue;

                    _sawmill.Warning("Unknown CVar found (typo in config?): {CVar}", cVar.Name);
                }
            }
        }

        /// <inheritdoc />
        public void SaveToTomlStream(Stream stream, IEnumerable<string> cvars)
        {
            var table = SaveToTomlTable(cvars);

            Toml.WriteStream(table, stream);
        }

        private TomlTable SaveToTomlTable(IEnumerable<string> cvars, Func<string, object>? overrideValue = null)
        {
            var tblRoot = Toml.Create();

            using (Lock.ReadGuard())
            {
                foreach (var name in cvars)
                {
                    if (!_configVars.TryGetValue(name, out var cVar))
                        continue;

                    object? value;
                    if (overrideValue != null)
                    {
                        value = overrideValue(name);
                    }
                    else
                    {
                        value = cVar.Value;
                        if (value == null && cVar.Registered)
                        {
                            value = cVar.DefaultValue;
                        }
                    }

                    if (value == null)
                    {
                        _sawmill.Error($"CVar {name} has no value or default value, was the default value registered as null?");
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
                    // If you add a type here, add it to .Rollback.cs too!!!
                    // I can't share the code because of how the serialization layers work :(
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
                            _sawmill.Warning($"Cannot serialize '{name}', unsupported type.");
                            break;
                    }
                }
            }

            return tblRoot;
        }

        /// <inheritdoc />
        public void SaveToFile()
        {
            if (_configFile == null)
            {
                _sawmill.Warning("Cannot save the config file, because one was never loaded.");
                return;
            }

            try
            {
                // Always write if it was present when reading from the config file, otherwise:
                // Don't write if Archive flag is not set.
                // Don't write if the cVar is the default value.
                var cvars = _configVars.Where(x => x.Value.ConfigModified
                                                   || ((x.Value.Flags & CVar.ARCHIVE) != 0 && x.Value.Value != null &&
                                                       !x.Value.Value.Equals(x.Value.DefaultValue))).Select(x => x.Key);

                // Write in-memory to avoid bulldozing config file on exception.
                var memoryStream = new MemoryStream();
                SaveToTomlStream(memoryStream, cvars);
                memoryStream.Position = 0;

                switch (_configFile)
                {
                    case ConfigFileStorageDisk disk:
                    {
                        using var file = File.Create(disk.Path);
                        memoryStream.CopyTo(file);
                        break;
                    }
                    case ConfigFileStorageVirtual @virtual:
                    {
                        @virtual.Stream.SetLength(0);
                        memoryStream.CopyTo(@virtual.Stream);
                        break;
                    }
                    default:
                    {
                        throw new UnreachableException();
                    }
                }

                _sawmill.Info($"config saved to '{_configFile}'.");
            }
            catch (Exception e)
            {
                _sawmill.Warning($"Cannot save the config file '{_configFile}'.\n {e}");
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
            DebugTools.AssertEqual(defaultValue.GetType(), type);
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
                    _sawmill.Error($"The variable '{name}' has already been registered.");

                if (cVar.Value != null && type != cVar.Value.GetType())
                {
                    try
                    {
                        cVar.Value = ConvertToCVarType(cVar.Value, type);
                    }
                    catch
                    {
                        _sawmill.Error($"TOML parsed cvar does not match registered cvar type. Name: {name}. Code Type: {type.Name}. Toml type: {cVar.Value.GetType().Name}");
                        return;
                    }
                }

                cVar.DefaultValue = defaultValue;
                cVar.Flags = flags;
                cVar.Register();

                if (cVar.OverrideValue != null)
                {
                    cVar.OverrideValueParsed = ParseOverrideValue(cVar.OverrideValue, type);
                }

                return;
            }

            var cvar = new ConfigVar(name, defaultValue, flags);
            cvar.Register();
            _configVars.Add(name, cvar);
        }

        public void SubValueChanged<T>(CVarDef<T> cVar, Action<T> onValueChanged, bool invokeImmediately = false) where T : notnull
        {
            SubValueChanged(cVar.Name, onValueChanged, invokeImmediately);
        }

        public void SubValueChanged<T>(string name, Action<T> onValueChanged, bool invokeImmediately = false)
            where T : notnull
        {
            using (Lock.WriteGuard())
            {
                var reg = _configVars[name];

                reg.ValueChanged.AddInPlace((object value, in CVarChangeInfo _) =>
                        onValueChanged((T)value),
                    onValueChanged);
            }

            if (invokeImmediately)
            {
                onValueChanged(GetCVar<T>(name));
            }
        }

        public void OnValueChanged<T>(CVarDef<T> cVar, Action<T> onValueChanged, bool invokeImmediately = false)
            where T : notnull
        {
            SubValueChanged(cVar.Name, onValueChanged, invokeImmediately);
        }

        public void OnValueChanged<T>(string name, Action<T> onValueChanged, bool invokeImmediately = false)
            where T : notnull
        {
            SubValueChanged(name, onValueChanged, invokeImmediately);
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

        public void SubValueChanged<T>(CVarDef<T> cVar, CVarChanged<T> onValueChanged, bool invokeImmediately = false)
            where T : notnull
        {
            SubValueChanged(cVar.Name, onValueChanged, invokeImmediately);
        }

        public void SubValueChanged<T>(string name, CVarChanged<T> onValueChanged, bool invokeImmediately = false)
            where T : notnull
        {
            object newValue;

            using (Lock.WriteGuard())
            {
                var reg = _configVars[name];
                newValue = GetConfigVarValue(reg);

                reg.ValueChanged.AddInPlace((object value, in CVarChangeInfo info) =>
                        onValueChanged((T)value, info),
                    onValueChanged);
            }

            if (invokeImmediately)
            {
                onValueChanged(GetCVar<T>(name), new CVarChangeInfo(name, _gameTiming.CurTick, newValue, newValue));
            }
        }

        public void OnValueChanged<T>(CVarDef<T> cVar, CVarChanged<T> onValueChanged, bool invokeImmediately = false)
            where T : notnull
        {
            SubValueChanged(cVar.Name, onValueChanged, invokeImmediately);
        }

        public void OnValueChanged<T>(string name, CVarChanged<T> onValueChanged, bool invokeImmediately = false)
            where T : notnull
        {
            SubValueChanged(name, onValueChanged, invokeImmediately);
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
            foreach (var type in assembly
                         .GetTypes()
                         .Where(p => Attribute.IsDefined(p, typeof(CVarDefsAttribute))))
            {
                LoadCVarsFromType(type);
            }
        }

        public void LoadCVarsFromType(Type containingType)
        {
            foreach (var defField in containingType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
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
        public virtual void SetCVar(string name, object value, bool force = false)
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
                        var oldValue = GetConfigVarValue(cVar);
                        invoke = SetupInvokeValueChanged(cVar, value, oldValue, intendedTick);

                        // Setting an overriden var just turns off the override, basically.
                        cVar.OverrideValue = null;
                        cVar.OverrideValueParsed = null;
                        cVar.Value = value;
                    }
                }
                else
                    throw new InvalidConfigurationException($"Trying to set unregistered variable '{name}'");
            }

            if (invoke != null)
                InvokeValueChanged(invoke.Value);
        }

        public void SetCVar<T>(CVarDef<T> def, T value, bool force = false) where T : notnull
        {
            SetCVar(def.Name, value, force);
        }

        public void OverrideDefault(string name, object value)
        {
            ValueChangedInvoke? invoke = null;

            using (Lock.WriteGuard())
            {
                //TODO: Make flags work, required non-derpy net system.
                if (!_configVars.TryGetValue(name, out var cVar) || !cVar.Registered)
                    throw new InvalidConfigurationException($"Trying to set unregistered variable '{name}'");

                if (cVar.OverrideValue == null && cVar.Value == null)
                {
                    var oldValue = GetConfigVarValue(cVar);
                    invoke = SetupInvokeValueChanged(cVar, value, oldValue);
                }

                cVar.DefaultValue = value;

            }

            if (invoke != null)
                InvokeValueChanged(invoke.Value);
        }

        public void OverrideDefault<T>(CVarDef<T> def, T value) where T : notnull
        {
            OverrideDefault(def.Name, value);
        }

        public object GetCVar(string name)
        {
            using var _ = Lock.ReadGuard();
            if (_configVars.TryGetValue(name, out var cVar) && cVar.Registered)
                return GetConfigVarValue(cVar);

            throw new InvalidConfigurationException($"Trying to get unregistered variable '{name}'");
        }

        /// <inheritdoc />
        public T GetCVar<T>(string name)
        {
            return (T)GetCVar(name);
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
            return cVar.Type!;
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
                        if (!cfgVar.Registered)
                            continue;

                        var newValue = ParseOverrideValue(value, cfgVar.Type!);
                        var oldValue = GetConfigVarValue(cfgVar);
                        invokes.Add(SetupInvokeValueChanged(cfgVar, newValue, oldValue));
                        cfgVar.OverrideValueParsed = newValue;
                    }
                    else
                    {
                        //or add another unregistered CVar
                        //Note: the initial defaultValue is null, but it will get overwritten when the cvar is registered.
                        var cVar = new ConfigVar(key, null!, CVar.NONE) { OverrideValue = value };
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

            if (type == typeof(long))
            {
                return long.Parse(value);
            }

            if (type == typeof(ushort))
            {
                return ushort.Parse(value);
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
                    var val = obj.Get<long>();
                    if (val is >= int.MinValue and <= int.MaxValue)
                        return obj.Get<int>();

                    return val;

                case TomlObjectType.String:
                    return obj.Get<string>();

                default:
                    throw new InvalidConfigurationException($"Cannot convert {tmlType}.");
            }
        }

        private void InvokeValueChanged(in ValueChangedInvoke invoke)
        {
            try
            {
                OnCVarValueChanged?.Invoke(invoke.Info);
            }
            catch (Exception e)
            {
                _sawmill.Error($"Error while running OnCVarValueChanged callback: {e}");
            }

            foreach (var entry in invoke.Invoke.Entries)
            {
                try
                {
                    entry.Value!.Invoke(invoke.Value, in invoke.Info);
                }
                catch (Exception e)
                {
                    _sawmill.Error($"Error while running OnValueChanged callback: {e}");
                }
            }
        }

        private ValueChangedInvoke SetupInvokeValueChanged(ConfigVar var, object newValue, object oldValue, GameTick? tick = null)
        {
            tick ??= _gameTiming.CurTick;
            var info = new CVarChangeInfo(var.Name, tick.Value, newValue, oldValue);
            return new ValueChangedInvoke(info, var.ValueChanged);
        }

        private IEnumerable<(string cvar, object value)> ParseCVarValuesFromToml(TomlTable tblRoot)
        {
            return ProcessTomlObject(tblRoot, "");

            IEnumerable<(string cvar, object value)> ProcessTomlObject(TomlObject obj, string tablePath)
            {
                if (obj is TomlTable table)
                {
                    foreach (var kvTml in table)
                    {
                        string newPath;

                        if ((kvTml.Value is TomlTable))
                            newPath = tablePath + kvTml.Key + TABLE_DELIMITER;
                        else
                            newPath = tablePath + kvTml.Key;

                        foreach (var tuple in ProcessTomlObject(kvTml.Value, newPath))
                        {
                            yield return tuple;
                        }
                    }

                    yield break;
                }

                var tomlValue = TypeConvert(obj);
                yield return (tablePath, tomlValue);
            }
        }

        /// <summary>
        /// Try to convert a compatible value to the actual registration type of a CVar.
        /// </summary>
        /// <remarks>
        /// When CVars are parsed from TOML, their in-code type is not known.
        /// This function does the necessary conversions from e.g. int to long.
        /// </remarks>
        /// <param name="value">
        /// The value to convert.
        /// This must be a simple type like strings or integers.
        /// </param>
        /// <param name="cVar">
        /// The registration type of the CVar.
        /// </param>
        /// <returns></returns>
        private static object ConvertToCVarType(object value, Type cVar)
        {
            if (cVar.IsEnum)
                return Enum.Parse(cVar, value.ToString() ?? string.Empty);

            return Convert.ChangeType(value, cVar);
        }

        internal List<Delegate> GetSubs(string name)
        {
            using (Lock.ReadGuard())
            {
                var list = new List<Delegate>();

                if (!_configVars.TryGetValue(name, out var cVar))
                    throw new InvalidConfigurationException($"Trying to get unregistered variable '{name}'");

                foreach (var entry in cVar.ValueChanged.Entries)
                {
                    list.Add((Delegate) entry.Equality!);
                }

                return list;
            }
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
                Flags = flags;
                _defaultValue = defaultValue;
            }

            /// <summary>
            ///     The type of the cvar's value. This may be null until the cvar is registered.
            /// </summary>
            public Type? Type { get; internal set; }

            /// <summary>
            ///     The name of the CVar.
            /// </summary>
            public string Name { get; }

            /// <summary>
            ///     The default value of this CVar.
            /// </summary>
            public object DefaultValue
            {
                get => _defaultValue;
                set
                {
                    if (Registered)
                        DebugTools.AssertEqual(value.GetType(), Type);
                    _defaultValue = value;
                }
            }

            /// <summary>
            ///     Optional flags to modify the behavior of this CVar.
            /// </summary>
            public CVar Flags { get; set; }

            /// <summary>
            ///     The current value of this CVar.
            /// </summary>
            public object? Value
            {
                get => _value;
                set
                {
                    if (value != null && Registered)
                        DebugTools.AssertEqual(value.GetType(), Type);
                    _value = value;
                }
            }

            /// <summary>
            ///     Has this CVar been registered in code?
            /// </summary>
            public bool Registered { get; private set; }

            public void Register()
            {
                if (Registered)
                {
                    DebugTools.AssertNotNull(DefaultValue);
                    DebugTools.AssertEqual(DefaultValue.GetType(), Type);
                    DebugTools.Assert(Value == null || Value.GetType() == Type);
                    DebugTools.Assert(OverrideValueParsed == null || OverrideValueParsed.GetType() == Type);
                    return;
                }

                if (_defaultValue == null)
                    throw new NullReferenceException("Must specify default value before registering");

                if (Value != null && DefaultValue.GetType() != Value.GetType())
                    throw new Exception($"The cvar value & default value must be of the same type");

                if (OverrideValueParsed != null && DefaultValue.GetType() != OverrideValueParsed.GetType())
                    throw new Exception($"The cvar override value & default value must be of the same type");

                Type = DefaultValue.GetType();
                Registered = true;
            }

            /// <summary>
            ///     Was the CVar present in the config file?
            ///     If so we need to always re-save it even if it's not ARCHIVE.
            /// </summary>
            public bool ConfigModified;

            public InvokeList<ValueChangedDelegate> ValueChanged;
            private object _defaultValue;
            private object? _value;
            private object? _overrideValueParsed;

            // We don't know what the type of the var is until it's registered.
            // So we can't actually parse them until then.
            // So we keep the raw string around.
            public string? OverrideValue { get; set; }

            public object? OverrideValueParsed
            {
                get => _overrideValueParsed;
                set
                {
                    if (value != null && Registered)
                        DebugTools.AssertEqual(value.GetType(), Type);
                    _overrideValueParsed = value;
                }
            }
        }

        /// <summary>
        /// All data we need to invoke a deferred ValueChanged handler outside of a write lock.
        /// </summary>
        private struct ValueChangedInvoke
        {
            public InvokeList<ValueChangedDelegate> Invoke;
            public object Value => Info.NewValue;
            public CVarChangeInfo Info;

            public ValueChangedInvoke(CVarChangeInfo info, InvokeList<ValueChangedDelegate> invoke) : this()
            {
                Info = info;
                Invoke = invoke;
            }
        }

        protected delegate void ValueChangedDelegate(object value, in CVarChangeInfo info);

        private abstract class ConfigFileStorage;

        private sealed class ConfigFileStorageDisk : ConfigFileStorage
        {
            public required string Path;

            public override string ToString()
            {
                return Path;
            }
        }

        private sealed class ConfigFileStorageVirtual : ConfigFileStorage
        {
            // I did not realize when adding this class that there is currently no way to *load* this data again.
            // Oh well, might be useful for a future unit test.
            public readonly MemoryStream Stream = new();

            public override string ToString()
            {
                return "<VIRTUAL>";
            }
        }
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
    }
}
