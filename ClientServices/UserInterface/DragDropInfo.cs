using CGO;
using ClientInterfaces.GOC;
using ClientInterfaces.UserInterface;
using ClientServices.Helpers;
using GorgonLibrary.Graphics;

namespace ClientServices.UserInterface
{
    public class DragDropInfo : IDragDropInfo
    {
        public IEntity DragEntity { get; private set; }
        public Sprite DragSprite { get; private set; }
        public bool IsEntity { get; private set;}

        public void Reset()
        {
            DragEntity = null;
            DragSprite = null;
            IsEntity = true;
        }

        public void StartDrag(IEntity entity)
        {
            DragEntity = entity;
            DragSprite = Utilities.GetSpriteComponentSprite(entity);
            IsEntity = true;
        }
    }
}
