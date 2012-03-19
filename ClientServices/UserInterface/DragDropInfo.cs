using CGO;
using ClientInterfaces.GOC;
using ClientInterfaces.UserInterface;
using ClientServices.Helpers;
using GorgonLibrary.Graphics;
using SS13.IoC;
using ClientInterfaces.Resource;

namespace ClientServices.UserInterface
{
    public class DragDropInfo : IDragDropInfo
    {
        public IEntity DragEntity { get; private set; }
        public Sprite DragSprite { get; private set; }
        public PlayerAction DragAction { get; private set; }
        public bool IsEntity { get; private set;}
        public bool IsActive { get { return Active(); } }

        public void Reset()
        {
            DragEntity = null;
            DragSprite = null;
            DragAction = null;
            IsEntity = true;
        }

        public bool Active()
        {
            return (DragAction != null || DragEntity != null);
        }

        public void StartDrag(IEntity entity)
        {
            Reset();
            DragEntity = entity;
            DragSprite = Utilities.GetSpriteComponentSprite(entity);
            IsEntity = true;
        }

        public void StartDrag(PlayerAction action)
        {
            Reset();
            DragAction = action;
            DragSprite = IoCManager.Resolve<IResourceManager>().GetSprite(action.icon);
            IsEntity = false;
        }
    }
}
