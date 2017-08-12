using SFML.Graphics;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.Console;
using SS14.Client.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS14.Client.Console
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
                console.AddLine($"entity {e.Uid}, {e.Prototype.Name}.", Color.White);
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
}
