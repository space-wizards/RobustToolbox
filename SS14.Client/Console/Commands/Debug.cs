using OpenTK.Graphics;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.Console;
using SS14.Client.Interfaces.Debugging;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.UserInterface.CustomControls;
using SS14.Shared.Console;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Text;

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

    class DumpRenderables : IConsoleCommand
    {
        public string Command => "dumprenderables";
        public string Help => "Dump renderables list";
        public string Description => "Dumps renderables list with component type.";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var componentManager = IoCManager.Resolve<IComponentManager>();

            IEnumerable<IComponent> components = componentManager.GetComponents<ISpriteRenderableComponent>();

            foreach (var component in components)
            {
                console.AddLine($"{component.Owner.Uid}: {component.GetType()}", Color.White);
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
                console.AddLine($"Not enough arguments.", Color4.Red);
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

    class ShowFPSCommand : IConsoleCommand
    {
        public string Command => "fps";
        public string Help => "Toggles showing FPS.";
        public string Description => "Toggles the FPS counter.";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var mgr = IoCManager.Resolve<IUserInterfaceManager>();
            mgr.ShowFPS = !mgr.ShowFPS;
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

    class DebugCollidersCommand : IConsoleCommand
    {
        public string Command => "debugcolliders";
        public string Help => "";
        public string Description => "Enables debug drawing over all collidables in the game.";

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

            var window = new SS14Window();
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
}
