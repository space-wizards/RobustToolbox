using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Server.Interfaces.GameObjects
{
    public interface IServerTransformComponent : ITransformComponent
    {
        // These definitions allow setting too,
        // because the client can only read the properties.
        new Angle LocalRotation { get; set; }
        new LocalCoordinates LocalPosition { get; set; }
        new Vector2 WorldPosition { get; set; }

        // These definitions are upgraded to IServerTransformCompont.
        new IServerTransformComponent Parent { get; }
        new IServerTransformComponent GetMapTransform();

        void DetachParent();
        void AttachParent(IServerTransformComponent parent);
        void AttachParent(IEntity parent);
    }
}
