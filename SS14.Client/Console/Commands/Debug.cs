using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenTK.Graphics;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Render;
using SS14.Client.Interfaces.Console;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.State;
using SS14.Client.State.States;
using SS14.Shared;
using SS14.Shared.Console;
using SS14.Shared.ContentPack;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;

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
                console.AddLine($"entity {e.Uid}, {e.Prototype.Name}.", ChatChannel.Default, Color4.White);
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

            IEnumerable<IComponent> components = componentManager.GetComponents<ISpriteRenderableComponent>()
                                          .Cast<IComponent>()
                                          .Union(componentManager.GetComponents<ParticleSystemComponent>());

            foreach (var component in components)
            {
                console.AddLine($"{component.Owner.Uid}: {component.GetType()}", ChatChannel.Default, Color4.White);
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
                console.AddLine($"Not enough arguments.", ChatChannel.Default, Color4.Red);
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

                console.AddLine(message.ToString(), ChatChannel.Default, Color4.White);

                foreach (Type type in registration.References)
                {
                    console.AddLine($"  {type}", ChatChannel.Default, Color4.White);
                }
            }
            catch (UnknownComponentException)
            {
                console.AddLine($"No registration found for '{args[0]}'", ChatChannel.Default, Color4.Red);
            }

            return false;
        }
    }

    class ShowFPSCommand : IConsoleCommand
    {
        public string Command => "fps";
        public string Help => "Toggles showing FPS.";
        public string Description => "";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            CluwneLib.Debug.ToggleFPSDebug();
            return false;
        }
    }

    class GetRenderImageDumpCommand : IConsoleCommand
    {
        public string Command => "dumprt";
        public string Description => "Dump RenderTarget";
        public string Help => @"usage: dumprt <key>
Dumps the specified render target used in the drawing process by key.
The file gets dumped besides the executable.
List of valid keys: playerocclusion, occluderdebug, light, lightintermediate, composedscene, overlay, scene, tiles, screenshadows, shadowblendintermediate, shadowintermediate.";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            if (args.Length == 0)
            {
                console.AddLine("No key specified.", ChatChannel.Default, Color4.Red);
                return false;
            }
            if (args.Length > 1)
            {
                console.AddLine("This command only takes one argument.", ChatChannel.Default, Color4.Red);
                return false;
            }
            var statemgr = IoCManager.Resolve<IStateManager>();
            if (!(statemgr.CurrentState is GameScreen screen))
            {
                console.AddLine("Wrong game state active. Must be GameScreen", ChatChannel.Default, Color4.Red);
                return false;
            }
            RenderImage target;
            var key = args[0];
            switch (key) {
                case "playerocclusion":
                    target = screen.PlayerOcclusionTarget;
                    break;
                case "occluderdebug":
                    target = screen.OccluderDebugTarget;
                    break;
                case "light":
                    target = screen.LightTarget;
                    break;
                case "lightintermediate":
                    target = screen.LightTargetIntermediate;
                    break;
                case "composedscene":
                    target = screen.ComposedSceneTarget;
                    break;
                case "overlay":
                    target = screen.OverlayTarget;
                    break;
                case "scene":
                    target = screen.SceneTarget;
                    break;
                case "tiles":
                    target = screen.TilesTarget;
                    break;
                case "screenshadows":
                    target = screen.ScreenShadows;
                    break;
                case "shadowblendintermediate":
                    target = screen.ShadowBlendIntermediate;
                    break;
                case "shadowintermediate":
                    target = screen.ShadowIntermediate;
                    break;
                default:
                    console.AddLine("Unknown key", ChatChannel.Default, Color4.Red);
                    return false;
            }

            using (var image = target.Texture.CopyToImage())
            {
                var timestamp = DateTime.Now.ToString("yyyyMMddTHHmmsszzz");
                var filename = Path.GetFullPath(PathHelpers.ExecutableRelativeFile($"dumprt-{key}-{timestamp}.png"));
                image.SaveToFile(filename);
                console.AddLine($"Saved dump to {filename}!", ChatChannel.Default, Color4.Green);
            }

            return false;
        }
    }

    class ReRenderCommand : IConsoleCommand
    {
        public string Command => "rerender";
        public string Description => "Forces the current GameState to re-render everything.";
        public string Help => "";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var screen = IoCManager.Resolve<IStateManager>().CurrentState;
            screen.FormResize();
            return false;
        }
    }
}
