using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.Components.Eye;
using Robust.Server.GameObjects.Components.Markers;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Appearance;
using Robust.Shared.GameObjects.Components.Eye;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.GameObjects.Components.Renderable;
using Robust.Shared.GameObjects.Components.Timers;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Interfaces.GameObjects.Components;

namespace Robust.Server.GameObjects
{
    public class ServerComponentFactory : ComponentFactory
    {
        public ServerComponentFactory()
        {
            // Required for the engine to work
            Register<MetaDataComponent>();
            RegisterReference<MetaDataComponent, IMetaDataComponent>();

            // Required for the engine to work
            Register<TransformComponent>();
            RegisterReference<TransformComponent, ITransformComponent>();

            Register<MapComponent>();
            RegisterReference<MapComponent, IMapComponent>();

            Register<MapGridComponent>();
            RegisterReference<MapGridComponent, IMapGridComponent>();

            Register<EyeComponent>();
            RegisterReference<EyeComponent, SharedEyeComponent>();

            Register<BasicActorComponent>();
            RegisterReference<BasicActorComponent, IActorComponent>();

            Register<PhysicsComponent>();
            RegisterReference<PhysicsComponent, IPhysicsComponent>();
            Register<PointLightComponent>();
            Register<OccluderComponent>();

            RegisterIgnore("Input");
            Register<SpriteComponent>();
            RegisterReference<SpriteComponent, SharedSpriteComponent>();
            RegisterReference<SpriteComponent, ISpriteRenderableComponent>();

            Register<ContainerManagerComponent>();
            RegisterReference<ContainerManagerComponent, IContainerManager>();

            Register<AppearanceComponent>();
            RegisterReference<AppearanceComponent, SharedAppearanceComponent>();

            Register<SnapGridComponent>();

            Register<ServerUserInterfaceComponent>();
            RegisterReference<ServerUserInterfaceComponent, SharedUserInterfaceComponent>();

            Register<IgnorePauseComponent>();

            Register<TimerComponent>();

            RegisterIgnore("AnimationPlayer");

#if DEBUG
            Register<DebugExceptionOnAddComponent>();
            Register<DebugExceptionExposeDataComponent>();
            Register<DebugExceptionInitializeComponent>();
            Register<DebugExceptionStartupComponent>();
#endif
        }
    }
}
