using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.Versioning;
using System.Text;
using Robust.Client.Input;
using Robust.Client.Interfaces;
using Robust.Client.Interfaces.Console;
using Robust.Client.Interfaces.Debugging;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics.Lighting;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Interfaces.State;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.ResourceManagement.ResourceTypes;
using Robust.Client.State.States;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.Console.Commands
{
    internal class DumpEntitiesCommand : IConsoleCommand
    {
        public string Command => "dumpentities";
        public string Help => "Dump entity list";
        public string Description => "Dumps entity list of UIDs and prototype.";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            foreach (var e in entityManager.GetEntities().OrderBy(e => e.Uid))
            {
                console.AddLine($"entity {e.Uid}, {e.Prototype?.ID}, {e.Transform.GridPosition}.", Color.White);
            }

            return false;
        }
    }

    internal class GetComponentRegistrationCommand : IConsoleCommand
    {
        public string Command => "getcomponentregistration";
        public string Help => "";
        public string Description => "";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            if (args.Length < 1)
            {
                console.AddLine("Not enough arguments.", Color.Red);
                return false;
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

                message.Append($", NSE: {registration.NetworkSynchronizeExistence}, references:");

                console.AddLine(message.ToString(), Color.White);

                foreach (var type in registration.References)
                {
                    console.AddLine($"  {type}", Color.White);
                }
            }
            catch (UnknownComponentException)
            {
                console.AddLine($"No registration found for '{args[0]}'", Color.Red);
            }

            return false;
        }
    }

    internal class ToggleMonitorCommand : IConsoleCommand
    {
        public string Command => "monitor";

        public string Help =>
            "Usage: monitor <name>\nPossible monitors are: fps, net, coord, time, frames, mem, clyde, input";

        public string Description => "Toggles a debug monitor in the F3 menu.";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var monitor = IoCManager.Resolve<IUserInterfaceManager>().DebugMonitors;

            if (args.Length != 1)
            {
                console.AddLine($"Must provide exactly 1 argument!");
                return false;
            }

            switch (args[0])
            {
                case "fps":
                    monitor.ShowFPS ^= true;
                    break;
                case "net":
                    monitor.ShowNet ^= true;
                    break;
                case "coord":
                    monitor.ShowCoords ^= true;
                    break;
                case "time":
                    monitor.ShowTime ^= true;
                    break;
                case "frames":
                    monitor.ShowFrameGraph ^= true;
                    break;
                case "mem":
                    monitor.ShowMemory ^= true;
                    break;
                case "clyde":
                    monitor.ShowClyde ^= true;
                    break;
                case "input":
                    monitor.ShowInput ^= true;
                    break;
                default:
                    console.AddLine($"Invalid key: {args[0]}");
                    break;
            }

            return false;
        }
    }

    internal class ExceptionCommand : IConsoleCommand
    {
        public string Command => "fuck";
        public string Help => "Throws an exception";
        public string Description => "Throws an exception";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            throw new InvalidOperationException("Fuck");
        }
    }

    internal class ShowBoundingBoxesCommand : IConsoleCommand
    {
        public string Command => "showbb";
        public string Help => "";
        public string Description => "Enables debug drawing over all bounding boxes in the game, showing their size.";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var mgr = IoCManager.Resolve<IDebugDrawing>();
            mgr.DebugColliders = !mgr.DebugColliders;
            return false;
        }
    }

    internal class ShowPositionsCommand : IConsoleCommand
    {
        public string Command => "showpos";
        public string Help => "";
        public string Description => "Enables debug drawing over all entity positions in the game.";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var mgr = IoCManager.Resolve<IDebugDrawing>();
            mgr.DebugPositions = !mgr.DebugPositions;
            return false;
        }
    }

    internal class ShowRayCommand : IConsoleCommand
    {
        public string Command => "showrays";
        public string Help => "showrays <raylifetime>";
        public string Description => "Toggles debug drawing of physics rays.";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            if (args.Length != 1)
            {
                console.AddLine("Must specify ray lifetime.", Color.Red);
                return false;
            }
            var mgr = IoCManager.Resolve<IDebugDrawingManager>();
            mgr.DebugDrawRays = !mgr.DebugDrawRays;
            console.AddLine("Toggled showing rays to:" + mgr.DebugDrawRays.ToString(), Color.Green);
            var indexSplit = args[0].Split(',');
            mgr.DebugRayLifetime = TimeSpan.FromSeconds((double)int.Parse(indexSplit[0], CultureInfo.InvariantCulture));
            return false;
        }
    }

    internal class DisconnectCommand : IConsoleCommand
    {
        public string Command => "disconnect";
        public string Help => "";
        public string Description => "Immediately disconnect from the server and go back to the main menu.";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            IoCManager.Resolve<IClientNetManager>().ClientDisconnect("Disconnect command used.");
            IoCManager.Resolve<IStateManager>().RequestStateChange<MainScreen>();
            return false;
        }
    }

    internal class EntityInfoCommand : IConsoleCommand
    {
        public string Command => "entfo";

        public string Help =>
            "entfo <entityuid>\nThe entity UID can be prefixed with 'c' to convert it to a client entity UID.";

        public string Description => "Displays verbose diagnostics for an entity.";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            if (args.Length != 1)
            {
                console.AddLine("Must pass exactly 1 argument", Color.Red);
                return false;
            }

            var uid = EntityUid.Parse(args[0]);
            var entmgr = IoCManager.Resolve<IEntityManager>();
            if (!entmgr.TryGetEntity(uid, out var entity))
            {
                console.AddLine("That entity does not exist. Sorry lad.", Color.Red);
                return false;
            }

            console.AddLine($"{entity.Uid}: {entity.Prototype.ID}/{entity.Name}");
            console.AddLine($"init/del/lmt: {entity.Initialized}/{entity.Deleted}/{entity.LastModifiedTick}");
            foreach (var component in entity.GetAllComponents())
            {
                console.AddLine(component.ToString());
                if (component is IComponentDebug debug)
                {
                    foreach (var line in debug.GetDebugString().Split('\n'))
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        console.AddLine("\t" + line);
                    }
                }
            }

            return false;
        }
    }

    internal class SnapGridGetCell : IConsoleCommand
    {
        public string Command => "sggcell";
        public string Help => "sggcell <gridID> <mapIndices> [offset]\nThat mapindices param is in the form x,y.";
        public string Description => "Lists entities on a snap grid cell.";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            if (args.Length != 2 && args.Length != 3)
            {
                console.AddLine("Must pass exactly 2 or 3 arguments", Color.Red);
                return false;
            }

            var gridID = new GridId(int.Parse(args[0], CultureInfo.InvariantCulture));
            var indexSplit = args[1].Split(',');
            var x = int.Parse(indexSplit[0], CultureInfo.InvariantCulture);
            var y = int.Parse(indexSplit[1], CultureInfo.InvariantCulture);
            var indices = new MapIndices(x, y);
            var offset = SnapGridOffset.Center;
            if (args.Length == 3)
            {
                offset = (SnapGridOffset)Enum.Parse(typeof(SnapGridOffset), args[2]);
            }

            var mapMan = IoCManager.Resolve<IMapManager>();
            var grid = mapMan.GetGrid(gridID);
            foreach (var entity in grid.GetSnapGridCell(indices, offset))
            {
                console.AddLine(entity.Owner.Uid.ToString());
            }

            return false;
        }
    }

    internal class SetPlayerName : IConsoleCommand
    {
        public string Command => "overrideplayername";
        public string Description => "Changes the name used when attempting to connect to the server.";
        public string Help => Command + " <name>";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var client = IoCManager.Resolve<IBaseClient>();
            client.PlayerNameOverride = args[0];

            console.AddLine($"Overriding player name to \"{args[0]}\".", Color.White);

            return false;
        }
    }

    internal class LoadResource : IConsoleCommand
    {
        public string Command => "ldrsc";
        public string Description => "Pre-caches a resource.";
        public string Help => "ldrsc <path> <type>";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var resourceCache = IoCManager.Resolve<IResourceCache>();
            var reflection = IoCManager.Resolve<IReflectionManager>();

            var type = reflection.LooseGetType(args[1]);
            var getResourceMethod =
                resourceCache.GetType().GetMethod("GetResource", new[] { typeof(string), typeof(bool) });
            DebugTools.Assert(getResourceMethod != null);
            var generic = getResourceMethod.MakeGenericMethod(type);
            generic.Invoke(resourceCache, new object[] { args[0], true });
            return false;
        }
    }

    internal class ReloadResource : IConsoleCommand
    {
        public string Command => "rldrsc";
        public string Description => "Reloads a resource.";
        public string Help => "rldrsc <path> <type>";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var resourceCache = IoCManager.Resolve<IResourceCache>();
            var reflection = IoCManager.Resolve<IReflectionManager>();

            var type = reflection.LooseGetType(args[1]);
            var getResourceMethod = resourceCache.GetType().GetMethod("ReloadResource", new[] { typeof(string) });
            DebugTools.Assert(getResourceMethod != null);
            var generic = getResourceMethod.MakeGenericMethod(type);
            generic.Invoke(resourceCache, new object[] { args[0] });
            return false;
        }
    }

    internal class GridTileCount : IConsoleCommand
    {
        public string Command => "gridtc";
        public string Description => "Gets the tile count of a grid";
        public string Help => "gridtc <gridId>";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var gridId = new GridId(int.Parse(args[0]));
            var grid = IoCManager.Resolve<IMapManager>().GetGrid(gridId);

            console.AddLine(grid.GetAllTiles().Count().ToString());
            return false;
        }
    }

    internal class GuiDumpCommand : IConsoleCommand
    {
        public string Command => "guidump";
        public string Description => "Dump GUI tree to /guidump.txt in user data.";
        public string Help => "guidump";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var root = IoCManager.Resolve<IUserInterfaceManager>().RootControl;
            var res = IoCManager.Resolve<IResourceManager>();

            using (var stream = res.UserData.Open(new ResourcePath("/guidump.txt"), FileMode.Create))
            using (var writer = new StreamWriter(stream, EncodingHelpers.UTF8))
            {
                _writeNode(root, 0, writer);
            }

            return false;
        }

        private static void _writeNode(Control control, int indents, TextWriter writer)
        {
            var indentation = new string(' ', indents * 2);
            writer.WriteLine("{0}{1}", indentation, control);
            foreach (var (key, value) in _propertyValuesFor(control))
            {
                writer.WriteLine("{2} * {0}: {1}", key, value, indentation);
            }

            foreach (var child in control.Children)
            {
                _writeNode(child, indents + 1, writer);
            }
        }

        private static List<(string, string)> _propertyValuesFor(Control control)
        {
            var members = new List<(string, string)>();
            var type = control.GetType();

            foreach (var fieldInfo in type.GetAllFields())
            {
                if (fieldInfo.GetCustomAttribute<ViewVariablesAttribute>() == null)
                {
                    continue;
                }

                members.Add((fieldInfo.Name, fieldInfo.GetValue(control)?.ToString() ?? "null"));
            }

            foreach (var propertyInfo in type.GetAllProperties())
            {
                if (propertyInfo.GetCustomAttribute<ViewVariablesAttribute>() == null)
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

    internal class UITestCommand : IConsoleCommand
    {
        public string Command => "uitest";
        public string Description => "Open a dummy UI testing window";
        public string Help => "uitest";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var window = new SS14Window();
            var tabContainer = new TabContainer();
            window.Contents.AddChild(tabContainer);
            var scroll = new ScrollContainer();
            tabContainer.AddChild(scroll);
            //scroll.SetAnchorAndMarginPreset(Control.LayoutPreset.Wide);
            var vBox = new VBoxContainer();
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

            var tree = new Tree { SizeFlagsVertical = Control.SizeFlags.FillExpand };
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
                        CustomMinimumSize = (50, 50),
                        Text = $"{x}, {y}"
                    });
                }
            }

            var group = new ButtonGroup();
            var vBoxRadioButtons = new VBoxContainer { Name = "Radio Buttons" };
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

            tabContainer.AddChild(new VBoxContainer
            {
                Name = "Slider",
                Children =
                {
                    new Slider()
                }
            });

            window.OpenCenteredMinSize();

            return false;
        }
    }

    internal class SetClipboardCommand : IConsoleCommand
    {
        public string Command => "setclipboard";
        public string Description => "Sets the system clipboard";
        public string Help => "setclipboard <text>";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var mgr = IoCManager.Resolve<IClipboardManager>();
            mgr.SetText(args[0]);
            return false;
        }
    }

    internal class GetClipboardCommand : IConsoleCommand
    {
        public string Command => "getclipboard";
        public string Description => "Gets the system clipboard";
        public string Help => "getclipboard";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var mgr = IoCManager.Resolve<IClipboardManager>();
            return false;
        }
    }

    internal class ToggleLight : IConsoleCommand
    {
        public string Command => "togglelight";
        public string Description => "Toggles light rendering.";
        public string Help => "togglelight";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var mgr = IoCManager.Resolve<ILightManager>();
            mgr.Enabled = !mgr.Enabled;
            return false;
        }
    }

    internal class ToggleShadows : IConsoleCommand
    {
        public string Command => "toggleshadows";
        public string Description => "Toggles shadow rendering.";
        public string Help => "toggleshadows";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var mgr = IoCManager.Resolve<ILightManager>();
            mgr.DrawShadows = !mgr.DrawShadows;
            return false;
        }
    }

    internal class GcCommand : IConsoleCommand
    {
        public string Command => "gc";
        public string Description => "Run the GC.";
        public string Help => "gc [generation]";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            if (args.Length == 0)
            {
                GC.Collect();
            }
            else
            {
                if (int.TryParse(args[0], out int result))
                    GC.Collect(result);
                else
                    console.AddLine("Failed to parse argument.");
            }

            return false;
        }
    }

    internal class GcModeCommand : IConsoleCommand
    {

        public string Command => "gc_mode";

        public string Description => "Change the GC Latency mode.";

        public string Help => "gc_mode [type]";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var prevMode = GCSettings.LatencyMode;
            if (args.Length == 0)
            {
                console.AddLine($"current gc latency mode: {(int) prevMode} ({prevMode})");
                console.AddLine("possible modes:");
                foreach (int mode in Enum.GetValues(typeof(GCLatencyMode)))
                {
                    console.AddLine($" {mode}: {Enum.GetName(typeof(GCLatencyMode), mode)}");
                }
            }
            else
            {
                GCLatencyMode mode;
                if (char.IsDigit(args[0][0]) && int.TryParse(args[0], out var modeNum))
                {
                    mode = (GCLatencyMode) modeNum;
                }
                else if (!Enum.TryParse(args[0], true, out mode))
                {
                    console.AddLine($"unknown gc latency mode: {args[0]}");
                    return false;
                }

                console.AddLine($"attempting gc latency mode change: {(int) prevMode} ({prevMode}) -> {(int) mode} ({mode})");
                GCSettings.LatencyMode = mode;
                console.AddLine($"resulting gc latency mode: {(int) GCSettings.LatencyMode} ({GCSettings.LatencyMode})");
            }

            return false;
        }

    }

    internal class SerializeStatsCommand : IConsoleCommand
    {

        public string Command => "szr_stats";

        public string Description => "Report serializer statistics.";

        public string Help => "szr_stats";

        public bool Execute(IDebugConsole console, params string[] args)
        {

            console.AddLine($"serialized: {RobustSerializer.BytesSerialized} bytes, {RobustSerializer.ObjectsSerialized} objects");
            console.AddLine($"largest serialized: {RobustSerializer.LargestObjectSerializedBytes} bytes, {RobustSerializer.LargestObjectSerializedType} objects");
            console.AddLine($"deserialized: {RobustSerializer.BytesDeserialized} bytes, {RobustSerializer.ObjectsDeserialized} objects");
            console.AddLine($"largest serialized: {RobustSerializer.LargestObjectDeserializedBytes} bytes, {RobustSerializer.LargestObjectDeserializedType} objects");

            return false;
        }

    }

    internal class ChunkInfoCommand : IConsoleCommand
    {
        public string Command => "chunkinfo";
        public string Description => "Gets info about a chunk under your mouse cursor.";
        public string Help => Command;

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var mapMan = IoCManager.Resolve<IMapManager>();
            var inputMan = IoCManager.Resolve<IInputManager>();
            var eyeMan = IoCManager.Resolve<IEyeManager>();

            var mousePos = eyeMan.ScreenToWorld(inputMan.MouseScreenPosition);

            var grid = (IMapGridInternal)mapMan.GetGrid(mousePos.GridID);

            var chunkIndex = grid.LocalToChunkIndices(mousePos);
            var chunk = grid.GetChunk(chunkIndex);

            console.AddLine($"worldBounds: {chunk.CalcWorldBounds()} localBounds: {chunk.CalcLocalBounds()}");
            return false;
        }
    }

    internal class ReloadShaders : IConsoleCommand
    {
        public string Command => "rldshader";
        public string Description => "Reloads all shaders";
        public string Help => "rldshader";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var resC = IoCManager.Resolve<IResourceCache>();

            var paths = resC.GetAllResources<ShaderSourceResource>().Select(p => p.Key).ToList();

            foreach (var path in paths)
            {
                resC.ReloadResource<ShaderSourceResource>(path);
            }

            return false;
        }
    }

    internal class ClydeDebugLayerCommand : IConsoleCommand
    {
        public string Command => "cldbglyr";
        public string Description => "";
        public string Help => "cldbglyr";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var clyde = IoCManager.Resolve<IClydeInternal>();

            if (args.Length < 1)
            {
                clyde.DebugLayers = ClydeDebugLayers.None;
                return false;
            }

            clyde.DebugLayers = args[0] switch
            {
                "fov" => ClydeDebugLayers.Fov,
                "light" => ClydeDebugLayers.Light,
                _ => ClydeDebugLayers.None
            };

            return false;
        }
    }

    internal class GetKeyInfoCommand : IConsoleCommand
    {
        public string Command => "keyinfo";
        public string Description { get; }
        public string Help => "keyinfo <Key>";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            if (args.Length != 1)
            {
                console.AddLine("Expected one argument", Color.Red);
                return false;
            }

            var clyde = IoCManager.Resolve<IClydeInternal>();

            if (Enum.TryParse(typeof(Keyboard.Key), args[0], true, out var parsed))
            {
                var key = (Keyboard.Key) parsed;

                var name = clyde.GetKeyName(key);
                var scanCode = clyde.GetKeyScanCode(key);
                var nameScanCode = clyde.GetKeyNameScanCode(scanCode);

                console.AddLine($"name: '{name}' scan code: '{scanCode}' name via scan code: '{nameScanCode}'");
            }
            else if (int.TryParse(args[0], out var scanCode))
            {
                var nameScanCode = clyde.GetKeyNameScanCode(scanCode);
                console.AddLine($"name via scan code: '{nameScanCode}'");
            }

            return false;
        }
    }
}
