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
        public override string Command => "dumpentities";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            foreach (var e in entityManager.GetEntities().OrderBy(e => e))
            {
                shell.WriteLine(
                    $"entity {e}, {entityManager.GetComponent<MetaDataComponent>(e).EntityPrototype?.ID}, {entityManager.GetComponent<TransformComponent>(e).Coordinates}.");
            }
        }
    }

    internal sealed class GetComponentRegistrationCommand : LocalizedCommands
    {
        public override string Command => "getcomponentregistration";


        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteLine(Help);
                return;
            }

            var componentFactory = IoCManager.Resolve<IComponentFactory>();

            try
            {
                var registration = componentFactory.GetRegistration(args[0]);

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
        public override string Command => "monitor";

        public string Help
        {
            get
            {
                var monitors = string.Join(", ", Enum.GetNames<DebugMonitor>());
                return Loc.GetString("cmd-monitor-help", ("monitors", monitors));
            }
        }

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var monitors = IoCManager.Resolve<IUserInterfaceManager>().DebugMonitors;

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

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
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
        public override string Command => "showpos";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var mgr = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<DebugDrawingSystem>();
            mgr.DebugPositions = !mgr.DebugPositions;
        }
    }

    internal sealed class ShowRayCommand : LocalizedCommands
    {
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

            var mgr = EntitySystem.Get<DebugRayDrawingSystem>();
            mgr.DebugDrawRays = !mgr.DebugDrawRays;
            shell.WriteError("Toggled showing rays to:" + mgr.DebugDrawRays);
            mgr.DebugRayLifetime = TimeSpan.FromSeconds(duration);
        }
    }

    internal sealed class DisconnectCommand : LocalizedCommands
    {
        public override string Command => "disconnect";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            IoCManager.Resolve<IClientNetManager>().ClientDisconnect("Disconnect command used.");
        }
    }

    internal sealed class EntityInfoCommand : LocalizedCommands
    {
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
            var entmgr = IoCManager.Resolve<IEntityManager>();
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

            var mapMan = IoCManager.Resolve<IMapManager>();
            if (mapMan.TryGetGrid(gridUid, out var grid))
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
        public override string Command => "overrideplayername";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteLine(Help);
                return;
            }

            var client = IoCManager.Resolve<IBaseClient>();
            client.PlayerNameOverride = args[0];

            shell.WriteLine($"Overriding player name to \"{args[0]}\".");
        }
    }

    internal sealed class LoadResource : LocalizedCommands
    {
        public override string Command => "ldrsc";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 2)
            {
                shell.WriteLine(Help);
                return;
            }

            var resourceCache = IoCManager.Resolve<IResourceCache>();
            var reflection = IoCManager.Resolve<IReflectionManager>();
            Type type;

            try
            {
                type = reflection.LooseGetType(args[1]);
            }
            catch (ArgumentException)
            {
                shell.WriteError("Unable to find type");
                return;
            }

            var getResourceMethod =
                resourceCache
                    .GetType()
                    .GetMethod("GetResource", new[] { typeof(string), typeof(bool) });
            DebugTools.Assert(getResourceMethod != null);
            var generic = getResourceMethod!.MakeGenericMethod(type);
            generic.Invoke(resourceCache, new object[] { args[0], true });
        }
    }

    internal sealed class ReloadResource : LocalizedCommands
    {
        public override string Command => "rldrsc";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 2)
            {
                shell.WriteLine(Help);
                return;
            }

            var resourceCache = IoCManager.Resolve<IResourceCache>();
            var reflection = IoCManager.Resolve<IReflectionManager>();

            Type type;
            try
            {
                type = reflection.LooseGetType(args[1]);
            }
            catch (ArgumentException)
            {
                shell.WriteError("Unable to find type");
                return;
            }

            var getResourceMethod = resourceCache.GetType().GetMethod("ReloadResource", new[] { typeof(string) });
            DebugTools.Assert(getResourceMethod != null);
            var generic = getResourceMethod!.MakeGenericMethod(type);
            generic.Invoke(resourceCache, new object[] { args[0] });
        }
    }

    internal sealed class GridTileCount : LocalizedCommands
    {
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

            var mapManager = IoCManager.Resolve<IMapManager>();
            if (mapManager.TryGetGrid(gridUid, out var grid))
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
        public override string Command => "guidump";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var uiMgr = IoCManager.Resolve<IUserInterfaceManager>();
            var res = IoCManager.Resolve<IResourceManager>();

            using var writer = res.UserData.OpenWriteText(new ResourcePath("/guidump.txt"));

            foreach (var root in uiMgr.AllRoots)
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

    internal sealed class UITestCommand : LocalizedCommands
    {
        public override string Command => "uitest";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var window = new DefaultWindow { MinSize = (500, 400) };
            var tabContainer = new TabContainer();
            window.Contents.AddChild(tabContainer);
            var scroll = new ScrollContainer();
            tabContainer.AddChild(scroll);
            //scroll.SetAnchorAndMarginPreset(Control.LayoutPreset.Wide);
            var vBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };
            scroll.AddChild(vBox);

            var progressBar = new ProgressBar { MaxValue = 10, Value = 5 };
            vBox.AddChild(progressBar);

            var optionButton = new OptionButton();
            optionButton.AddItem("Honk");
            optionButton.AddItem("Foo");
            optionButton.AddItem("Bar");
            optionButton.AddItem("Baz");
            optionButton.OnItemSelected += eventArgs => optionButton.SelectId(eventArgs.Id);
            vBox.AddChild(optionButton);

            var tree = new Tree { VerticalExpand = true };
            var root = tree.CreateItem();
            root.Text = "Honk!";
            var child = tree.CreateItem();
            child.Text = "Foo";
            for (var i = 0; i < 20; i++)
            {
                child = tree.CreateItem();
                child.Text = $"Bar {i}";
            }

            vBox.AddChild(tree);

            var rich = new RichTextLabel();
            var message = new FormattedMessage();
            message.AddText("Foo\n");
            message.PushColor(Color.Red);
            message.AddText("Bar");
            message.Pop();
            rich.SetMessage(message);
            vBox.AddChild(rich);

            var itemList = new ItemList();
            tabContainer.AddChild(itemList);
            for (var i = 0; i < 10; i++)
            {
                itemList.AddItem(i.ToString());
            }

            var grid = new GridContainer { Columns = 3 };
            tabContainer.AddChild(grid);
            for (var y = 0; y < 3; y++)
            {
                for (var x = 0; x < 3; x++)
                {
                    grid.AddChild(new Button
                    {
                        MinSize = (50, 50),
                        Text = $"{x}, {y}"
                    });
                }
            }

            var group = new ButtonGroup();
            var vBoxRadioButtons = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };
            for (var i = 0; i < 10; i++)
            {
                vBoxRadioButtons.AddChild(new Button
                {
                    Text = i.ToString(),
                    Group = group
                });

                // ftftftftftftft
            }

            tabContainer.AddChild(vBoxRadioButtons);

            TabContainer.SetTabTitle(vBoxRadioButtons, "Radio buttons!!");

            tabContainer.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Name = "Slider",
                Children =
                {
                    new Slider()
                }
            });

            tabContainer.AddChild(new SplitContainer
            {
                Orientation = SplitContainer.SplitOrientation.Horizontal,
                Children =
                {
                    new PanelContainer
                    {
                        PanelOverride = new StyleBoxFlat { BackgroundColor = Color.Red },
                        Children =
                        {
                            new Label { Text = "FOOBARBAZ" },
                        }
                    },
                    new PanelContainer
                    {
                        PanelOverride = new StyleBoxFlat { BackgroundColor = Color.Blue },
                        Children =
                        {
                            new Label { Text = "FOOBARBAZ" },
                        }
                    },
                }
            });

            window.OpenCentered();
        }
    }

    internal sealed class SetClipboardCommand : LocalizedCommands
    {
        public override string Command => "setclipboard";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var mgr = IoCManager.Resolve<IClipboardManager>();
            mgr.SetText(args[0]);
        }
    }

    internal sealed class GetClipboardCommand : LocalizedCommands
    {
        public override string Command => "getclipboard";

        public override async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var mgr = IoCManager.Resolve<IClipboardManager>();
            shell.WriteLine(await mgr.GetText());
        }
    }

    internal sealed class ToggleLight : LocalizedCommands
    {
        public override string Command => "togglelight";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var mgr = IoCManager.Resolve<ILightManager>();
            if (!mgr.LockConsoleAccess)
                mgr.Enabled = !mgr.Enabled;
        }
    }

    internal sealed class ToggleFOV : LocalizedCommands
    {
        public override string Command => "togglefov";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var mgr = IoCManager.Resolve<IEyeManager>();
            if (mgr.CurrentEye != null)
                mgr.CurrentEye.DrawFov = !mgr.CurrentEye.DrawFov;
        }
    }

    internal sealed class ToggleHardFOV : LocalizedCommands
    {
        public override string Command => "togglehardfov";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var mgr = IoCManager.Resolve<ILightManager>();
            if (!mgr.LockConsoleAccess)
                mgr.DrawHardFov = !mgr.DrawHardFov;
        }
    }

    internal sealed class ToggleShadows : LocalizedCommands
    {
        public override string Command => "toggleshadows";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var mgr = IoCManager.Resolve<ILightManager>();
            if (!mgr.LockConsoleAccess)
                mgr.DrawShadows = !mgr.DrawShadows;
        }
    }

    internal sealed class ToggleLightBuf : LocalizedCommands
    {
        public override string Command => "togglelightbuf";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var mgr = IoCManager.Resolve<ILightManager>();
            if (!mgr.LockConsoleAccess)
                mgr.DrawLighting = !mgr.DrawLighting;
        }
    }

    internal sealed class ChunkInfoCommand : LocalizedCommands
    {
        public override string Command => "chunkinfo";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var mapMan = IoCManager.Resolve<IMapManager>();
            var inputMan = IoCManager.Resolve<IInputManager>();
            var eyeMan = IoCManager.Resolve<IEyeManager>();

            var mousePos = eyeMan.ScreenToMap(inputMan.MouseScreenPosition);

            if (!mapMan.TryFindGridAt(mousePos, out var grid))
            {
                shell.WriteLine("No grid under your mouse cursor.");
                return;
            }

            var internalGrid = (IMapGridInternal)grid;

            var chunkIndex = grid.LocalToChunkIndices(grid.MapToGrid(mousePos));
            var chunk = internalGrid.GetChunk(chunkIndex);

            shell.WriteLine($"worldBounds: {internalGrid.CalcWorldAABB(chunk)} localBounds: {chunk.CachedBounds}");
        }
    }

    internal sealed class ReloadShadersCommand : LocalizedCommands
    {
        public override string Command => "rldshader";

        public static Dictionary<string, FileSystemWatcher>? _watchers;

        public static ConcurrentDictionary<string, bool>? _reloadShadersQueued = new();

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            IResourceCacheInternal resC;
            if (args.Length == 1)
            {
                if (args[0] == "+watch")
                {
                    if (_watchers != null)
                    {
                        shell.WriteLine("Already watching.");
                        return;
                    }

                    resC = IoCManager.Resolve<IResourceCacheInternal>();

                    _watchers = new Dictionary<string, FileSystemWatcher>();

                    var stringComparer = PathHelpers.IsFileSystemCaseSensitive()
                        ? StringComparer.Ordinal
                        : StringComparer.OrdinalIgnoreCase;

                    var reversePathResolution = new ConcurrentDictionary<string, HashSet<ResourcePath>>(stringComparer);

                    var taskManager = IoCManager.Resolve<ITaskManager>();

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
                                            IoCManager.Resolve<IResourceCache>()
                                                .ReloadResource<ShaderSourceResource>(resPath);
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

            resC = IoCManager.Resolve<IResourceCacheInternal>();

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
        public override string Command => "cldbglyr";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var clyde = IoCManager.Resolve<IClydeInternal>();

            if (args.Length < 1)
            {
                clyde.DebugLayers = ClydeDebugLayers.None;
                return;
            }

            clyde.DebugLayers = args[0] switch
            {
                "fov" => ClydeDebugLayers.Fov,
                "light" => ClydeDebugLayers.Light,
                _ => ClydeDebugLayers.None
            };
        }
    }

    internal sealed class GetKeyInfoCommand : LocalizedCommands
    {
        public override string Command => "keyinfo";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine(Help);
                return;
            }

            var clyde = IoCManager.Resolve<IClydeInternal>();

            if (Enum.TryParse(typeof(Keyboard.Key), args[0], true, out var parsed))
            {
                var key = (Keyboard.Key)parsed!;

                var name = clyde.GetKeyName(key);

                shell.WriteLine($"name: '{name}' ");
            }
        }
    }
}
