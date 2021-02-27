using Robust.Shared.GameObjects;
using Robust.Shared.Physics;

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
            RegisterReference<PhysicsComponent, IPhysBody>();
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

            Register<TimerComponent>();

            RegisterIgnore("AnimationPlayer");

#if DEBUG
            Register<DebugExceptionOnAddComponent>();
            Register<DebugExceptionExposeDataComponent>();
            Register<DebugExceptionInitializeComponent>();
            Register<DebugExceptionStartupComponent>();
#endif

            Register<MapSaveIdComponent>();
        }
    }
}
