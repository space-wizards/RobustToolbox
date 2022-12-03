using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Text.RegularExpressions;
using Robust.Client.Debugging;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Asynchronous;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Robust.Client.Console.Commands
{
    internal sealed class DumpEntitiesCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override string Command => "dumpentities";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            foreach (var e in _entityManager.GetEntities().OrderBy(e => e))
            {
                shell.WriteLine(
                    $"entity {e}, {_entityManager.GetComponent<MetaDataComponent>(e).EntityPrototype?.ID}, {_entityManager.GetComponent<TransformComponent>(e).Coordinates}.");
            }
        }
    }

    internal sealed class GetComponentRegistrationCommand : LocalizedCommands
    {
        [Dependency] private readonly IComponentFactory _componentFactory = default!;


        public override string Command => "getcomponentregistration";


        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteLine(Help);
                return;
            }

            try
            {
                var registration = _componentFactory.GetRegistration(args[0]);

                var message = new StringBuilder($"'{registration.Name}': (type: {registration.Type}, ");
                if (registration.NetID == null)
                {
                    message.Append("no Net ID");
                }
                else
                {
                    message.Append($"net ID: {registration.NetID}");
                }

                message.Append($", References:");

                shell.WriteLine(message.ToString());

                foreach (var type in registration.References)
                {
                    shell.WriteLine($"  {type}");
                }
            }
            catch (UnknownComponentException)
            {
                shell.WriteError($"No registration found for '{args[0]}'");
            }
        }
    }

    internal sealed class ToggleMonitorCommand : LocalizedCommands
    {
        [Dependency] private readonly IUserInterfaceManager _uiMgr = default!;


        public override string Command => "monitor";

        public override string Help
        {
            get
            {
                var monitors = string.Join(", ", Enum.GetNames<DebugMonitor>());
                return Loc.GetString("cmd-monitor-help", ("monitors", monitors));
            }
        }

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var monitors = _uiMgr.DebugMonitors;

            if (args.Length != 1)
            {
                shell.WriteLine(Loc.GetString("cmd-monitor-arg-count"));
                return;
            }

            var monitorArg = args[0];
            if (monitorArg.Equals("-all", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var monitor in Enum.GetValues<DebugMonitor>())
                {
                    monitors.SetMonitor(monitor, false);
                }

                return;
            }

            if (monitorArg.Equals("+all", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var monitor in Enum.GetValues<DebugMonitor>())
                {
                    monitors.SetMonitor(monitor, true);
                }

                return;
            }

            if (!Enum.TryParse(monitorArg, true, out DebugMonitor parsedMonitor))
            {
                shell.WriteError(Loc.GetString("cmd-monitor-invalid-name"));
                return;
            }

            monitors.ToggleMonitor(parsedMonitor);
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var allOptions = new CompletionOption[]
                {
                    new("-all", Loc.GetString("cmd-monitor-minus-all-hint")),
                    new("+all", Loc.GetString("cmd-monitor-plus-all-hint"))
                };

                var options = allOptions.Concat(Enum.GetNames<DebugMonitor>().Select(c => new CompletionOption(c)));
                return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-monitor-arg-monitor"));
            }

            return CompletionResult.Empty;
        }
    }

    internal sealed class ExceptionCommand : LocalizedCommands
    {
        public override string Command => "fuck";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            throw new InvalidOperationException("Fuck");
        }
    }

    internal sealed class ShowPositionsCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntitySystemManager _entitySystems = default!;

        public override string Command => "showpos";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var mgr = _entitySystems.GetEntitySystem<DebugDrawingSystem>();
            mgr.DebugPositions = !mgr.DebugPositions;
        }
    }

    internal sealed class ShowRayCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntitySystemManager _entitySystems = default!;

        public override string Command => "showrays";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine(Help);
                return;
            }

            if (!float.TryParse(args[0], out var duration))
            {
                shell.WriteError($"{args[0]} is not a valid float.");
                return;
            }

            var mgr = _entitySystems.GetEntitySystem<DebugRayDrawingSystem>();
            mgr.DebugDrawRays = !mgr.DebugDrawRays;
            shell.WriteError("Toggled showing rays to:" + mgr.DebugDrawRays);
            mgr.DebugRayLifetime = TimeSpan.FromSeconds(duration);
        }
    }

    internal sealed class DisconnectCommand : LocalizedCommands
    {
        [Dependency] private readonly IClientNetManager _netManager = default!;

        public override string Command => "disconnect";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            _netManager.ClientDisconnect("Disconnect command used.");
        }
    }

    internal sealed class EntityInfoCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override string Command => "entfo";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine(Help);
                return;
            }

            if ((!new Regex(@"^c?[0-9]+$").IsMatch(args[0])))
            {
                shell.WriteError("Malformed UID");
                return;
            }

            var uid = EntityUid.Parse(args[0]);
            var entmgr = _entityManager;
            if (!entmgr.EntityExists(uid))
            {
                shell.WriteError("That entity does not exist. Sorry lad.");
                return;
            }

            var meta = entmgr.GetComponent<MetaDataComponent>(uid);
            shell.WriteLine($"{uid}: {meta.EntityPrototype?.ID}/{meta.EntityName}");
            shell.WriteLine(
                $"init/del/lmt: {meta.EntityInitialized}/{meta.EntityDeleted}/{meta.EntityLastModifiedTick}");
            foreach (var component in entmgr.GetComponents(uid))
            {
                shell.WriteLine(component.ToString() ?? "");
                if (component is IComponentDebug debug)
                {
                    foreach (var line in debug.GetDebugString().Split('\n'))
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        shell.WriteLine("\t" + line);
                    }
                }
            }
        }
    }

    internal sealed class SnapGridGetCell : LocalizedCommands
    {
        [Dependency] private readonly IMapManager _map = default!;

        public override string Command => "sggcell";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine(Help);
                return;
            }

            string indices = args[1];

            if (!EntityUid.TryParse(args[0], out var gridUid))
            {
                shell.WriteError($"{args[0]} is not a valid entity UID.");
                return;
            }

            if (!new Regex(@"^-?[0-9]+,-?[0-9]+$").IsMatch(indices))
            {
                shell.WriteError("mapIndicies must be of form x<int>,y<int>");
                return;
            }

            if (_map.TryGetGrid(gridUid, out var grid))
            {
                foreach (var entity in grid.GetAnchoredEntities(new Vector2i(
                             int.Parse(indices.Split(',')[0], CultureInfo.InvariantCulture),
                             int.Parse(indices.Split(',')[1], CultureInfo.InvariantCulture))))
                {
                    shell.WriteLine(entity.ToString());
                }
            }
            else
            {
                shell.WriteError("grid does not exist");
            }
        }
    }

    internal sealed class SetPlayerName : LocalizedCommands
    {
        [Dependency] private readonly IBaseClient _baseClient = default!;

        public override string Command => "overrideplayername";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteLine(Help);
                return;
            }

            _baseClient.PlayerNameOverride = args[0];

            shell.WriteLine($"Overriding player name to \"{args[0]}\".");
        }
    }

    internal sealed class LoadResource : LocalizedCommands
    {
        [Dependency] private readonly IResourceCache _res = default!;
        [Dependency] private readonly IReflectionManager _reflection = default!;

        public override string Command => "ldrsc";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 2)
            {
                shell.WriteLine(Help);
                return;
            }

            Type type;

            try
            {
                type = _reflection.LooseGetType(args[1]);
            }
            catch (ArgumentException)
            {
                shell.WriteError("Unable to find type");
                return;
            }

            var getResourceMethod =
                _res
                    .GetType()
                    .GetMethod("GetResource", new[] { typeof(string), typeof(bool) });
            DebugTools.Assert(getResourceMethod != null);
            var generic = getResourceMethod!.MakeGenericMethod(type);
            generic.Invoke(_res, new object[] { args[0], true });
        }
    }

    internal sealed class ReloadResource : LocalizedCommands
    {
        [Dependency] private readonly IResourceCache _res = default!;
        [Dependency] private readonly IReflectionManager _reflection = default!;

        public override string Command => "rldrsc";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 2)
            {
                shell.WriteLine(Help);
                return;
            }

            Type type;
            try
            {
                type = _reflection.LooseGetType(args[1]);
            }
            catch (ArgumentException)
            {
                shell.WriteError("Unable to find type");
                return;
            }

            var getResourceMethod = _res.GetType().GetMethod("ReloadResource", new[] { typeof(string) });
            DebugTools.Assert(getResourceMethod != null);
            var generic = getResourceMethod!.MakeGenericMethod(type);
            generic.Invoke(_res, new object[] { args[0] });
        }
    }

    internal sealed class GridTileCount : LocalizedCommands
    {
        [Dependency] private readonly IMapManager _map = default!;

        public override string Command => "gridtc";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine(Help);
                return;
            }

            if (!EntityUid.TryParse(args[0], out var gridUid))
            {
                shell.WriteLine($"{args[0]} is not a valid entity UID.");
                return;
            }

            if (_map.TryGetGrid(gridUid, out var grid))
            {
                shell.WriteLine(grid.GetAllTiles().Count().ToString());
            }
            else
            {
                shell.WriteError($"No grid exists with id {gridUid}");
            }
        }
    }

    internal sealed class GuiDumpCommand : LocalizedCommands
    {
        [Dependency] private readonly IUserInterfaceManager _ui = default!;
        [Dependency] private readonly IResourceCache _res = default!;

        public override string Command => "guidump";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            using var writer = _res.UserData.OpenWriteText(new ResourcePath("/guidump.txt"));

            foreach (var root in _ui.AllRoots)
            {
                writer.WriteLine($"ROOT: {root}");
                _writeNode(root, 0, writer);
                writer.WriteLine("---------------");
            }

            shell.WriteLine("Saved guidump");
        }

        private static void _writeNode(Control control, int indents, TextWriter writer)
        {
            var indentation = new string(' ', indents * 2);
            writer.WriteLine("{0}{1}", indentation, control);
            foreach (var (key, value) in PropertyValuesFor(control))
            {
                writer.WriteLine("{2} * {0}: {1}", key, value, indentation);
            }

            foreach (var child in control.Children)
            {
                _writeNode(child, indents + 1, writer);
            }
        }

        internal static List<(string, string)> PropertyValuesFor(Control control)
        {
            var members = new List<(string, string)>();
            var type = control.GetType();

            foreach (var fieldInfo in type.GetAllFields())
            {
                if (!ViewVariablesUtility.TryGetViewVariablesAccess(fieldInfo, out _))
                {
                    continue;
                }

                members.Add((fieldInfo.Name, fieldInfo.GetValue(control)?.ToString() ?? "null"));
            }

            foreach (var propertyInfo in type.GetAllProperties())
            {
                if (!ViewVariablesUtility.TryGetViewVariablesAccess(propertyInfo, out _))
                {
                    continue;
                }

                members.Add((propertyInfo.Name, propertyInfo.GetValue(control)?.ToString() ?? "null"));
            }

            foreach (var (attachedProperty, value) in control.AllAttachedProperties)
            {
                members.Add(($"{attachedProperty.OwningType.Name}.{attachedProperty.Name}",
                    value?.ToString() ?? "null"));
            }

            members.Sort((a, b) => string.Compare(a.Item1, b.Item1, StringComparison.Ordinal));
            return members;
        }
    }

    internal sealed class SetClipboardCommand : LocalizedCommands
    {
        [Dependency] private readonly IClipboardManager _clipboard = default!;

        public override string Command => "setclipboard";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            _clipboard.SetText(args[0]);
        }
    }

    internal sealed class GetClipboardCommand : LocalizedCommands
    {
        [Dependency] private readonly IClipboardManager _clipboard = default!;

        public override string Command => "getclipboard";

        public override async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            shell.WriteLine(await _clipboard.GetText());
        }
    }

    internal sealed class ToggleLight : LocalizedCommands
    {
        [Dependency] private readonly ILightManager _light = default!;

        public override string Command => "togglelight";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (!_light.LockConsoleAccess)
                _light.Enabled = !_light.Enabled;
        }
    }

    internal sealed class ToggleFOV : LocalizedCommands
    {
        [Dependency] private readonly IEyeManager _eye = default!;

        public override string Command => "togglefov";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            _eye.CurrentEye.DrawFov = !_eye.CurrentEye.DrawFov;
        }
    }

    internal sealed class ToggleHardFOV : LocalizedCommands
    {
        [Dependency] private readonly ILightManager _light = default!;

        public override string Command => "togglehardfov";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (!_light.LockConsoleAccess)
                _light.DrawHardFov = !_light.DrawHardFov;
        }
    }

    internal sealed class ToggleShadows : LocalizedCommands
    {
        [Dependency] private readonly ILightManager _light = default!;

        public override string Command => "toggleshadows";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (!_light.LockConsoleAccess)
                _light.DrawShadows = !_light.DrawShadows;
        }
    }

    internal sealed class ToggleLightBuf : LocalizedCommands
    {
        [Dependency] private readonly ILightManager _light = default!;

        public override string Command => "togglelightbuf";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (!_light.LockConsoleAccess)
                _light.DrawLighting = !_light.DrawLighting;
        }
    }

    internal sealed class ChunkInfoCommand : LocalizedCommands
    {
        [Dependency] private readonly IMapManager _map = default!;
        [Dependency] private readonly IEyeManager _eye = default!;
        [Dependency] private readonly IInputManager _input = default!;

        public override string Command => "chunkinfo";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var mousePos = _eye.ScreenToMap(_input.MouseScreenPosition);

            if (!_map.TryFindGridAt(mousePos, out var grid))
            {
                shell.WriteLine("No grid under your mouse cursor.");
                return;
            }

            var chunkIndex = grid.LocalToChunkIndices(grid.MapToGrid(mousePos));
            var chunk = grid.GetOrAddChunk(chunkIndex);

            shell.WriteLine($"worldBounds: {grid.CalcWorldAABB(chunk)} localBounds: {chunk.CachedBounds}");
        }
    }

    internal sealed class ReloadShadersCommand : LocalizedCommands
    {
        [Dependency] private readonly IResourceCacheInternal _res = default!;
        [Dependency] private readonly ITaskManager _taskManager = default!;

        public override string Command => "rldshader";

        public static Dictionary<string, FileSystemWatcher>? _watchers;

        public static ConcurrentDictionary<string, bool>? _reloadShadersQueued = new();

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var resC = _res;
            if (args.Length == 1)
            {
                if (args[0] == "+watch")
                {
                    if (_watchers != null)
                    {
                        shell.WriteLine("Already watching.");
                        return;
                    }

                    _watchers = new Dictionary<string, FileSystemWatcher>();

                    var stringComparer = PathHelpers.IsFileSystemCaseSensitive()
                        ? StringComparer.Ordinal
                        : StringComparer.OrdinalIgnoreCase;

                    var reversePathResolution = new ConcurrentDictionary<string, HashSet<ResourcePath>>(stringComparer);

                    var taskManager = _taskManager;

                    var shaderCount = 0;
                    var created = 0;
                    var dirs = new ConcurrentDictionary<string, SortedSet<string>>(stringComparer);
                    foreach (var (path, src) in resC.GetAllResources<ShaderSourceResource>())
                    {
                        if (!resC.TryGetDiskFilePath(path, out var fullPath))
                        {
                            throw new NotImplementedException();
                        }

                        reversePathResolution.GetOrAdd(fullPath, _ => new HashSet<ResourcePath>()).Add(path);

                        var dir = Path.GetDirectoryName(fullPath)!;
                        var fileName = Path.GetFileName(fullPath);
                        dirs.GetOrAdd(dir, _ => new SortedSet<string>(stringComparer))
                            .Add(fileName);

                        foreach (var inc in src.ParsedShader.Includes)
                        {
                            if (!resC.TryGetDiskFilePath(inc, out var incFullPath))
                            {
                                throw new NotImplementedException();
                            }

                            reversePathResolution.GetOrAdd(incFullPath, _ => new HashSet<ResourcePath>()).Add(path);

                            var incDir = Path.GetDirectoryName(incFullPath)!;
                            var incFileName = Path.GetFileName(incFullPath);
                            dirs.GetOrAdd(incDir, _ => new SortedSet<string>(stringComparer))
                                .Add(incFileName);
                        }

                        ++shaderCount;
                    }

                    foreach (var (dir, files) in dirs)
                    {
                        if (_watchers.TryGetValue(dir, out var watcher))
                        {
                            throw new NotImplementedException();
                        }

                        watcher = new FileSystemWatcher(dir);
                        watcher.Changed += (_, ev) =>
                        {
                            if (_reloadShadersQueued!.TryAdd(ev.FullPath, true))
                            {
                                taskManager.RunOnMainThread(() =>
                                {
                                    var resPaths = reversePathResolution[ev.FullPath];
                                    foreach (var resPath in resPaths)
                                    {
                                        try
                                        {
                                            resC.ReloadResource<ShaderSourceResource>(resPath);
                                            shell.WriteLine($"Reloaded shader: {resPath}");
                                        }
                                        catch (Exception)
                                        {
                                            shell.WriteLine($"Failed to reload shader: {resPath}");
                                        }

                                        _reloadShadersQueued.TryRemove(ev.FullPath, out var _);
                                    }
                                });
                            }
                        };

                        foreach (var file in files)
                        {
                            watcher.Filters.Add(file);
                        }

                        watcher.EnableRaisingEvents = true;

                        _watchers.Add(dir, watcher);
                        ++created;
                    }

                    shell.WriteLine($"Created {created} shader directory watchers for {shaderCount} shaders.");

                    return;
                }

                if (args[0] == "-watch")
                {
                    if (_watchers == null)
                    {
                        shell.WriteLine("No shader directory watchers active.");
                        return;
                    }

                    var disposed = 0;
                    foreach (var (_, watcher) in _watchers)
                    {
                        ++disposed;
                        watcher.Dispose();
                    }

                    _watchers = null;

                    shell.WriteLine($"Disposed of {disposed} shader directory watchers.");

                    return;
                }
            }

            if (args.Length > 1)
            {
                shell.WriteLine("Not implemented.");
                return;
            }

            shell.WriteLine("Reloading content shader resources...");

            foreach (var (path, _) in resC.GetAllResources<ShaderSourceResource>())
            {
                try
                {
                    resC.ReloadResource<ShaderSourceResource>(path);
                }
                catch (Exception)
                {
                    shell.WriteLine($"Failed to reload shader: {path}");
                }
            }

            shell.WriteLine("Done.");
        }
    }

    internal sealed class ClydeDebugLayerCommand : LocalizedCommands
    {
        [Dependency] private readonly IClydeInternal _clyde = default!;

        public override string Command => "cldbglyr";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                _clyde.DebugLayers = ClydeDebugLayers.None;
                return;
            }

            _clyde.DebugLayers = args[0] switch
            {
                "fov" => ClydeDebugLayers.Fov,
                "light" => ClydeDebugLayers.Light,
                _ => ClydeDebugLayers.None
            };
        }
    }

    internal sealed class GetKeyInfoCommand : LocalizedCommands
    {
        [Dependency] private readonly IClydeInternal _clyde = default!;

        public override string Command => "keyinfo";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine(Help);
                return;
            }

            if (Enum.TryParse(typeof(Keyboard.Key), args[0], true, out var parsed))
            {
                var key = (Keyboard.Key)parsed!;

                var name = _clyde.GetKeyName(key);

                shell.WriteLine($"name: '{name}' ");
            }
        }
    }
}
