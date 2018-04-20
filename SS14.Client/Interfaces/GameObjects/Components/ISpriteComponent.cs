using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Client.Interfaces.GameObjects.Components
{
    public interface ISpriteComponent : IComponent
    {
        void FrameUpdate(float delta);
    }
}
