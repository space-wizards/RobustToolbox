using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Robust.Client.Input;
using System.Threading;
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
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Asynchronous;
using Robust.Shared.ContentPack;
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
                console.AddLine($"entity {e.Uid}, {e.Prototype?.ID}, {e.Transform.Coordinates}.", Color.White);
            }

            return false;
        }
    }

    internal class GetComponentRegistrationCommand : IConsoleCommand
    {
        public string Command => "getcomponentregistration";
        public string Help => "Usage: getcomponentregistration <componentName>";
        public string Description => "Gets component registration information";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            if (args.Length < 1)
            {
                console.AddLine(Help);
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
            "Usage: monitor <name>\nPossible monitors are: fps, net, bandwidth, coord, time, frames, mem, clyde, input";

        public string Description => "Toggles a debug monitor in the F3 menu.";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var monitor = IoCManager.Resolve<IUserInterfaceManager>().DebugMonitors;

            if (args.Length != 1)
            {
                console.AddLine(Help);
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
                case "bandwidth":
                    monitor.ShowNetBandwidth ^= true;
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
        public string Help => "Usage: showrays <raylifetime>";
        public string Description => "Toggles debug drawing of physics rays. An integer for <raylifetime> must be provided";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            if (args.Length != 1)
            {
                console.AddLine(Help);
                return false;
            }

            if (!int.TryParse(args[0], out var id))
            {
                console.AddLine($"{args[0]} is not a valid integer.",Color.Red);
                return false;
            }

            var mgr = IoCManager.Resolve<IDebugDrawingManager>();
            mgr.DebugDrawRays = !mgr.DebugDrawRays;
            console.AddLine("Toggled showing rays to:" + mgr.DebugDrawRays.ToString(), Color.Green);
            mgr.DebugRayLifetime = TimeSpan.FromSeconds((double)int.Parse(args[0], CultureInfo.InvariantCulture));
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
                console.AddLine(Help);
                return false;
            }

            if ((!new Regex(@"^c?[0-9]+$").IsMatch(args[0])))
            {
                console.AddLine("Malformed UID", Color.Red);
                return false;
            }

            var uid = EntityUid.Parse(args[0]);
            var entmgr = IoCManager.Resolve<IEntityManager>();
            if (!entmgr.TryGetEntity(uid, out var entity))
            {
                console.AddLine("That entity does not exist. Sorry lad.", Color.Red);
                return false;
            }

            console.AddLine($"{entity.Uid}: {entity.Prototype?.ID}/{entity.Name}");
            console.AddLine($"init/del/lmt: {entity.Initialized}/{entity.Deleted}/{entity.LastModifiedTick}");
            foreach (var component in entity.GetAllComponents())
            {
                console.AddLine(component.ToString() ?? "");
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
        public string Help => "sggcell <gridID> <vector2i> [offset]\nThat vector2i param is in the form x<int>,y<int>.";
        public string Description => "Lists entities on a snap grid cell.";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            if (args.Length != 2 && args.Length != 3)
            {
                console.AddLine(Help);
                return false;
            }

            string gridId = args[0];
            string indices = args[1];
            string offset = args.Length == 3 ? args[2] : "Center";

            if (!int.TryParse(args[0], out var id))
            {
                console.AddLine($"{args[0]} is not a valid integer.",Color.Red);
                return false;
            }

            if (!new Regex(@"^-?[0-9]+,-?[0-9]+$").IsMatch(indices))
            {
                console.AddLine("mapIndicies must be of form x<int>,y<int>", Color.Red);
                return false;
            }

            SnapGridOffset selectedOffset;
            if (Enum.IsDefined(typeof(SnapGridOffset), offset))
            {
                    selectedOffset = (SnapGridOffset)Enum.Parse(typeof(SnapGridOffset), offset);
            }
            else
            {
                console.AddLine("given offset type is not defined", Color.Red);
                return false;
            }

            var mapMan = IoCManager.Resolve<IMapManager>();

            if (mapMan.GridExists(new GridId(int.Parse(gridId, CultureInfo.InvariantCulture))))
            {
                foreach (var entity in
                    mapMan.GetGrid(new GridId(int.Parse(gridId, CultureInfo.InvariantCulture))).GetSnapGridCell(
                        new Vector2i(
                            int.Parse(indices.Split(',')[0], CultureInfo.InvariantCulture),
                            int.Parse(indices.Split(',')[1], CultureInfo.InvariantCulture)),
                        selectedOffset))
                {
                    console.AddLine(entity.Owner.Uid.ToString());
                }
            }
            else
            {
                console.AddLine("grid does not exist", Color.Red);
                return false;
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
            if (args.Length < 1)
            {
                console.AddLine(Help);
                return false;
            }
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
            if (args.Length < 2)
            {
                console.AddLine(Help);
                return false;
            }
            var resourceCache = IoCManager.Resolve<IResourceCache>();
            var reflection = IoCManager.Resolve<IReflectionManager>();
            Type type;

            try
            {
                type = reflection.LooseGetType(args[1]);
            }
            catch(ArgumentException)
            {
                console.AddLine("Unable to find type", Color.Red);
                return false;
            }

            var getResourceMethod =
                resourceCache
                    .GetType()
                    .GetMethod("GetResource", new[] { typeof(string), typeof(bool) });
            DebugTools.Assert(getResourceMethod != null);
            var generic = getResourceMethod!.MakeGenericMethod(type);
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
            if (args.Length < 2)
            {
                console.AddLine(Help);
                return false;
            }
            var resourceCache = IoCManager.Resolve<IResourceCache>();
            var reflection = IoCManager.Resolve<IReflectionManager>();

            Type type;
            try
            {
                type = reflection.LooseGetType(args[1]);
            }
            catch(ArgumentException)
            {
                console.AddLine("Unable to find type", Color.Red);
                return false;
            }

            var getResourceMethod = resourceCache.GetType().GetMethod("ReloadResource", new[] { typeof(string) });
            DebugTools.Assert(getResourceMethod != null);
            var generic = getResourceMethod!.MakeGenericMethod(type);
            generic.Invoke(resourceCache, new object[] { args[0] });
            return false;
        }
    }

    internal class GridTileCount : IConsoleCommand
    {
        public string Command => "gridtc";
        public string Description => "Gets the tile count of a grid";
        public string Help => "Usage: gridtc <gridId>";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            if (args.Length != 1)
            {
                console.AddLine(Help);
                return false;
            }

            if (!int.TryParse(args[0], out var id))
            {
                console.AddLine($"{args[0]} is not a valid integer.");
                return false;
            }

            var gridId = new GridId(int.Parse(args[0]));
            var mapManager = IoCManager.Resolve<IMapManager>();

            if (mapManager.TryGetGrid(gridId, out var grid))
            {
                console.AddLine(mapManager.GetGrid(gridId).GetAllTiles().Count().ToString());
                return false;
            }
            else
            {
                console.AddLine($"No grid exists with id {id}",Color.Red);
                return false;
            }
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

            using (var stream = res.UserData.Create(new ResourcePath("/guidump.txt")))
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
            var window = new SS14Window { CustomMinimumSize = (500, 400)};
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
            var vBoxRadioButtons = new VBoxContainer();
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

            tabContainer.AddChild(new VBoxContainer
            {
                Name = "Slider",
                Children =
                {
                    new Slider()
                }
            });

            window.OpenCentered();

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
            console.AddLine(mgr.GetText());
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
            if (!mgr.LockConsoleAccess)
                mgr.Enabled = !mgr.Enabled;
            return false;
        }
    }

    internal class ToggleFOV : IConsoleCommand
    {
        public string Command => "togglefov";
        public string Description => "Toggles fov for client.";
        public string Help => "togglefov";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var lmgr = IoCManager.Resolve<ILightManager>();
            var mgr = IoCManager.Resolve<IEyeManager>();
            if (!lmgr.LockConsoleAccess)
                if (mgr.CurrentEye != null)
                    mgr.CurrentEye.DrawFov = !mgr.CurrentEye.DrawFov;
            return false;
        }
    }

    internal class ToggleHardFOV : IConsoleCommand
    {
        public string Command => "togglehardfov";
        public string Description => "Toggles hard fov for client (for debugging space-station-14#2353).";
        public string Help => "togglehardfov";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var mgr = IoCManager.Resolve<ILightManager>();
            if (!mgr.LockConsoleAccess)
                mgr.DrawHardFov = !mgr.DrawHardFov;
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
            if (!mgr.LockConsoleAccess)
                mgr.DrawShadows = !mgr.DrawShadows;
            return false;
        }
    }
    internal class ToggleLightBuf : IConsoleCommand
    {
        public string Command => "togglelightbuf";
        public string Description => "Toggles lighting rendering. This includes shadows but not FOV.";
        public string Help => "togglelightbuf";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var mgr = IoCManager.Resolve<ILightManager>();
            if (!mgr.LockConsoleAccess)
                mgr.DrawLighting = !mgr.DrawLighting;
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
                    console.AddLine("Failed to parse argument.",Color.Red);
            }

            return false;
        }
    }

    internal class GcFullCommand : IConsoleCommand
    {
        public string Command => "gcf";
        public string Description => "Run the GC, fully, compacting LOH and everything.";
        public string Help => "gcf";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(2, GCCollectionMode.Forced, true, true);

            return false;
        }
    }

    internal class GcModeCommand : IConsoleCommand
    {

        public string Command => "gc_mode";

        public string Description => "Change/Read the GC Latency mode.";

        public string Help => "gc_mode\nSee current GC Latencymode\ngc_mode [type]\n Change GC Latency mode to [type]";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var prevMode = GCSettings.LatencyMode;
            if (args.Length == 0)
            {
                console.AddLine($"current gc latency mode: {(int) prevMode} ({prevMode})");
                console.AddLine("possible modes:");
                foreach (int mode in (int[]) Enum.GetValues(typeof(GCLatencyMode)))
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

            var mousePos = eyeMan.ScreenToMap(inputMan.MouseScreenPosition);

            if (!mapMan.TryFindGridAt(mousePos, out var grid))
            {
                console.AddLine("No grid under your mouse cursor.");
                return false;
            }

            var internalGrid = (IMapGridInternal)grid;

            var chunkIndex = grid.LocalToChunkIndices(grid.MapToGrid(mousePos));
            var chunk = internalGrid.GetChunk(chunkIndex);

            console.AddLine($"worldBounds: {chunk.CalcWorldBounds()} localBounds: {chunk.CalcLocalBounds()}");
            return false;
        }
    }

    internal class ReloadShadersCommand : IConsoleCommand
    {

        public string Command => "rldshader";

        public string Description => "Reloads all shaders";

        public string Help => "rldshader";

        public static Dictionary<string, FileSystemWatcher>? _watchers;

        public static ConcurrentDictionary<string, bool>? _reloadShadersQueued = new();

        public bool Execute(IDebugConsole console, params string[] args)
        {
            IResourceCacheInternal resC;
            if (args.Length == 1)
            {
                if (args[0] == "+watch")
                {
                    if (_watchers != null)
                    {
                        console.AddLine("Already watching.");
                        return false;
                    }
                    resC = IoCManager.Resolve<IResourceCacheInternal>();

                    _watchers = new Dictionary<string, FileSystemWatcher>();

                    var stringComparer = PathHelpers.IsFileSystemCaseSensitive()
                        ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

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
                                            console.AddLine($"Reloaded shader: {resPath}");
                                        }
                                        catch (Exception)
                                        {
                                            console.AddLine($"Failed to reload shader: {resPath}");
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

                    console.AddLine($"Created {created} shader directory watchers for {shaderCount} shaders.");

                    return false;
                }

                if (args[0] == "-watch")
                {
                    if (_watchers == null)
                    {
                        console.AddLine("No shader directory watchers active.");
                        return false;
                    }

                    var disposed = 0;
                    foreach (var (_, watcher) in _watchers)
                    {
                        ++disposed;
                        watcher.Dispose();
                    }

                    _watchers = null;

                    console.AddLine($"Disposed of {disposed} shader directory watchers.");

                    return false;
                }
            }

            if (args.Length > 1)
            {
                console.AddLine("Not implemented.");
                return false;
            }

            console.AddLine("Reloading content shader resources...");

            resC = IoCManager.Resolve<IResourceCacheInternal>();

            foreach (var (path, _) in resC.GetAllResources<ShaderSourceResource>())
            {
                try
                {
                    resC.ReloadResource<ShaderSourceResource>(path);
                }
                catch (Exception)
                {
                    console.AddLine($"Failed to reload shader: {path}");
                }
            }

            console.AddLine("Done.");

            return false;
        }

    }

    internal class ClydeDebugLayerCommand : IConsoleCommand
    {
        public string Command => "cldbglyr";
        public string Description => "Toggle fov and light debug layers";
        public string Help => "cldbglyr <layer>: Toggle <layer>\ncldbglyr: Turn all Layers off";

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
        public string Description => "Keys key info for a key";
        public string Help => "keyinfo <Key>";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            if (args.Length != 1)
            {
                console.AddLine(Help);
                return false;
            }

            var clyde = IoCManager.Resolve<IClydeInternal>();

            if (Enum.TryParse(typeof(Keyboard.Key), args[0], true, out var parsed))
            {
                var key = (Keyboard.Key) parsed!;

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
