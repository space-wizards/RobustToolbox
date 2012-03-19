using ClientInterfaces.GOC;
using GorgonLibrary.Graphics;

namespace ClientInterfaces.UserInterface
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
