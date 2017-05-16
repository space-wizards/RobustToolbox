using SFML.Graphics;
using SS14.Client.Interfaces.GOC;
using SS14.Shared.GameObjects;

namespace SS14.Client.Interfaces.UserInterface
{
    public interface IDragDropInfo
    {
        Entity DragEntity { get; }
        Sprite DragSprite { get; }
        bool IsEntity { get; }
        bool IsActive { get; }

        void Reset();

        void StartDrag(Entity entity);
    }
}
