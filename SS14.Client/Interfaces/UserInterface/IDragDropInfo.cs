using SFML.Graphics;
using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Client.Interfaces.UserInterface
{
    public interface IDragDropInfo
    {
        IEntity DragEntity { get; }
        Sprite DragSprite { get; }
        bool IsEntity { get; }
        bool IsActive { get; }

        void Reset();

        void StartDrag(IEntity entity);
    }
}
