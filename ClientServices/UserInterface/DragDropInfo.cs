using System;
using CGO;
using ClientInterfaces.GOC;
using ClientInterfaces.UserInterface;
using ClientServices.Helpers;
using GameObject;
using GorgonLibrary.Graphics;
using SS13.IoC;
using ClientInterfaces.Resource;
using ClientInterfaces.UserInterface;
using ClientInterfaces.Placement;

namespace ClientServices.UserInterface
{
    public class DragDropInfo : IDragDropInfo
    {
        public Entity DragEntity { get; private set; }
        public Sprite DragSprite { get; private set; }
        public IPlayerAction DragAction { get; private set; }
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

        public void StartDrag(Entity entity)
        {
            Reset();

            IoCManager.Resolve<IUserInterfaceManager>().CancelTargeting();
            IoCManager.Resolve<IPlacementManager>().Clear();

            DragEntity = entity;
            DragSprite = Utilities.GetSpriteComponentSprite(entity);
            IsEntity = true;
        }

        public void StartDrag(IPlayerAction action)
        {
            Reset();
            DragAction = action;
            DragSprite = IoCManager.Resolve<IResourceManager>().GetSprite(action.Icon);
            IsEntity = false;
        }
    }
}
