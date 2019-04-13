using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.Console;
using SS14.Client.Interfaces.Debugging;
using SS14.Client.Interfaces.Graphics;
using SS14.Client.Interfaces.Graphics.Lighting;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.Interfaces.State;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.State.States;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Client.UserInterface.CustomControls;
using SS14.Shared.Console;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Transform;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.Interfaces.Resources;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Prototypes;
using SS14.Shared.Utility;
using SS14.Shared.ViewVariables;

namespace SS14.Client.Console.Commands
{
    internal class DumpEntitiesCommand : IConsoleCommand
    {
        public string Command => "dumpentities";
        public string Help => "Dump entity list";
        public string Description => "Dumps entity list of UIDs and prototype.";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            foreach (var e in entityManager.GetEntities())
            {
                console.AddLine($"entity {e.Uid}, {e.Prototype.ID}, {e.Transform.GridPosition}.", ChatChannel.Default,
                    Color.White);
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
        public string Help => "Usage: monitor <name>\nPossible monitors are: fps, net, coord, time";
        public string Description => "Toggles a debug monitor in the F3 menu.";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            if (args.Length != 1)
            {
                throw new InvalidOperationException("Must have exactly 1 argument.");
            }

            var monitor = IoCManager.Resolve<IUserInterfaceManager>().DebugMonitors;

            switch (args[0])
            {
                case "fps":
                    monitor.ShowFPS = !monitor.ShowFPS;
                    break;
                case "net":
                    monitor.ShowNet = !monitor.ShowNet;
                    break;
                case "coord":
                    monitor.ShowCoords = !monitor.ShowCoords;
                    break;
                case "time":
                    monitor.ShowTime = !monitor.ShowTime;
                    break;
                case "frames":
                    monitor.ShowFrameGraph = !monitor.ShowFrameGraph;
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

    internal class SpawnWindowCommand : IConsoleCommand
    {
        public string Command => "spawnwindow";
        public string Help => "";
        public string Description => "";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var window = new EntitySpawnWindow(IoCManager.Resolve<IDisplayManager>(), IoCManager.Resolve<IPlacementManager>(), IoCManager.Resolve<IPrototypeManager>(), IoCManager.Resolve<IResourceCache>());
            window.AddToScreen();
            return false;
        }
    }

    internal class DumpDeferredLightingCommand : IConsoleCommand
    {
        public string Command => "dumpdeferredlighting";
        public string Help => "";
        public string Description => "";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var viewport =
                IoCManager.Resolve<ISceneTreeHolder>().SceneTree.Root.GetNode("LightingViewport") as Godot.Viewport;
            var tex = viewport.GetTexture().GetData();
            tex.SavePng("res://deferredlightingdump.png");
            return false;
        }
    }

    internal class GetRootViewportTransformCommand : IConsoleCommand
    {
        public string Command => "rootvptransform";
        public string Help => "";
        public string Description => "";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var vp = IoCManager.Resolve<ISceneTreeHolder>().SceneTree.Root;
            console.AddLine(
                $"canvas_transform: {vp.CanvasTransform}, global_canvas_transform: {vp.GlobalCanvasTransform}, size: {vp.Size}");
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
            foreach (var component in entity.GetAllComponents().Distinct())
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
                offset = (SnapGridOffset) Enum.Parse(typeof(SnapGridOffset), args[2]);
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
                resourceCache.GetType().GetMethod("GetResource", new[] {typeof(string), typeof(bool)});
            DebugTools.Assert(getResourceMethod != null);
            var generic = getResourceMethod.MakeGenericMethod(type);
            generic.Invoke(resourceCache, new object[] {args[0], true});
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
            var getResourceMethod = resourceCache.GetType().GetMethod("ReloadResource", new[] {typeof(string)});
            DebugTools.Assert(getResourceMethod != null);
            var generic = getResourceMethod.MakeGenericMethod(type);
            generic.Invoke(resourceCache, new object[] {args[0]});
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
            var window = new SS14Window(IoCManager.Resolve<IDisplayManager>(), "UITest");
            window.AddToScreen();
            var scroll = new ScrollContainer();
            window.Contents.AddChild(scroll);
            scroll.SetAnchorAndMarginPreset(Control.LayoutPreset.Wide);
            var vBox = new VBoxContainer();
            scroll.AddChild(vBox);

            var progressBar = new ProgressBar {MaxValue = 10, Value = 5};
            vBox.AddChild(progressBar);

            var optionButton = new OptionButton();
            optionButton.AddItem("Honk");
            optionButton.AddItem("Foo");
            optionButton.AddItem("Bar");
            optionButton.AddItem("Baz");
            optionButton.OnItemSelected += eventArgs => optionButton.SelectId(eventArgs.Id);
            vBox.AddChild(optionButton);

            var tree = new Tree {SizeFlagsVertical = Control.SizeFlags.FillExpand};
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
            if (!mgr.Available)
            {
                console.AddLine(mgr.NotAvailableReason, Color.Red);
                return false;
            }

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
            if (!mgr.Available)
            {
                console.AddLine(mgr.NotAvailableReason, Color.Red);
                return false;
            }

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
            mgr.Enabled = !mgr.Enabled;
            return false;
        }
    }
}
