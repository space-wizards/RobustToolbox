using ClientInterfaces.GOC;
using GameObject;
using GorgonLibrary.Graphics;

namespace ClientInterfaces.UserInterface
{
    public interface IDragDropInfo
    {
        Entity DragEntity { get; }
        IPlayerAction DragAction { get; }
        Sprite DragSprite { get; }
        bool IsEntity { get; }
        bool IsActive { get; }

        void Reset();

        void StartDrag(Entity entity);
        void StartDrag(IPlayerAction action);
    }
}
