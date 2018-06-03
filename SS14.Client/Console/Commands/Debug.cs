using SS14.Client.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.Console;
using SS14.Client.Interfaces.Debugging;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.UserInterface.CustomControls;
using SS14.Client.Interfaces.State;
using SS14.Client.State.States;
using SS14.Shared.Console;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Interfaces.Network;

namespace SS14.Client.Console.Commands
{
    class DumpEntitiesCommand : IConsoleCommand
    {
        public string Command => "dumpentities";
        public string Help => "Dump entity list";
        public string Description => "Dumps entity list of UIDs and prototype.";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var entitymanager = IoCManager.Resolve<IEntityManager>();

            foreach (IEntity e in entitymanager.GetEntities(new ComponentEntityQuery()))
            {
                console.AddLine($"entity {e.Uid}, {e.Prototype.Name}.", ChatChannel.Default, Color.White);
            }

            return false;
        }
    }

    class GetComponentRegistrationCommand : IConsoleCommand
    {
        public string Command => "getcomponentregistration";
        public string Help => "";
        public string Description => "";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            if (args.Length < 1)
            {
                console.AddLine($"Not enough arguments.", Color.Red);
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

                foreach (Type type in registration.References)
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

    class ToggleMonitorCommand : IConsoleCommand
    {
        public string Command => "monitor";
        public string Help => "Usage: monitor <name>\nPossible monitors are: fps, net, coord";
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
            }

            return false;
        }
    }

    class ExceptionCommand : IConsoleCommand
    {
        public string Command => "fuck";
        public string Help => "Throws an exception";
        public string Description => "Throws an exception";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            throw new InvalidOperationException("Fuck");
        }
    }

    class ShowBoundingBoxesCommand : IConsoleCommand
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

    class SpawnWindowCommand : IConsoleCommand
    {
        public string Command => "spawnwindow";
        public string Help => "";
        public string Description => "";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var uiMgr = IoCManager.Resolve<IUserInterfaceManager>();

            var window = new EntitySpawnWindow();
            window.AddToScreen();
            return false;
        }
    }

    class DumpDeferredLightingCommand : IConsoleCommand
    {
        public string Command => "dumpdeferredlighting";
        public string Help => "";
        public string Description => "";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var viewport = IoCManager.Resolve<ISceneTreeHolder>().SceneTree.Root.GetNode("LightingViewport") as Godot.Viewport;
            var tex = viewport.GetTexture().GetData();
            tex.SavePng("res://deferredlightingdump.png");
            return false;
        }
    }

    class GetRootViewportTransformCommand : IConsoleCommand
    {
        public string Command => "rootvptransform";
        public string Help => "";
        public string Description => "";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var vp = IoCManager.Resolve<ISceneTreeHolder>().SceneTree.Root;
            console.AddLine($"canvas_transform: {vp.CanvasTransform}, global_canvas_transform: {vp.GlobalCanvasTransform}");
            return false;
        }
    }

    class DisconnectCommand : IConsoleCommand
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
}
