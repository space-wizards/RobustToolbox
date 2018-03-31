using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.GameObjects
{
    public class GodotComponentFactory : ClientComponentFactory
    {
        public GodotComponentFactory() : base()
        {
            Register<GodotTransformComponent>(overwrite: true);
            RegisterReference<GodotTransformComponent, ITransformComponent>();
            RegisterReference<GodotTransformComponent, IGodotTransformComponent>();

            Register<GodotCollidableComponent>(overwrite: true);
            RegisterReference<GodotCollidableComponent, ICollidableComponent>();
        }
    }
}
