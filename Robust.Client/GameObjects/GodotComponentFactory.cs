using Robust.Client.GameObjects.Components.Transform;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects.Components;

namespace Robust.Client.GameObjects
{
    public class GodotComponentFactory : ClientComponentFactory
    {
        public GodotComponentFactory() : base()
        {
            Register<GodotTransformComponent>(overwrite: true);
            RegisterReference<GodotTransformComponent, ITransformComponent>();
            RegisterReference<GodotTransformComponent, IGodotTransformComponent>();
        }
    }
}
